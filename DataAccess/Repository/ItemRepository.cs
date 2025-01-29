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
    public class ItemRepository : Repository<Item>,IItemRepository
    {
        private readonly ApplicationDbContext _context;

        public ItemRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        public IEnumerable<Item> GetApprovedItems()
        {
            return _context.Items.Where(i => i.Status == "Approved").ToList();
        }

        public IEnumerable<Item> GetItemsPendingApproval()
        {
            return _context.Items.Where(i => i.Status == "PendingApproval").ToList();
        }

        public IEnumerable<Item> GetItemsInAuction()
        {
            return _context.Items.Where(i => i.Status == "InAuction").ToList();
        }


    }
}
