using DataAccess.Repository.IRepository;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace DataAccess.Repository
{
    public interface IItemRepository : IRepository<Item>
    {
        Task <IEnumerable<Item>> GetItemsByStatusAsync(AuctionStatus status);
        Task AddItemAsync(Item item);
        Task<Item> GetByIdAsNoTrackingAsync(int id);

    }

}
