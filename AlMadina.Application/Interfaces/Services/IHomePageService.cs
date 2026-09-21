using AlMadina.Application.DTOs;

namespace AlMadina.Application.Interfaces.Services
{
    public interface IHomePageService
    {
        Task<HomePageConfigDto> GetConfigAsync();
        Task UpdateCategoryOrderAsync(UpdateCategoryOrderDto dto);
        Task UpdateFeaturedProductAsync(UpdateFeaturedProductDto dto);
        Task UpdateFeaturedProductsOrderAsync(UpdateFeaturedProductsOrderDto dto);
    }
}
