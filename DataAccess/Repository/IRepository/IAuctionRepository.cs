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

        Task<List<Auction>> GetAuctionsByStatusAsync(AuctionStatus status);
        Task<Auction?> GetAuctionWithHighestBidAsync(int itemId);
        Task CancelAuctionAsync(int auctionId);
        Task<Auction?> GetAuctionByItemIdAsync(int itemId);
        Task<List<Auction>> GetCancelledAuctionsAsync();
        Task StartAuctionAsync();
        Task EndAuctionAsync();
        Task RelistUnsoldItemsAsync();


    }

}
