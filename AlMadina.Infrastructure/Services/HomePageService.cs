using AlMadina.Application.DTOs;
using AlMadina.Application.Exceptions;
using AlMadina.Application.Interfaces;
using AlMadina.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace AlMadina.Infrastructure.Services
{
    public class HomePageService : IHomePageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public HomePageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<HomePageConfigDto> GetConfigAsync()
        {
            var categories = await _unitOfWork.Categories.GetAllQueryable()
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CategoryOrderDto
                {
                    Id = c.Id,
                    NameAr = c.NameAr,
                    DisplayOrder = c.DisplayOrder,
                    IsVisible = c.IsVisible
                })
                .ToListAsync();

            var featuredProducts = await _unitOfWork.Products.GetAllQueryable()
                .Where(p => p.IsFeatured || p.IsPinned)
                .OrderByDescending(p => p.IsPinned)
                .ThenBy(p => p.DisplayOrder)
                .ThenByDescending(p => p.CreatedAt)
                .Select(p => new FeaturedProductDto
                {
                    Id = p.Id,
                    NameAr = p.NameAr,
                    ImageUrl = p.ImageUrl,
                    IsFeatured = p.IsFeatured,
                    IsPinned = p.IsPinned,
                    DisplayOrder = p.DisplayOrder
                })
                .ToListAsync();

            return new HomePageConfigDto
            {
                Categories = categories,
                FeaturedProducts = featuredProducts
            };
        }

        public async Task UpdateCategoryOrderAsync(UpdateCategoryOrderDto dto)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId);

            if (category == null)
                throw new NotFoundException("Category not found.");

            Console.WriteLine($"BEFORE: {category.NameAr} - {category.DisplayOrder}");

            category.DisplayOrder = dto.DisplayOrder;
            category.IsVisible = dto.IsVisible;

            _unitOfWork.Categories.Update(category);

            var result = await _unitOfWork.SaveChangesAsync();

            Console.WriteLine($"AFTER SAVE RESULT: {result}");
        }

        public async Task UpdateFeaturedProductAsync(UpdateFeaturedProductDto dto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product == null)
                throw new NotFoundException("Product not found.");

            product.IsFeatured = dto.IsFeatured;
            product.IsPinned = dto.IsPinned;
            product.DisplayOrder = dto.DisplayOrder;
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateFeaturedProductsOrderAsync(UpdateFeaturedProductsOrderDto dto)
        {
            foreach (var item in dto.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null)
                    continue;

                product.DisplayOrder = item.DisplayOrder;
                product.IsFeatured = true;
                _unitOfWork.Products.Update(product);
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
