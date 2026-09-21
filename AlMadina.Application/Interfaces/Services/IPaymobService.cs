using AlMadina.Application.DTOs;

namespace AlMadina.Application.Interfaces.Services
{
    public interface IPaymobService
    {
        Task<PaymobInitiatePaymentResponseDto> InitiatePaymentAsync(PaymobInitiatePaymentDto dto, string customerEmail, string customerFirstName);
        Task<bool> VerifyCallbackAsync(PaymobCallbackDto callback, string receivedHmac);
    }
}
