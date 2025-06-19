using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BidBuzz.Hubs;
using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Models;
using Models.ViewModels;
using Utility;

namespace BidBuzz.Controllers
{
    [Authorize]
    public class BidController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<BidHub> _hubContext;

        public BidController(IUnitOfWork unitOfWork, IHubContext<BidHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> PlaceBid(ItemVM model)
        {
            var itemId = model.ItemId;
            var bidAmount = model.BidModel.BidAmount;
            var enableAutoBid = model.BidModel.EnableAutoBid;
            var maxBidAmount = model.BidModel.MaxBidAmount;

            var item = await _unitOfWork.Items.GetByIdAsync(itemId, includeProperties: "Category");
            if (item == null)
            {
                TempData["Error"] = "Item not found!";
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (item.UserId == userId)
            {
                TempData["Error"] = "You cannot bid on your own item.";
                return RedirectToAction("Details", new { itemId = model.ItemId });
            }

            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);

            if (auction == null || auction.Status != AuctionStatus.InAuction)
            {
                TempData["Error"] = "The auction is not active.";
                ModelState.AddModelError(string.Empty, "The auction is not active.");
                var bids = await _unitOfWork.Bids.GetBidsForAuctionAsync(auction?.Id ?? 0);
                model.Item = item;
                model.AuctionStatus = auction?.Status ?? AuctionStatus.PendingApproval;
                model.BidList = bids;
                return View("~/Views/Home/Details.cshtml", model);
            }

            var bidsActive = await _unitOfWork.Bids.GetBidsForAuctionAsync(auction.Id);
            var highestBid = bidsActive.FirstOrDefault();
            var highestBidAmount = highestBid?.Amount ?? item.StartingPrice;

            if (bidAmount <= highestBidAmount)
            {
                TempData["Error"] = "Your bid must be higher than the current highest bid.";
                ModelState.AddModelError("BidModel.BidAmount", "Your bid must be higher than the current highest bid.");
                model.Item = item;
                model.AuctionStatus = auction.Status;
                model.BidList = bidsActive;
                return View("~/Views/Home/Details.cshtml", model);
            }

            // Check for auto bidding
            if (enableAutoBid && maxBidAmount > 0)
            {
                if (maxBidAmount < bidAmount)
                {
                    TempData["Error"] = "Your maximum bid amount must be at least equal to your initial bid.";
                    ModelState.AddModelError("BidModel.MaxBidAmount", "Max bid must be at least equal to your initial bid.");
                    model.Item = item;
                    model.AuctionStatus = auction.Status;
                    model.BidList = bidsActive;
                    return View("~/Views/Home/Details.cshtml", model);
                }

                // Create or update auto bid
                var existingAutoBid = await _unitOfWork.AutoBids.GetActiveAutoBidForUserAsync(auction.Id, userId);

                if (existingAutoBid != null)
                {
                    existingAutoBid.MaxAmount = maxBidAmount;
                }
                else
                {
                    var autoBid = new AutoBid
                    {
                        AuctionId = auction.Id,
                        UserId = userId,
                        MaxAmount = maxBidAmount,
                        IsActive = true
                    };
                    await _unitOfWork.AutoBids.AddAsync(autoBid);
                }
            }

            // Create the bid
            var newBid = new Bid
            {
                Amount = bidAmount,
                AuctionId = auction.Id,
                UserId = userId,
                BidTime = DateTime.Now
            };

            await _unitOfWork.Bids.AddAsync(newBid);
            await _unitOfWork.CompleteAsync();

            // Process auto bids if there are any
            if (highestBid != null && highestBid.UserId != userId)
            {
                await _unitOfWork.AutoBids.ProcessAutoBidsAsync(auction.Id, bidAmount, userId);
                await _unitOfWork.CompleteAsync();
            }

            // Notify clients
            await _hubContext.Clients.Group($"item-{itemId}").SendAsync("ReceiveBidUpdate", itemId);

            TempData["Success"] = "Your bid has been placed successfully!";
            return RedirectToAction("Details", "Home", new { itemId });
        }

        public async Task<IActionResult> Top5(int itemId)
        {
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);
            if (auction == null)
                return NotFound();

            var bids = await _unitOfWork.Bids.GetBidsForAuctionAsync(auction.Id);
            var top5 = bids.Take(5).ToList();

            return PartialView("_Top5Partial", top5);
        }


        [HttpGet]
        public async Task<IActionResult> GetHighest(int itemId)
        {
            // 1) Find the auction for this item
            var auction = await _unitOfWork.Auctions
                .GetFirstOrDefaultAsync(a => a.ItemId == itemId);

            if (auction == null)
                return NotFound();

            // 2) Pull all bids for that auction, ordered descending so the first is highest
            var bids = await _unitOfWork.Bids.GetBidsForAuctionAsync(auction.Id);
            var highest = bids.FirstOrDefault()?.Amount
                          ?? (await _unitOfWork.Items.GetByIdAsync(itemId)).StartingPrice;

            // 3) Compute the next minimum
            var nextMin = highest + BiddingDefaults.Increment;

            // 4) Return both as formatted strings
            return Json(new
            {
                highestFormatted = highest > 0
                    ? highest.ToString("C")
                    : "No bids yet",
                nextMinFormatted = nextMin.ToString("C")
            });
        }


    }
}