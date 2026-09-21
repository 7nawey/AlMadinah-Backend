using AlMadina.Application.DTOs;

namespace AlMadina.Application.Interfaces.Services
{
    public interface IDealService
    {
        Task<IEnumerable<DealDto>> GetAllAsync();
        Task<IEnumerable<DealDto>> GetActiveAsync();
        Task<DealDto?> GetByIdAsync(Guid id);
        Task<DealDto> CreateAsync(CreateDealDto dto);
        Task<bool> UpdateAsync(UpdateDealDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ToggleActiveAsync(Guid id);
    }
}
