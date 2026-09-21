using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces;
using AlMadina.Application.Interfaces.Services;
using AlMadina.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlMadina.Infrastructure.Services
{
    public class StockMovementService : IStockMovementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AutoMapper.IMapper _mapper;

        public StockMovementService(IUnitOfWork unitOfWork, AutoMapper.IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StockMovementDto>> GetAllAsync()
        {
            var movements = await _unitOfWork.StockMovements.GetAllQueryable()
                .Include(m => m.Product)
                .OrderByDescending(m => m.Date)
                .ToListAsync();

            return movements.Select(MapToDto);
        }

        public async Task<IEnumerable<StockMovementDto>> GetByProductAsync(Guid productId)
        {
            var movements = await _unitOfWork.StockMovements.GetAllQueryable()
                .Include(m => m.Product)
                .Where(m => m.ProductId == productId)
                .OrderByDescending(m => m.Date)
                .ToListAsync();

            return movements.Select(MapToDto);
        }

        public async Task<StockMovementDto> AddAsync(CreateStockMovementDto dto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product == null)
                throw new Application.Exceptions.NotFoundException("Product not found.");

            product.StockQuantity += dto.Quantity;
            _unitOfWork.Products.Update(product);

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                UnitCost = dto.UnitCost,
                TotalCost = dto.UnitCost.HasValue ? dto.UnitCost.Value * dto.Quantity : null,
                Supplier = dto.Supplier,
                Type = StockMovementType.In,
                Notes = dto.Notes,
                Date = DateTime.UtcNow
            };

            await _unitOfWork.StockMovements.AddAsync(movement);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(movement);
        }

        public async Task<StockMovementDto> AddWithNewProductAsync(CreateStockMovementWithNewProductDto dto)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                NameAr = dto.NameAr,
                NameEn = dto.NameEn,
                NameArNormalized = AlMadina.Application.Common.ArabicTextNormalizer.Normalize(dto.NameAr),
                Description = dto.Description,
                Barcode = dto.Barcode,
                Price = dto.Price,
                CostPrice = dto.CostPrice,
                StockQuantity = dto.InitialStock,
                CategoryId = dto.CategoryId,
                IsActive = true
            };

            await _unitOfWork.Products.AddAsync(product);

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = dto.InitialStock,
                UnitCost = dto.UnitCost,
                TotalCost = dto.UnitCost.HasValue ? dto.UnitCost.Value * dto.InitialStock : null,
                Supplier = dto.Supplier,
                Type = StockMovementType.In,
                Notes = dto.Notes,
                Date = DateTime.UtcNow
            };

            await _unitOfWork.StockMovements.AddAsync(movement);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(movement);
        }

        private StockMovementDto MapToDto(StockMovement m)
        {
            var dto = _mapper.Map<StockMovementDto>(m);

            // ProductName is not part of the entity mapping — fill it
            // from the loaded navigation when present.
            dto.ProductName = m.Product?.NameAr ?? "Unknown";

            return dto;
        }
    }
}
