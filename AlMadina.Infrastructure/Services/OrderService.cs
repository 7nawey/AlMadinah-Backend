using AlMadina.Application.DTOs;
using AlMadina.Application.Exceptions;
using AlMadina.Application.Interfaces;
using AlMadina.Application.Interfaces.Services;
using AlMadina.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlMadina.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        // Single source of truth for wholesale/free-shipping rules.
        // The Angular checkout mirrors these values — keep them in sync.
        private const decimal WholesaleThreshold = 1000m;
        private const decimal RetailDeliveryFee = 25m;

        private readonly IUnitOfWork _unitOfWork;
        private readonly AutoMapper.IMapper _mapper;

        public OrderService(IUnitOfWork unitOfWork, AutoMapper.IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<OrderResultDto> CreateAsync(CreateOrderDto dto, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User must be authenticated to create orders.");

            if (dto.Items == null || !dto.Items.Any())
                throw new ValidationException("Order must contain at least one item.");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = OrderStatus.PendingPayment,
                IsPaid = false,
                IsInStore = dto.IsInStore,
                CreatedAt = DateTime.UtcNow
            };

            if (!dto.IsInStore)
            {
                if (string.IsNullOrWhiteSpace(dto.CustomerPhone))
                    throw new ValidationException("Phone number is required for delivery orders.");
                if (string.IsNullOrWhiteSpace(dto.CustomerAddress))
                    throw new ValidationException("Address is required for delivery orders.");

                order.CustomerPhone = dto.CustomerPhone;
                order.CustomerAddress = dto.CustomerAddress;
            }

            order.Notes = dto.Notes;

            decimal subtotal = 0;

            foreach (var itemDto in dto.Items)
            {
                if (itemDto.Quantity <= 0)
                    throw new ValidationException($"Quantity must be greater than zero for product {itemDto.ProductId}.");

                var product = await _unitOfWork.Products.GetByIdAsync(itemDto.ProductId);
                if (product == null)
                    throw new NotFoundException($"Product {itemDto.ProductId} not found.");

                if (product.StockQuantity < itemDto.Quantity)
                    throw new ValidationException($"Insufficient stock for {product.NameAr}. Available: {product.StockQuantity}, Requested: {itemDto.Quantity}");

                var unitPrice = await CalculateUnitPriceAsync(product);
                var totalPrice = unitPrice * itemDto.Quantity;

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice
                };

                order.Items.Add(orderItem);
                subtotal += totalPrice;
            }

            // Determine order type and delivery fee.
            // Wholesale starts AT the threshold (>=) to match the
            // Angular checkout, which shows free shipping from 1000 EGP.
            order.Type = subtotal >= WholesaleThreshold ? OrderType.Wholesale : OrderType.Retail;
            order.DeliveryFee = order.Type == OrderType.Wholesale ? 0 : (dto.IsInStore ? 0 : RetailDeliveryFee);
            order.TotalPrice = subtotal + order.DeliveryFee;

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            return new OrderResultDto
            {
                OrderId = order.Id,
                Total = order.TotalPrice,
                Status = (int)order.Status,
                Type = order.Type,
                DeliveryFee = order.DeliveryFee
            };
        }

        public async Task<OrderResultDto> CreateInStoreAsync(CreateInStoreOrderDto dto, string userId)
        {
            var createDto = new CreateOrderDto
            {
                Items = dto.Items,
                Notes = dto.Notes,
                IsInStore = true,
                CustomerPhone = string.Empty,
                CustomerAddress = string.Empty
            };

            return await CreateAsync(createDto, userId);
        }

        public async Task FinalizePaymentAsync(Guid orderId)
        {
            var order = await _unitOfWork.Orders.GetAllQueryable()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new NotFoundException($"Order {orderId} not found.");

            // Idempotency: if already paid, do nothing (prevents duplicate stock deduction)
            if (order.IsPaid)
                return;

            if (order.Status != OrderStatus.PendingPayment)
                throw new ValidationException("Order is not in pending payment state.");

            // Re-check stock from the database before deducting
            foreach (var item in order.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null)
                    throw new NotFoundException($"Product {item.ProductId} not found.");

                if (product.StockQuantity < item.Quantity)
                    throw new ValidationException($"Insufficient stock for {product.NameAr}. Available: {product.StockQuantity}, Requested: {item.Quantity}");

                product.StockQuantity -= item.Quantity;
                _unitOfWork.Products.Update(product);
            }

            order.Status = OrderStatus.Paid;
            order.IsPaid = true;
            _unitOfWork.Orders.Update(order);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> CancelAsync(Guid orderId, string userId, bool isAdmin)
        {
            var order = await _unitOfWork.Orders.GetAllQueryable()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return false;

            if (!isAdmin && order.UserId != userId)
                throw new UnauthorizedException("You can only cancel your own orders.");

            // A delivered (or already closed) order can never be cancelled —
            // otherwise the customer would get the goods AND the stock back.
            if (order.Status == OrderStatus.Delivered ||
                order.Status == OrderStatus.Returned ||
                order.Status == OrderStatus.Cancelled)
                throw new ValidationException($"Orders in '{order.Status}' status cannot be cancelled.");

            // If already paid, restore stock
            if (order.IsPaid)
            {
                foreach (var item in order.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                        _unitOfWork.Products.Update(product);
                    }
                }
            }

            order.Status = OrderStatus.Cancelled;
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<OrderDto>> GetAllAsync()
        {
            var orders = await _unitOfWork.Orders.GetAllQueryable()
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(MapToDto);
        }

        public async Task<IEnumerable<OrderDto>> GetByUserIdAsync(string userId)
        {
            var orders = await _unitOfWork.Orders.GetAllQueryable()
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(MapToDto);
        }

        public async Task<OrderDto?> GetByIdAsync(Guid id)
        {
            var order = await _unitOfWork.Orders.GetAllQueryable()
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            return order == null ? null : MapToDto(order);
        }

        public async Task<bool> UpdateStatusAsync(UpdateOrderStatusDto dto)
        {
            var order = await _unitOfWork.Orders.GetAllQueryable()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null)
                return false;

            // Prevent changing status of already-cancelled or returned orders
            if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Returned)
                throw new ValidationException("Cannot change status of cancelled or returned orders.");

            // Prevent un-paying a paid order
            if (order.IsPaid && !dto.IsPaid)
                throw new ValidationException("Cannot mark a paid order as unpaid.");

            // Delivered orders must be paid
            if (dto.Status == OrderStatus.Delivered && !dto.IsPaid)
                throw new ValidationException("Delivered orders must be marked as paid.");

            // Stock sync: when an admin marks an unpaid order as paid
            // (cash payment, bank transfer, etc.), the stock must be
            // deducted here exactly as FinalizePaymentAsync does —
            // otherwise the dashboard stock drifts from reality.
            var transitionedToPaid = dto.IsPaid && !order.IsPaid;

            if (transitionedToPaid)
            {
                foreach (var item in order.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product == null)
                        throw new NotFoundException($"Product {item.ProductId} not found.");

                    if (product.StockQuantity < item.Quantity)
                        throw new ValidationException($"Insufficient stock for {product.NameAr}. Available: {product.StockQuantity}, Requested: {item.Quantity}");

                    product.StockQuantity -= item.Quantity;
                    _unitOfWork.Products.Update(product);
                }
            }

            order.Status = dto.Status;
            order.IsPaid = dto.IsPaid;
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var order = await _unitOfWork.Orders.GetAllQueryable()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return false;

            // Restore stock if paid
            if (order.IsPaid)
            {
                foreach (var item in order.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                        _unitOfWork.Products.Update(product);
                    }
                }
            }

            _unitOfWork.Orders.Delete(order);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task ApplyReturnAsync(Guid orderId, decimal returnedAmount, bool isFullyReturned)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
                throw new NotFoundException($"Order {orderId} not found.");

            order.TotalPrice -= returnedAmount;
            if (order.TotalPrice < 0)
                order.TotalPrice = 0;

            if (isFullyReturned)
                order.Status = OrderStatus.Returned;

            _unitOfWork.Orders.Update(order);
        }

        private async Task<decimal> CalculateUnitPriceAsync(Product product)
        {
            // An active deal takes precedence over the product's own discount columns.
            var now = DateTime.UtcNow;

            var deal = await _unitOfWork.Deals
                .GetAllQueryable()
                .Where(d => d.ProductId == product.Id
                    && d.IsActive
                    && (d.StartDate == default || d.StartDate <= now)
                    && (d.EndDate == default || d.EndDate >= now))
                .OrderByDescending(d => d.DiscountPercentage ?? 0)
                .FirstOrDefaultAsync();

            if (deal != null)
                return deal.DiscountedPrice;

            var price = product.Price;

            if (product.DiscountPercentage.HasValue && product.DiscountPercentage.Value > 0 &&
                product.DiscountStartDate.HasValue && product.DiscountEndDate.HasValue)
            {
                if (now >= product.DiscountStartDate.Value && now <= product.DiscountEndDate.Value)
                {
                    price = price * (1 - product.DiscountPercentage.Value / 100m);
                }
            }

            return price;
        }

        private OrderDto MapToDto(Order order)
        {
            var dto = _mapper.Map<OrderDto>(order);

            // ProductName is not mapped by AutoMapper (Product may be
            // unloaded); fill it from the loaded navigation when present.
            foreach (var itemDto in dto.Items)
            {
                var entity = order.Items.FirstOrDefault(i => i.Id == itemDto.Id);
                itemDto.ProductName =
                    entity?.Product?.NameAr ??
                    entity?.Product?.NameEn ??
                    "Unknown";
            }

            return dto;
        }
    }
}
