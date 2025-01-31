using DataAccess.Data;
using DataAccess.Repositary;
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
        public IRepository<Category> Categories { get; private set; }
        public IRepository<Item> Items { get; private set; }
        public IRepository<Bid> Bids { get; private set; }
        public IRepository<Auction> Auctions { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Categories = new Repository<Category>(_context);
            Items = new Repository<Item>(_context);
            Bids = new Repository<Bid>(_context);
            Auctions = new Repository<Auction>(_context);
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
