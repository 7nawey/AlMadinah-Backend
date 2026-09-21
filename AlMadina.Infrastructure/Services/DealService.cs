using AlMadina.Application.DTOs;
using AlMadina.Application.Exceptions;
using AlMadina.Application.Interfaces;
using AlMadina.Application.Interfaces.Services;
using AlMadina.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlMadina.Infrastructure.Services
{
    public class DealService : IDealService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DealService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<DealDto>> GetAllAsync()
        {
            var deals = await _unitOfWork.Deals.GetAllQueryable()
                .Include(d => d.Product)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return deals.Select(MapToDto);
        }

        public async Task<IEnumerable<DealDto>> GetActiveAsync()
        {
            var now = DateTime.UtcNow;
            var deals = await _unitOfWork.Deals.GetAllQueryable()
                .Include(d => d.Product)
                .Where(d => d.IsActive
                    && (d.StartDate == default || d.StartDate <= now)
                    && (d.EndDate == default || d.EndDate >= now))
                .ToListAsync();

            return deals.Select(MapToDto);
        }

        public async Task<DealDto?> GetByIdAsync(Guid id)
        {
            var deal = await _unitOfWork.Deals.GetAllQueryable()
                .Include(d => d.Product)
                .FirstOrDefaultAsync(d => d.Id == id);

            return deal == null ? null : MapToDto(deal);
        }

        public async Task<DealDto> CreateAsync(CreateDealDto dto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product == null)
                throw new NotFoundException("Product not found.");

            ResolveDealValues(dto.OriginalPrice, dto.DiscountedPrice, dto.DiscountPercentage, product.Price,
                out var originalPrice, out var discountedPrice, out var discountPercentage);

            var deal = new Deal
            {
                Id = Guid.NewGuid(),
                ProductId = dto.ProductId,
                OriginalPrice = originalPrice,
                DiscountedPrice = discountedPrice,
                DiscountPercentage = discountPercentage,
                StartDate = dto.StartDate ?? default,
                EndDate = dto.EndDate ?? default,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Deals.AddAsync(deal);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(deal);
        }

        public async Task<bool> UpdateAsync(UpdateDealDto dto)
        {
            var deal = await _unitOfWork.Deals.GetByIdAsync(dto.Id);
            if (deal == null)
                return false;

            var product = await _unitOfWork.Products.GetByIdAsync(deal.ProductId);

            ResolveDealValues(dto.OriginalPrice, dto.DiscountedPrice, dto.DiscountPercentage,
                product?.Price ?? deal.OriginalPrice,
                out var originalPrice, out var discountedPrice, out var discountPercentage);

            deal.OriginalPrice = originalPrice;
            deal.DiscountedPrice = discountedPrice;
            deal.DiscountPercentage = discountPercentage;
            deal.StartDate = dto.StartDate ?? default;
            deal.EndDate = dto.EndDate ?? default;
            deal.IsActive = dto.IsActive;

            _unitOfWork.Deals.Update(deal);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var deal = await _unitOfWork.Deals.GetByIdAsync(id);
            if (deal == null)
                return false;

            _unitOfWork.Deals.Delete(deal);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(Guid id)
        {
            var deal = await _unitOfWork.Deals.GetByIdAsync(id);
            if (deal == null)
                return false;

            deal.IsActive = !deal.IsActive;
            _unitOfWork.Deals.Update(deal);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Fills in the price triad (original / discounted / percentage) from whatever the caller provided.
        /// The original price falls back to the product's current price when not supplied.
        /// </summary>
        private static void ResolveDealValues(decimal? originalPriceDto, decimal? discountedPriceDto,
            decimal? percentageDto, decimal productPrice,
            out decimal originalPrice, out decimal discountedPrice, out decimal? discountPercentage)
        {
            originalPrice = (originalPriceDto.HasValue && originalPriceDto.Value > 0) ? originalPriceDto.Value : productPrice;

            if (originalPrice <= 0)
                throw new ValidationException("Original price could not be determined. Provide a valid product or price.");

            if (discountedPriceDto.HasValue && discountedPriceDto.Value > 0)
            {
                discountedPrice = discountedPriceDto.Value;

                if (percentageDto.HasValue && percentageDto.Value > 0)
                {
                    discountPercentage = percentageDto.Value;
                }
                else
                {
                    discountPercentage = Math.Round((1 - discountedPrice / originalPrice) * 100m, 2);
                }
            }
            else if (percentageDto.HasValue && percentageDto.Value > 0)
            {
                discountPercentage = percentageDto.Value;
                discountedPrice = Math.Round(originalPrice * (1 - percentageDto.Value / 100m), 2);
            }
            else
            {
                throw new ValidationException("Provide either a discounted price or a discount percentage.");
            }

            if (discountedPrice > originalPrice)
                throw new ValidationException("Discounted price cannot be greater than the original price.");

            if (discountPercentage is < 0 or > 100)
                throw new ValidationException("Discount percentage must be between 0 and 100.");
        }

        private static DealDto MapToDto(Deal d)
        {
            var now = DateTime.UtcNow;
            return new DealDto
            {
                Id = d.Id,
                ProductId = d.ProductId,
                ProductName = d.Product?.NameAr ?? "Unknown",
                ProductImageUrl = d.Product?.ImageUrl,
                OriginalPrice = d.OriginalPrice,
                DiscountedPrice = d.DiscountedPrice,
                DiscountPercentage = d.DiscountPercentage,
                StartDate = d.StartDate == default ? null : d.StartDate,
                EndDate = d.EndDate == default ? null : d.EndDate,
                IsActive = d.IsActive,
                IsExpired = d.EndDate != default && d.EndDate < now
            };
        }
    }
}
