﻿using DataAccess.Data;
using DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Models;
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
        public async Task<List<Bid>> GetBidsForAuctionAsync(int auctionId)
        {
            return await _context.Bids
                .Where(b => b.AuctionId == auctionId)
                .Include(b => b.User) // Ensure the user is loaded
                .OrderByDescending(b => b.Amount)
                .ToListAsync();
        }


    }
}
