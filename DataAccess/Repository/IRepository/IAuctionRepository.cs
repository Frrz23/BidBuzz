using Models;
using Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace DataAccess.Repository.IRepository
{
    public interface IAuctionRepository : IRepository<Auction>
    {
        Task<IEnumerable<Auction>> GetAuctionsByStatusAsync(AuctionStatus status);
        Task<IEnumerable<Auction>> GetUpcomingAuctionsAsync();
        Task<IEnumerable<Auction>> GetActiveAuctionsAsync();
        Task StartAuctionAsync();
        Task EndAuctionAsync();
        Task RelistUnsoldItemsAsync();
        Task<IEnumerable<AuctionVM>> GetAllForAuctionManagementAsync();



    }

}
