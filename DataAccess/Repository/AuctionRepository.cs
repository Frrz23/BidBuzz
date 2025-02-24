using DataAccess.Data;
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
        public async Task StartAuctionAsync()
        {
            var auctionsToStart = await _context.Auctions
                .Where(a => a.Status == AuctionStatus.Approved && a.StartTime <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var auction in auctionsToStart)
            {
                auction.Status = AuctionStatus.InAuction;
            }

            await _context.SaveChangesAsync();
        }


        public async Task EndAuctionAsync()
        {
            var auctionsToEnd = await _context.Auctions
                .Where(a => a.Status == AuctionStatus.InAuction && a.EndTime <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var auction in auctionsToEnd)
            {
                var highestBid = await _bidRepository.GetHighestBidAsync(auction.Id);
                auction.Status = (highestBid != null) ? AuctionStatus.Sold : AuctionStatus.Unsold;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RelistUnsoldItemsAsync()
        {
            var unsoldAuctions = await _context.Auctions
                .Where(a => a.Status == AuctionStatus.Unsold)
                .ToListAsync();

            foreach (var auction in unsoldAuctions)
            {
                auction.Status = AuctionStatus.Approved;  // Ready for next auction
                auction.StartTime = DateTime.UtcNow.AddDays(7);  // Schedule for next cycle
                auction.EndTime = auction.StartTime.GetValueOrDefault().AddDays(1); // 24-hour auction
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuctionVM>> GetAllForAuctionManagementAsync()
        {
            var auctions = await _context.Auctions.Include(a => a.Item).ToListAsync();
            var items = await _context.Items.ToListAsync();

            var auctionVMList = new List<AuctionVM>();

            foreach (var item in items)
            {
                var auction = auctions.FirstOrDefault(a => a.ItemId == item.Id);
                auctionVMList.Add(new AuctionVM
                {
                    Auction = auction ?? new Auction { ItemId = item.Id },  // If no auction, create an empty one
                    Item = item
                });
            }

            return auctionVMList;
        }



    }
}
