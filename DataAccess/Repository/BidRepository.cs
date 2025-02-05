using DataAccess.Data;
using DataAccess.Repositary;
using DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public class BidRepository : Repository<Bid>,IBidRepository
    {
        private readonly ApplicationDbContext _context;

        public BidRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Bid>> GetBidsByAuctionIdAsync(int auctionId)
        {
            return await _context.Bids.Where(i => i.AuctionId == auctionId).OrderByDescending(i=>i.Amount).ToListAsync();
        }
        public async Task<Bid> GetHighestBidAsync(int auctionId)
        {
            return await _context.Bids
                .Where(b => b.AuctionId == auctionId)  
                .OrderByDescending(b => b.Amount) 
                .FirstOrDefaultAsync(); 
        }


    }
}
