namespace AlMadina.Domain.Entities
{
    public class StockMovement
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? TotalCost { get; set; }
        public string? Supplier { get; set; }
        public StockMovementType Type { get; set; }
        public string? Notes { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }

    public enum StockMovementType
    {
        In,
        Out,
        Adjust
    }
}
