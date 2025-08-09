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



    public UnitOfWork(ApplicationDbContext context, ILogger<AuctionRepository> logger)
    {
        _context = context;
        _logger = logger; 

        Categories = new CategoryRepository(_context);
        Items = new ItemRepository(_context);
        Bids = new BidRepository(_context);
        Auctions = new AuctionRepository(_context); 
        AuctionSchedules=new AuctionScheduleRepository(_context);
        AutoBids = new AutoBidRepository(_context, Bids); 


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
