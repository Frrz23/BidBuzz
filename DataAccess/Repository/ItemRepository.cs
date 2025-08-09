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
    public class ItemRepository : Repository<Item>,IItemRepository
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





    }
}
