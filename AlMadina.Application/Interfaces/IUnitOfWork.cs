using AlMadina.Domain.Entities;

namespace AlMadina.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Products { get; }
        IGenericRepository<Category> Categories { get; }
        IGenericRepository<Order> Orders { get; }
        IGenericRepository<OrderItem> OrderItems { get; }
        IGenericRepository<StockMovement> StockMovements { get; }
        IGenericRepository<ReturnRequest> ReturnRequests { get; }
        IGenericRepository<Deal> Deals { get; }
        IGenericRepository<PaymentRecord> PaymentRecords { get; }

        Task<int> SaveChangesAsync();
    }
}
