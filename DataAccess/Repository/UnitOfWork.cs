using DataAccess.Data;
using DataAccess.Repository.IRepository;
using DataAccess.Repository;
using Microsoft.Extensions.Logging;
using Models;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuctionRepository> _logger;  

    public ICategoryRepository Categories { get; private set; }
    public IItemRepository Items { get; private set; }
    public IBidRepository Bids { get; private set; }
    public IAuctionRepository Auctions { get; private set; }
    public IAuctionScheduleRepository AuctionSchedules { get; private set; }
    public IAutoBidRepository AutoBids { get; private set; }
    public IRepository<Notification> Notifications { get; private set; }
    public IRepository<Review> Reviews { get; private set; }


    public UnitOfWork(ApplicationDbContext context, ILogger<AuctionRepository> logger, IAuctionScheduleRepository scheduleRepo)
    {
        _context = context;
        _logger = logger;

        Categories = new CategoryRepository(_context);
        Items = new ItemRepository(_context);
        Bids = new BidRepository(_context);
        Auctions = new AuctionRepository(_context, scheduleRepo);
        AuctionSchedules = scheduleRepo;
        AutoBids = new AutoBidRepository(_context, Bids);
        Notifications = new Repository<Notification>(_context);
        Reviews = new Repository<Review>(_context);
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
