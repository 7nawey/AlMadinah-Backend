namespace AlMadina.Application.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int LowStock { get; set; }
    }

    public class BestSellingProductDto
    {
        public Guid ProductId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class MostProfitableProductDto
    {
        public Guid ProductId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Profit { get; set; }
    }

    public class RecentOrderDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalPrice { get; set; }
        public int Status { get; set; }
        public bool IsPaid { get; set; }
        public string CustomerPhone { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
    }

    public class SalesChartDataDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }
}
