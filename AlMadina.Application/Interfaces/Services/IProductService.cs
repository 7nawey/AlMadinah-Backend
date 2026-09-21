using AlMadina.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace AlMadina.Application.Interfaces.Services
{
    public interface IProductService
    {
        Task<PagedResult<ProductDto>> GetAllAsync(int pageNumber = 1, int pageSize = 20);

        Task<PagedResult<ProductDto>> SearchAsync(string keyword, int pageNumber = 1, int pageSize = 20);

        Task<ProductDto?> GetByIdAsync(Guid id);

        Task<ProductDto?> GetForUpdateAsync(string value);

        Task<PagedResult<ProductDto>> GetFeaturedAsync(int pageNumber = 1, int pageSize = 20);

        Task<PagedResult<ProductDto>> GetDiscountedAsync(int pageNumber = 1, int pageSize = 20);

        Task<PagedResult<ProductDto>> GetBestSellingAsync(int pageNumber = 1, int pageSize = 20);

        Task<PagedResult<ProductDto>> GetOthersAsync(int pageNumber = 1, int pageSize = 20);

        Task<PagedResult<ProductDto>> GetProductsByCategoryAsync(
            Guid categoryId,
            int pageNumber = 1,
            int pageSize = 40);

        Task<PagedResult<ProductDto>> GetHomeRegularProductsAsync(int pageNumber = 1, int pageSize = 20);

        Task<ProductDto> CreateAsync(CreateProductDto dto);

        Task<bool> UpdateAsync(UpdateProductDto dto);

        Task<bool> DeleteAsync(Guid id);

        Task<string> UploadImageAsync(Guid id, IFormFile file);
    }
}
