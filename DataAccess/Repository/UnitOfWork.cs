using DataAccess.Data;
using DataAccess.Repository.IRepository;
using DataAccess.Repository;
using Microsoft.Extensions.Logging;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuctionRepository> _logger;  // Add logger

    public ICategoryRepository Categories { get; private set; }
    public IItemRepository Items { get; private set; }
    public IBidRepository Bids { get; private set; }
    public IAuctionRepository Auctions { get; private set; }
    public IAuctionScheduleRepository AuctionSchedules { get; private set; }


    public UnitOfWork(ApplicationDbContext context, ILogger<AuctionRepository> logger)
    {
        _context = context;
        _logger = logger; // Store logger instance

        Categories = new CategoryRepository(_context);
        Items = new ItemRepository(_context);
        Bids = new BidRepository(_context);
        Auctions = new AuctionRepository(_context); // Pass logger
        AuctionSchedules=new AuctionScheduleRepository(_context);


    }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
