namespace AlMadina.Application.DTOs
{
    public class UpdateCategoryOrderDto
    {
        public Guid CategoryId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; }
    }

    public class UpdateFeaturedProductDto
    {
        public Guid ProductId { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPinned { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class HomePageConfigDto
    {
        public List<CategoryOrderDto> Categories { get; set; } = new();
        public List<FeaturedProductDto> FeaturedProducts { get; set; } = new();
    }

    public class CategoryOrderDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; }
    }

    public class FeaturedProductDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPinned { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class FeaturedProductOrderItem
    {
        public Guid ProductId { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class UpdateFeaturedProductsOrderDto
    {
        public List<FeaturedProductOrderItem> Items { get; set; } = new();
    }
}
