using DataAccess.Repository.IRepository;
using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositary
{
    public interface IItemRepository : IRepository<Item>
    {
        IEnumerable<Item> GetApprovedItems();
        IEnumerable<Item> GetItemsPendingApproval();
        IEnumerable<Item> GetItemsInAuction();
    }

}
