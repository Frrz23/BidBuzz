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
    public class ItemRepository : Repository<Item>,IItemRepository
    {
        private readonly ApplicationDbContext _context;

        public ItemRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Item>> GetItemsByStatusAsync(AuctionStatus status)
        {
            return await _context.Items.Where(i => i.Status == status).ToListAsync();
        }



    }
}
