namespace AlMadina.Application.DTOs
{
    public class StockMovementDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? TotalCost { get; set; }
        public string? Supplier { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime Date { get; set; }
    }

    public class CreateStockMovementDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public string? Supplier { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateStockMovementWithNewProductDto
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public int InitialStock { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal? UnitCost { get; set; }
        public string? Supplier { get; set; }
        public string? Notes { get; set; }
    }
}
