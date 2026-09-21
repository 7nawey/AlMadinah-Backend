using AlMadina.Application.Interfaces;
using AlMadina.Domain.Entities;
using AlMadina.Infrastructure.Persistence;

namespace AlMadina.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IProductRepository Products { get; }
        public IGenericRepository<Category> Categories { get; }
        public IGenericRepository<Order> Orders { get; }
        public IGenericRepository<OrderItem> OrderItems { get; }
        public IGenericRepository<StockMovement> StockMovements { get; }
        public IGenericRepository<ReturnRequest> ReturnRequests { get; }
        public IGenericRepository<Deal> Deals { get; }
        public IGenericRepository<PaymentRecord> PaymentRecords { get; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Products = new ProductRepository(_context);
            Categories = new GenericRepository<Category>(_context);
            Orders = new GenericRepository<Order>(_context);
            OrderItems = new GenericRepository<OrderItem>(_context);
            StockMovements = new GenericRepository<StockMovement>(_context);
            ReturnRequests = new GenericRepository<ReturnRequest>(_context);
            Deals = new GenericRepository<Deal>(_context);
            PaymentRecords = new GenericRepository<PaymentRecord>(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
