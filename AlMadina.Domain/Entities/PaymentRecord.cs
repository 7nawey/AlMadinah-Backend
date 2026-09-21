namespace AlMadina.Domain.Entities
{
    public class PaymentRecord
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public int PaymobOrderId { get; set; }
        public int IntegrationId { get; set; }
        public int AmountCents { get; set; }
        public string Currency { get; set; } = "EGP";
        public bool IsProcessed { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? TransactionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
