using AlMadina.Application.DTOs;

namespace AlMadina.Application.Interfaces.Services
{
    public interface IReturnService
    {
        Task<ReturnRequestDto> CreateAsync(CreateReturnRequestDto dto, string userId);
        Task<IEnumerable<ReturnRequestDto>> GetByUserAsync(string userId);
        Task<IEnumerable<ReturnRequestDto>> GetAllAsync();
        Task<ReturnRequestDto?> GetByIdAsync(Guid id);
        Task<bool> ProcessAsync(ProcessReturnDto dto);
    }
}
