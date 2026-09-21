namespace AlMadina.Application.DTOs
{
    public class DealDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
    }

    public class CreateDealDto
    {
        public Guid ProductId { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class UpdateDealDto
    {
        public Guid Id { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
