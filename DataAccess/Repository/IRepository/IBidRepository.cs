using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repository.IRepository
{
    public interface IBidRepository : IRepository<Bid>
    {
        Task <IEnumerable<Bid>> GetBidsByAuctionIdAsync(int auctionId);
        Task<Bid> GetHighestBidAsync(int auctionId);
    }

}


//Task==asyncronous Operation that may complete in future 
