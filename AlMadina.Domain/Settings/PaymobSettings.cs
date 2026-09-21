namespace AlMadina.Domain.Settings
{
    public class PaymobSettings
    {
        public const string SectionName = "Paymob";
        public string IframeBaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int CardIntegrationId { get; set; }
        public int WalletIntegrationId { get; set; }
        public int IframeId { get; set; }
        public string HmacSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://accept.paymob.com/api";

        public string ReturnUrl { get; set; }
    }
}
