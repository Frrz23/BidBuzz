using DataAccess.Repository.IRepository;
using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace DataAccess.Repositary
{
    public interface IAuctionRepository : IRepository<Auction>
    {
        Task<IEnumerable<Auction>> GetAuctionsByStatusAsync(AuctionStatus status);
        Task<IEnumerable<Auction>> GetUpcomingAuctionsAsync();
        Task<IEnumerable<Auction>> GetActiveAuctionsAsync();
        Task StartAuctionAsync(int auctionId);
        Task EndAuctionAsync(int auctionId);
        Task RelistUnsoldItemsAsync();



    }

}
