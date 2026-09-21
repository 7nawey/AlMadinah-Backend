using AlMadina.Application.DTOs;

namespace AlMadina.Application.Interfaces.Services
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync();
        Task<IEnumerable<BestSellingProductDto>> GetBestSellingAsync(int top = 10);
        Task<IEnumerable<MostProfitableProductDto>> GetMostProfitableAsync(int top = 10);
        Task<IEnumerable<RecentOrderDto>> GetRecentOrdersAsync(int top = 10);
        Task<IEnumerable<SalesChartDataDto>> GetSalesChartAsync(int days = 30);
    }
}
