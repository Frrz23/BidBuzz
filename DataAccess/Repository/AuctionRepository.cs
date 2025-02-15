﻿using DataAccess.Data;
using DataAccess.Repository;
using DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Models.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace DataAccess.Repository
{
    public class AuctionRepository : Repository<Auction>, IAuctionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IBidRepository _bidRepository;

        public AuctionRepository(ApplicationDbContext context, IBidRepository bidRepository) : base(context)
        {
            _context = context;
            _bidRepository = bidRepository;
        }

        public async Task EndAuctionAsync(int auctionId)
        {
            var auction = await _context.Auctions.FindAsync(auctionId);
            if (auction == null) return;

            if (auction.EndTime <= DateTime.UtcNow)
            {
                var highestBid = await _bidRepository.GetHighestBidAsync(auctionId); 

                auction.Status = (highestBid != null) ? AuctionStatus.Sold : AuctionStatus.Unsold;
                _context.Auctions.Update(auction);
                await _context.SaveChangesAsync();
            }
        }


        public async Task<IEnumerable<Auction>> GetActiveAuctionsAsync()
        {
            return await _context.Auctions.Where(i => i.Status == AuctionStatus.InAuction && i.StartTime <= DateTime.UtcNow && i.EndTime >= DateTime.UtcNow).ToListAsync();
        }

        public async Task<IEnumerable<Auction>> GetAuctionsByStatusAsync(AuctionStatus status)
        {
            return await _context.Auctions.Where(i => i.Status == status).ToListAsync();
        }



        public async Task<IEnumerable<Auction>> GetUpcomingAuctionsAsync()
        {
            return await _context.Auctions.Where(i => i.Status == AuctionStatus.Approved && i.StartTime > DateTime.UtcNow).ToListAsync();
        }

        public async Task StartAuctionAsync(int auctionId)
        {
            var auction = await _context.Auctions.FindAsync(auctionId);
            if (auction == null) return;  

            if (auction.StartTime <= DateTime.UtcNow)  
            {
                auction.Status = AuctionStatus.InAuction; 
                _context.Auctions.Update(auction);
                await _context.SaveChangesAsync();  
            }
        }
        public async Task RelistUnsoldItemsAsync()
        {
            var unsoldAuctions = await _context.Auctions
                .Where(a => a.Status == AuctionStatus.Unsold)
                .ToListAsync();

            foreach (var auction in unsoldAuctions)
            {
                auction.Status = AuctionStatus.Approved;  // Ready for next auction
                auction.StartTime = DateTime.UtcNow.AddDays(7);  // Example: Relist for the next week
                auction.EndTime = auction.StartTime.AddDays(7);
            }

            await _context.SaveChangesAsync();
        }



    }
}
