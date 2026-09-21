using Microsoft.AspNetCore.Http;

namespace AlMadina.Application.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }

        public string NameAr { get; set; }

        public string NameEn { get; set; }

        public string? Barcode { get; set; }

        public decimal Price { get; set; }

        public decimal CostPrice { get; set; }

        public int StockQuantity { get; set; }

        public Guid? CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public bool IsFeatured { get; set; }

        public decimal? DiscountPercentage { get; set; }

        /// <summary>
        /// Effective customer price after applying the active deal (or the product's own
        /// date-bounded discount). Null when no active discount exists; use Price then.
        /// </summary>
        public decimal? FinalPrice { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateProductDto
    {
        public string NameAr { get; set; }

        public string? NameEn { get; set; }

        public string? Barcode { get; set; }

        public decimal Price { get; set; }

        public decimal CostPrice { get; set; }

        public int StockQuantity { get; set; }

        public string? CategoryName { get; set; }

        public IFormFile? Image { get; set; }
    }

    public class UpdateProductDto
    {
        public Guid Id { get; set; }

        public string NameAr { get; set; }

        public string? NameEn { get; set; }

        public string? Barcode { get; set; }

        public decimal Price { get; set; }

        public decimal CostPrice { get; set; }

        public int StockQuantity { get; set; }

        public string? CategoryName { get; set; }

        public bool IsActive { get; set; }

        public IFormFile? Image { get; set; }
    }

    public class PaginationParams
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }


}