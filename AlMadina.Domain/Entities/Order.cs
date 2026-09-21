namespace AlMadina.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal TotalPrice { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

        public OrderType Type { get; set; } = OrderType.Retail;

        public string CustomerPhone { get; set; } = string.Empty;

        public string CustomerAddress { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public decimal DeliveryFee { get; set; }

        public bool IsPaid { get; set; }

        public bool IsInStore { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }

    public enum OrderStatus
    {
        PendingPayment,
        Paid,
        Failed,
        Processing,
        Delivered,
        Returned,
        Cancelled
    }

    public enum OrderType
    {
        Retail,
        Wholesale
    }
}
