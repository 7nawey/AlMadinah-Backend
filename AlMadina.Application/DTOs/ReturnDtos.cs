namespace AlMadina.Application.DTOs
{
    public class ReturnRequestDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AdminNotes { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public List<ReturnItemDto> Items { get; set; } = new();
    }

    public class ReturnItemDto
    {
        public Guid Id { get; set; }
        public Guid OrderItemId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateReturnRequestDto
    {
        public Guid OrderId { get; set; }
        public List<CreateReturnItemDto> Items { get; set; } = new();
    }

    public class CreateReturnItemDto
    {
        public Guid OrderItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class ProcessReturnDto
    {
        public Guid ReturnRequestId { get; set; }
        public string Status { get; set; } = string.Empty; // Approved, Rejected, PartiallyApproved
        public string? AdminNotes { get; set; }
        public List<ProcessReturnItemDto>? ItemDecisions { get; set; }
    }

    public class ProcessReturnItemDto
    {
        public Guid ReturnItemId { get; set; }
        public string Status { get; set; } = string.Empty; // Approved, Rejected
    }
}
