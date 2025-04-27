using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repository.IRepository
{
    // IAutoBidRepository.cs
    public interface IAutoBidRepository : IRepository<AutoBid>
    {
        Task<AutoBid> GetActiveAutoBidForUserAsync(int auctionId, string userId);
        Task DeactivateAsync(int autoBidId);
        Task ProcessAutoBidsAsync(int auctionId, decimal currentHighestBid, string currentHighestBidUserId);

    }


}
