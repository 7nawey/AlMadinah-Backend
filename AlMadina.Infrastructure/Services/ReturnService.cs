using AlMadina.Application.DTOs;
using AlMadina.Application.Exceptions;
using AlMadina.Application.Interfaces;
using AlMadina.Application.Interfaces.Services;
using AlMadina.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlMadina.Infrastructure.Services
{
    public class ReturnService : IReturnService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderService _orderService;
        private readonly AutoMapper.IMapper _mapper;

        public ReturnService(IUnitOfWork unitOfWork, IOrderService orderService, AutoMapper.IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _orderService = orderService;
            _mapper = mapper;
        }

        public async Task<ReturnRequestDto> CreateAsync(CreateReturnRequestDto dto, string userId)
        {
            var order = await _unitOfWork.Orders.GetAllQueryable()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null)
                throw new NotFoundException("Order not found.");

            if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Paid)
                throw new ValidationException("Only delivered or paid orders can be returned.");

            // Get existing returns for this order to prevent double-returning.
            // Pending requests count too: the customer must not be able to
            // request the same items again while a request is awaiting review.
            var existingReturns = await _unitOfWork.ReturnRequests.GetAllQueryable()
                .Include(r => r.Items)
                .Where(r => r.OrderId == dto.OrderId &&
                       (r.Status == ReturnStatus.Pending ||
                        r.Status == ReturnStatus.Approved ||
                        r.Status == ReturnStatus.PartiallyApproved))
                .SelectMany(r => r.Items)
                .GroupBy(i => i.OrderItemId)
                .Select(g => new { OrderItemId = g.Key, ReturnedQty = g.Sum(i => i.Quantity) })
                .ToDictionaryAsync(x => x.OrderItemId, x => x.ReturnedQty);

            var returnRequest = new ReturnRequest
            {
                Id = Guid.NewGuid(),
                OrderId = dto.OrderId,
                UserId = userId,
                // The request starts as Pending: stock is restored and the
                // order total is adjusted ONLY when an admin approves it in
                // ProcessAsync — never here.
                Status = ReturnStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var itemDto in dto.Items)
            {
                if (itemDto.Quantity <= 0)
                    throw new ValidationException("Return quantity must be greater than zero.");

                var orderItem = order.Items.FirstOrDefault(i => i.Id == itemDto.OrderItemId);
                if (orderItem == null)
                    throw new NotFoundException($"Order item {itemDto.OrderItemId} not found.");

                if (itemDto.Quantity > orderItem.Quantity)
                    throw new ValidationException($"Cannot return more than purchased quantity for item {orderItem.ProductId}.");

                var alreadyReturned = existingReturns.TryGetValue(itemDto.OrderItemId, out var qty) ? qty : 0;
                if (alreadyReturned + itemDto.Quantity > orderItem.Quantity)
                    throw new ValidationException($"Cannot return more than purchased quantity. Already returned {alreadyReturned} of {orderItem.Quantity} for this item.");

                returnRequest.Items.Add(new ReturnItem
                {
                    Id = Guid.NewGuid(),
                    ReturnRequestId = returnRequest.Id,
                    OrderItemId = itemDto.OrderItemId,
                    ProductId = orderItem.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = orderItem.UnitPrice,
                    Status = ReturnItemStatus.Pending
                });
            }

            await _unitOfWork.ReturnRequests.AddAsync(returnRequest);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(returnRequest);
        }

        public async Task<bool> ProcessAsync(ProcessReturnDto dto)
        {
            var returnRequest = await _unitOfWork.ReturnRequests.GetAllQueryable()
                .Include(r => r.Items)
                .Include(r => r.Order)
                .ThenInclude(o => o!.Items)
                .FirstOrDefaultAsync(r => r.Id == dto.ReturnRequestId);

            if (returnRequest == null)
                return false;

            if (returnRequest.Status != ReturnStatus.Pending)
                throw new ValidationException("Return request has already been processed.");

            // Map string status to enum
            ReturnStatus status;
            if (!Enum.TryParse<ReturnStatus>(dto.Status, true, out status))
                throw new ValidationException("Invalid return status.");

            if (status == ReturnStatus.Pending)
                throw new ValidationException("Processing must result in Approved, Rejected or PartiallyApproved.");

            returnRequest.Status = status;

            // Process each item based on decisions
            if (dto.ItemDecisions != null)
            {
                foreach (var decision in dto.ItemDecisions)
                {
                    var item = returnRequest.Items.FirstOrDefault(i => i.Id == decision.ReturnItemId);
                    if (item == null) continue;

                    if (Enum.TryParse<ReturnItemStatus>(decision.Status, true, out var itemStatus))
                    {
                        item.Status = itemStatus;

                        // If approved, restore stock — exactly once, here.
                        // Stock is NEVER touched at request creation time.
                        if (itemStatus == ReturnItemStatus.Approved)
                        {
                            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                            if (product != null)
                            {
                                product.StockQuantity += item.Quantity;
                                _unitOfWork.Products.Update(product);
                            }
                        }
                    }
                }
            }
            else if (status == ReturnStatus.Approved)
            {
                // Approve all items
                foreach (var item in returnRequest.Items)
                {
                    item.Status = ReturnItemStatus.Approved;
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                        _unitOfWork.Products.Update(product);
                    }
                }
            }
            else // Rejected
            {
                foreach (var item in returnRequest.Items)
                    item.Status = ReturnItemStatus.Rejected;
            }

            // --------------------------------------------------------
            // Apply approved amount to the order total.
            // "Fully returned" considers previous approved returns too.
            // --------------------------------------------------------

            var approvedItems = returnRequest.Items
                .Where(i => i.Status == ReturnItemStatus.Approved)
                .ToList();

            if (approvedItems.Count > 0)
            {
                var totalReturnedAmount = approvedItems.Sum(i => i.Quantity * i.UnitPrice);

                var approvedThisRequest = approvedItems
                    .GroupBy(i => i.OrderItemId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

                var previousApproved = await _unitOfWork.ReturnRequests.GetAllQueryable()
                    .Include(r => r.Items)
                    .Where(r => r.OrderId == returnRequest.OrderId
                        && r.Id != returnRequest.Id
                        && (r.Status == ReturnStatus.Approved || r.Status == ReturnStatus.PartiallyApproved))
                    .SelectMany(r => r.Items)
                    .Where(i => i.Status == ReturnItemStatus.Approved)
                    .GroupBy(i => i.OrderItemId)
                    .Select(g => new { OrderItemId = g.Key, Qty = g.Sum(i => i.Quantity) })
                    .ToDictionaryAsync(x => x.OrderItemId, x => x.Qty);

                var fullyReturned = returnRequest.Order!.Items.All(oi =>
                {
                    var prev = previousApproved.TryGetValue(oi.Id, out var p) ? p : 0;
                    var curr = approvedThisRequest.TryGetValue(oi.Id, out var c) ? c : 0;
                    return prev + curr >= oi.Quantity;
                });

                await _orderService.ApplyReturnAsync(
                    returnRequest.OrderId, totalReturnedAmount, fullyReturned);
            }

            _unitOfWork.ReturnRequests.Update(returnRequest);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ReturnRequestDto>> GetByUserAsync(string userId)
        {
            var requests = await _unitOfWork.ReturnRequests.GetAllQueryable()
                .Include(r => r.Items)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return requests.Select(MapToDto);
        }

        public async Task<IEnumerable<ReturnRequestDto>> GetAllAsync()
        {
            var requests = await _unitOfWork.ReturnRequests.GetAllQueryable()
                .Include(r => r.Items)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return requests.Select(MapToDto);
        }

        public async Task<ReturnRequestDto?> GetByIdAsync(Guid id)
        {
            var request = await _unitOfWork.ReturnRequests.GetAllQueryable()
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id);

            return request == null ? null : MapToDto(request);
        }

        private ReturnRequestDto MapToDto(ReturnRequest r)
        {
            var dto = _mapper.Map<ReturnRequestDto>(r);

            // ReturnItem has no Product navigation — ProductName stays
            // empty by design; filled here when the caller needs it.
            return dto;
        }
    }
}
