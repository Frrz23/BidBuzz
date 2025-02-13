using DataAccess.Data;
using DataAccess.Repository.IRepository;
using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public ICategoryRepository Categories { get; private set; }
        public IItemRepository Items { get; private set; }
        public IBidRepository Bids { get; private set; }
        public IAuctionRepository Auctions { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Categories = new CategoryRepository(_context);
            Items = new ItemRepository(_context);
            Bids = new BidRepository(_context);
            Auctions = new AuctionRepository(_context, new BidRepository(_context));
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


}
