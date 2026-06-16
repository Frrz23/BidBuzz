using DataAccess.Data;
using DataAccess.Repository;
using DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace DataAccess.Repository
{
    public class ItemRepository : Repository<Item>, IItemRepository
    {
        private readonly ApplicationDbContext _context;

        public ItemRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Item>> GetItemsByStatusAsync(AuctionStatus status)
        {
            return await _context.Items
                .Include(i => i.Auctions)
                .Where(i => i.Auctions.Any(a => a.Status == status))
                .ToListAsync();
        }

        public async Task AddItemAsync(Item item)
        {
            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            var auction = new Auction
            {
                ItemId = item.Id,
                Status = AuctionStatus.PendingApproval,
                StartTime = null,
                EndTime = null
            };

            _context.Auctions.Add(auction);
            await _context.SaveChangesAsync();
        }

        public async Task<Item> GetByIdAsNoTrackingAsync(int id)
        {
            return await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        }

        /// <summary>
        /// Returns recommended items using a Popularity + Recency Scoring algorithm.
        ///
        /// Scoring formula per item:
        ///   Score = (Number of Bids x 2) + Recency Bonus
        ///
        /// Recency Bonus (based on how recently the auction started):
        ///   Listed within 1 day  -> +10 points
        ///   Listed within 3 days -> +6  points
        ///   Listed within 7 days -> +3  points
        ///   Older than 7 days    -> +0  points
        ///
        /// Items are ranked by score descending so the most popular and
        /// most recently listed items from the same category appear first.
        /// This replaces the previous approach of simply taking any 4 items
        /// from the same category with no ordering or relevance ranking.
        /// </summary>
        public async Task<IEnumerable<Item>> GetRecommendedItemsAsync(int itemId, int categoryId, int count = 4)
        {
            // Step 1: Fetch all candidate items from the same category in ONE query
            var candidates = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Auctions)
                    .ThenInclude(a => a.Bids)
                .Where(i => i.CategoryId == categoryId
                         && i.Id != itemId
                         && i.Auctions.Any(a => a.Status == AuctionStatus.InAuction
                                             || a.Status == AuctionStatus.Approved))
                .ToListAsync();

            var now = DateTime.UtcNow;

            // Step 2: Score each candidate in memory
            var scored = candidates.Select(item =>
            {
                var auction      = item.Auctions.FirstOrDefault();
                var bidCount     = auction?.Bids?.Count ?? 0;
                var startTime    = auction?.StartTime ?? now;
                var daysOld      = (now - startTime).TotalDays;

                // Recency bonus: newer items score higher
                int recencyBonus = daysOld <= 1 ? 10
                                 : daysOld <= 3 ? 6
                                 : daysOld <= 7 ? 3
                                 : 0;

                // Final score: popularity (bids) weighted more than recency
                int score = (bidCount * 2) + recencyBonus;

                return new { Item = item, Score = score };
            })
            .OrderByDescending(x => x.Score)   // Step 3: Rank best first
            .Take(count)
            .Select(x => x.Item)
            .ToList();

            return scored;
        }
    }
}
