using AlMadina.Application.DTOs;

namespace AlMadina.Application.Interfaces.Services
{
    public interface IStockMovementService
    {
        Task<IEnumerable<StockMovementDto>> GetAllAsync();
        Task<IEnumerable<StockMovementDto>> GetByProductAsync(Guid productId);
        Task<StockMovementDto> AddAsync(CreateStockMovementDto dto);
        Task<StockMovementDto> AddWithNewProductAsync(CreateStockMovementWithNewProductDto dto);
    }
}
