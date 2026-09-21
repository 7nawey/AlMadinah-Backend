using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces;
using AlMadina.Application.Interfaces.Services;
using AlMadina.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlMadina.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            var totalProducts = await _unitOfWork.Products.CountAsync();
            var totalCategories = await _unitOfWork.Categories.CountAsync();
            var totalOrders = await _unitOfWork.Orders.CountAsync();

            var totalRevenue = await _unitOfWork.Orders.GetAllQueryable()
                .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

            var pendingOrders = await _unitOfWork.Orders.GetAllQueryable()
                .CountAsync(o => o.Status == OrderStatus.PendingPayment);

            var lowStock = await _unitOfWork.Products.GetAllQueryable()
                .CountAsync(p => p.StockQuantity < 10);

            return new DashboardStatsDto
            {
                TotalProducts = totalProducts,
                TotalCategories = totalCategories,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                PendingOrders = pendingOrders,
                LowStock = lowStock
            };
        }

        public async Task<IEnumerable<BestSellingProductDto>> GetBestSellingAsync(int top = 10)
        {
            var returnedByProduct = await _unitOfWork.ReturnRequests.GetAllQueryable()
                .Where(r => r.Status == ReturnStatus.Approved || r.Status == ReturnStatus.PartiallyApproved)
                .SelectMany(r => r.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ReturnedQty = g.Sum(i => i.Quantity),
                    ReturnedRevenue = g.Sum(i => i.UnitPrice * i.Quantity)
                })
                .ToDictionaryAsync(x => x.ProductId, x => x);

            // Only count items from actually paid/delivered orders —
            // cancelled or unpaid orders must not inflate the numbers.
            var items = await _unitOfWork.OrderItems.GetAllQueryable()
                .Include(i => i.Product)
                .Where(i => i.Order.Status == OrderStatus.Paid ||
                            i.Order.Status == OrderStatus.Delivered)
                .GroupBy(i => new { i.ProductId, i.Product.NameAr, i.Product.ImageUrl })
                .Select(g => new BestSellingProductDto
                {
                    ProductId = g.Key.ProductId,
                    NameAr = g.Key.NameAr,
                    ImageUrl = g.Key.ImageUrl,
                    TotalSold = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.TotalPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(top)
                .ToListAsync();

            foreach (var item in items)
            {
                if (returnedByProduct.TryGetValue(item.ProductId, out var ret))
                {
                    item.TotalSold = Math.Max(0, item.TotalSold - ret.ReturnedQty);
                    item.TotalRevenue = Math.Max(0, item.TotalRevenue - ret.ReturnedRevenue);
                }
            }

            return items.Where(i => i.TotalSold > 0).ToList();
        }

        public async Task<IEnumerable<MostProfitableProductDto>> GetMostProfitableAsync(int top = 10)
        {
            var returnedByProduct = await _unitOfWork.ReturnRequests.GetAllQueryable()
                .Where(r => r.Status == ReturnStatus.Approved || r.Status == ReturnStatus.PartiallyApproved)
                .SelectMany(r => r.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ReturnedQty = g.Sum(i => i.Quantity),
                    ReturnedRevenue = g.Sum(i => i.UnitPrice * i.Quantity)
                })
                .ToDictionaryAsync(x => x.ProductId, x => x);

            var items = await _unitOfWork.OrderItems.GetAllQueryable()
                .Include(i => i.Product)
                .Where(i => i.Order.Status == OrderStatus.Paid ||
                            i.Order.Status == OrderStatus.Delivered)
                .GroupBy(i => new { i.ProductId, i.Product.NameAr, i.Product.ImageUrl, i.Product.CostPrice })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.NameAr,
                    g.Key.ImageUrl,
                    g.Key.CostPrice,
                    TotalSold = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.TotalPrice),
                    TotalCost = g.Sum(i => g.Key.CostPrice * i.Quantity)
                })
                .OrderByDescending(x => x.TotalRevenue - x.TotalCost)
                .Take(top)
                .ToListAsync();

            return items.Select(x =>
            {
                var ret = returnedByProduct.TryGetValue(x.ProductId, out var r) ? r : null;
                var returnedQty = ret?.ReturnedQty ?? 0;
                var returnedRevenue = ret?.ReturnedRevenue ?? 0;
                var returnedCost = returnedQty * x.CostPrice;

                return new MostProfitableProductDto
                {
                    ProductId = x.ProductId,
                    NameAr = x.NameAr,
                    ImageUrl = x.ImageUrl,
                    TotalSold = Math.Max(0, x.TotalSold - returnedQty),
                    TotalRevenue = Math.Max(0, x.TotalRevenue - returnedRevenue),
                    TotalCost = Math.Max(0, x.TotalCost - returnedCost),
                    Profit = Math.Max(0, (x.TotalRevenue - returnedRevenue) - (x.TotalCost - returnedCost))
                };
            });
        }

        public async Task<IEnumerable<RecentOrderDto>> GetRecentOrdersAsync(int top = 10)
        {
            var orders = await _unitOfWork.Orders.GetAllQueryable()
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Take(top)
                .Select(o => new RecentOrderDto
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    TotalPrice = o.TotalPrice,
                    Status = (int)o.Status,
                    IsPaid = o.IsPaid,
                    CustomerPhone = o.CustomerPhone,
                    ItemsCount = o.Items.Count
                })
                .ToListAsync();

            return orders;
        }

        public async Task<IEnumerable<SalesChartDataDto>> GetSalesChartAsync(int days = 30)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            var data = await _unitOfWork.Orders.GetAllQueryable()
                .Where(o => o.CreatedAt >= startDate &&
                       (o.Status == OrderStatus.Paid ||
                        o.Status == OrderStatus.Delivered))
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.TotalPrice),
                    Orders = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return data.Select(x => new SalesChartDataDto
            {
                Date = x.Date.ToString("yyyy-MM-dd"),
                Revenue = x.Revenue,
                Orders = x.Orders
            });
        }
    }
}
