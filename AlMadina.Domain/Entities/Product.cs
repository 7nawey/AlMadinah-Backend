namespace AlMadina.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;

        /// <summary>
        /// Search-optimized form of NameAr (diacritics/alef variants folded).
        /// Maintained by the application layer; not edited by users directly.
        /// </summary>
        public string? NameArNormalized { get; set; }
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public int StockQuantity { get; set; }
        public Guid? CategoryId { get; set; }
        public Category? Category { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }
        public bool IsPinned { get; set; }
        public int DisplayOrder { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? DiscountStartDate { get; set; }
        public DateTime? DiscountEndDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<OrderItem>? OrderItems { get; set; }
        public ICollection<StockMovement>? StockMovements { get; set; }
    }
}
