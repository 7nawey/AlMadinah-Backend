using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlMadina.Application.DTOs
{
    public class PaymobTokenRequestDto
    {
        public string ApiKey { get; set; } = string.Empty;
    }

    public class PaymobTokenResponseDto
    {
        public string Token { get; set; } = string.Empty;
    }

    public class PaymobOrderRequestDto
    {
        public string AuthToken { get; set; } = string.Empty;
        public decimal AmountCents { get; set; }
        public string Currency { get; set; } = "EGP";
        public int IntegrationId { get; set; }
        public List<PaymobOrderItemDto> Items { get; set; } = new();
        public string? DeliveryNeeded { get; set; } = "false";
    }

    public class PaymobOrderItemDto
    {
        public string Name { get; set; } = string.Empty;
        public int AmountCents { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class PaymobOrderResponseDto
    {
        public int Id { get; set; }
    }

    public class PaymobPaymentKeyRequestDto
    {
        public string AuthToken { get; set; } = string.Empty;
        public decimal AmountCents { get; set; }
        public int OrderId { get; set; }
        public string Currency { get; set; } = "EGP";
        public int IntegrationId { get; set; }
        public PaymobBillingDataDto BillingData { get; set; } = new();

        [JsonPropertyName("redirection_url")]
        public string RedirectionUrl { get; set; }
    }

    public class PaymobBillingDataDto
    {
        public string Apartment { get; set; } = "NA";
        public string Email { get; set; } = string.Empty;
        public string Floor { get; set; } = "NA";
        public string FirstName { get; set; } = string.Empty;
        public string Street { get; set; } = "NA";
        public string Building { get; set; } = "NA";
        public string PhoneNumber { get; set; } = string.Empty;
        public string ShippingMethod { get; set; } = "NA";
        public string PostalCode { get; set; } = "NA";
        public string City { get; set; } = "NA";
        public string Country { get; set; } = "NA";
        public string LastName { get; set; } = "NA";
        public string State { get; set; } = "NA";
    }

    public class PaymobPaymentKeyResponseDto
    {
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO sent by the frontend to initiate a Paymob payment.
    /// The backend loads the internal order and calculates the actual amount.
    /// </summary>
    public class PaymobInitiatePaymentDto
    {
        public Guid OrderId { get; set; }
        public string Currency { get; set; } = "EGP";

        /// <summary>
        /// Selected payment method: "card" or "wallet".
        /// The backend decides which Paymob integration ID to use.
        /// </summary>
        public string PaymentMethod { get; set; } = "card";

        /// <summary>
        /// Customer wallet mobile number (required when PaymentMethod = "wallet").
        /// </summary>
        public string? WalletPhone { get; set; }
    }

    public class PaymobInitiatePaymentResponseDto
    {
        public string PaymentToken { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string IframeUrl { get; set; } = string.Empty;

        /// <summary>
        /// Wallet provider redirect URL (populated for wallet payments).
        /// </summary>
        public string RedirectUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Wallet payment initiation via Paymob Accept API
    /// (POST /acceptance/payments/pay).
    /// </summary>
    public class PaymobWalletPayRequestDto
    {
        [JsonPropertyName("source")]
        public PaymobWalletSourceDto Source { get; set; } = new();

        [JsonPropertyName("payment_token")]
        public string PaymentToken { get; set; } = string.Empty;
    }

    public class PaymobWalletSourceDto
    {
        [JsonPropertyName("identifier")]
        public string Identifier { get; set; } = string.Empty;

        [JsonPropertyName("subtype")]
        public string Subtype { get; set; } = "WALLET";
    }

    /// <summary>
    /// DTO for Paymob callback/webhook.
    /// Uses JsonElement for the nested 'obj' payload to support robust HMAC calculation.
    /// </summary>
    public class PaymobCallbackDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("obj")]
        public JsonElement Obj { get; set; }
        public string? Hmac { get; set; }
    }
}
