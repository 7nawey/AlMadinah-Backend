using AlMadina.Application.DTOs;

namespace AlMadina.Application.Interfaces.Services
{
    public interface IAdminUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<UserDto?> GetByIdAsync(string id);
        Task<bool> ToggleBlockAsync(string id);
        Task<bool> ToggleFlagAsync(string id);
        Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId);
    }
}
