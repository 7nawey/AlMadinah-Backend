using AlMadina.Domain.Entities;

namespace AlMadina.Application.DTOs
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal TotalPrice { get; set; }
        public OrderStatus Status { get; set; }
        public OrderType Type { get; set; }
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal DeliveryFee { get; set; }
        public bool IsPaid { get; set; }
        public bool IsInStore { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderResultDto
    {
        public Guid OrderId { get; set; }
        public decimal Total { get; set; }
        public int Status { get; set; }
        public OrderType Type { get; set; }
        public decimal DeliveryFee { get; set; }
    }

    public class CreateOrderItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        public Guid OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public bool IsPaid { get; set; }
    }

    public class CreateOrderDto
    {
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = new();
        public bool IsInStore { get; set; }
    }

    public class CreateInStoreOrderDto
    {
        public List<CreateOrderItemDto> Items { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class OrderItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
