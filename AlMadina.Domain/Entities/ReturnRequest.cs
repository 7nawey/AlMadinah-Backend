namespace AlMadina.Domain.Entities
{
    public class ReturnRequest
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ReturnStatus Status { get; set; } = ReturnStatus.Pending;

        public string? AdminNotes { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public List<ReturnItem> Items { get; set; } = new();
    }

    public class ReturnItem
    {
        public Guid Id { get; set; }

        public Guid ReturnRequestId { get; set; }
        public ReturnRequest ReturnRequest { get; set; } = null!;

        public Guid OrderItemId { get; set; }

        public Guid ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public ReturnItemStatus Status { get; set; } = ReturnItemStatus.Pending;
    }

    public enum ReturnStatus
    {
        Pending,
        Approved,
        Rejected,
        PartiallyApproved
    }

    public enum ReturnItemStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
