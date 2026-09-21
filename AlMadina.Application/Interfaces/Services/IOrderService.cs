using AlMadina.Application.DTOs;

namespace AlMadina.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<OrderResultDto> CreateAsync(CreateOrderDto dto, string userId);
        Task<OrderResultDto> CreateInStoreAsync(CreateInStoreOrderDto dto, string userId);
        Task FinalizePaymentAsync(Guid orderId);
        Task<bool> CancelAsync(Guid orderId, string userId, bool isAdmin);
        Task<IEnumerable<OrderDto>> GetAllAsync();
        Task<IEnumerable<OrderDto>> GetByUserIdAsync(string userId);
        Task<OrderDto?> GetByIdAsync(Guid id);
        Task<bool> UpdateStatusAsync(UpdateOrderStatusDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task ApplyReturnAsync(Guid orderId, decimal returnedAmount, bool isFullyReturned);
    }
}
