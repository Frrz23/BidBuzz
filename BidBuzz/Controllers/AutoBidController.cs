using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using DataAccess.Repository.IRepository;
using Models.ViewModels;
using Microsoft.AspNetCore.SignalR;
using BidBuzz.Hubs;
using System.Linq;
using Utility;

namespace BidBuzz.Controllers
{
    [Authorize]
    public class AutoBidController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<BidHub> _hubContext;

        public AutoBidController(IUnitOfWork unitOfWork, IHubContext<BidHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> SetAutoBid(int ItemId, decimal MaxBidAmount)
        {
            var itemId = ItemId;
            var maxBidAmount = MaxBidAmount;
            var increment = BiddingDefaults.Increment;

            if (maxBidAmount <= 0)
            {
                TempData["Error"] = "Maximum bid amount must be greater than zero.";
                return RedirectToAction("Details", "Home", new { itemId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var item = await _unitOfWork.Items.GetByIdAsync(itemId);
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);

            if (auction == null || auction.Status != AuctionStatus.InAuction)
            {
                TempData["Error"] = "The auction is not active.";
                return RedirectToAction("Details", "Home", new { itemId });
            }

            // Get current highest bid
            var bids = await _unitOfWork.Bids.GetBidsForAuctionAsync(auction.Id);
            var highestBid = bids.FirstOrDefault();
            var currentHighestAmount = highestBid?.Amount ?? item.StartingPrice;
            var conflict = await _unitOfWork.AutoBids.ExistsActiveAutoBidWithMaxAsync(auction.Id, maxBidAmount, userId);

            // Check if maximum bid is enough
            if (maxBidAmount <= currentHighestAmount)
            {
                TempData["Error"] = $"Your maximum bid must be higher than the current highest bid ({currentHighestAmount:C}).";
                return RedirectToAction("Details", "Home", new { itemId });
            }
            if (conflict)
            {
                TempData["Error"] =
                    $"Someone already has an active auto-bid at {maxBidAmount:C}. " +
                    "Please choose a different maximum.";
                return RedirectToAction("Details", "Home", new { itemId });
            }
            // Check if user already has an active auto bid for this auction
            var existingAutoBid = await _unitOfWork.AutoBids.GetActiveAutoBidForUserAsync(auction.Id, userId);

            if (existingAutoBid != null)
            {
                // Update existing auto bid
                existingAutoBid.MaxAmount = maxBidAmount;
            }
            else
            {
                // Create new auto bid
                var autoBid = new AutoBid
                {
                    AuctionId = auction.Id,
                    UserId = userId,
                    MaxAmount = maxBidAmount,
                    IsActive = true
                };

                await _unitOfWork.AutoBids.AddAsync(autoBid);
            }

            // If the user is not the current highest bidder, place a bid for them
            if (highestBid == null || highestBid.UserId != userId)
            {
                // Place initial bid with minimum increment
                var initialBidAmount = Math.Min(maxBidAmount, currentHighestAmount + increment);
                var newBid = new Bid
                {
                    Amount = initialBidAmount,
                    AuctionId = auction.Id,
                    UserId = userId,
                    BidTime = DateTime.Now
                };

                await _unitOfWork.Bids.AddAsync(newBid);
            }

            await _unitOfWork.CompleteAsync();

            // Process any competing auto bids
            bids = await _unitOfWork.Bids.GetBidsForAuctionAsync(auction.Id);
            highestBid = bids.FirstOrDefault(); // Get updated highest bid

            if (highestBid != null)
            {
                await _unitOfWork.AutoBids.ProcessAutoBidsAsync(
                    auction.Id,
                    highestBid.Amount,
                    highestBid.UserId);

                await _unitOfWork.CompleteAsync();
            }

            // Notify clients about bid updates
            await _hubContext.Clients.Group($"item-{itemId}").SendAsync("ReceiveBidUpdate", itemId);

            // Send auto-bid update notification
            await _hubContext.Clients.Group($"item-{itemId}").SendAsync("ReceiveAutoBidUpdate", itemId);

            TempData["Success"] = "Your auto bid has been set!";
            return RedirectToAction("Details", "Home", new { itemId });
        }

        [HttpPost]
        public async Task<IActionResult> CancelAutoBid(int itemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);

            if (auction == null)
            {
                TempData["Error"] = "Auction not found.";
                return RedirectToAction("Details", "Home", new { itemId });
            }

            var autoBid = await _unitOfWork.AutoBids.GetActiveAutoBidForUserAsync(auction.Id, userId);

            if (autoBid != null)
            {
                await _unitOfWork.AutoBids.DeactivateAsync(autoBid.Id);
                await _unitOfWork.CompleteAsync();

                // Send auto-bid update notification
                await _hubContext.Clients.Group($"item-{itemId}").SendAsync("ReceiveAutoBidUpdate", itemId);

                TempData["Success"] = "Your auto bid has been canceled.";
            }
            else
            {
                TempData["Error"] = "No active auto bid found.";
            }

            return RedirectToAction("Details", "Home", new { itemId });
        }

        [HttpGet]
        public async Task<IActionResult> GetAutoBidStatus(int itemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);

            if (auction == null)
            {
                return Json(new { hasActiveAutoBid = false });
            }

            var autoBid = await _unitOfWork.AutoBids.GetActiveAutoBidForUserAsync(auction.Id, userId);

            if (autoBid != null)
            {
                return Json(new
                {
                    hasActiveAutoBid = true,
                    maxAmount = autoBid.MaxAmount,
                    maxAmountFormatted = autoBid.MaxAmount.ToString("C")
                });
            }

            return Json(new { hasActiveAutoBid = false });
        }
    }
}