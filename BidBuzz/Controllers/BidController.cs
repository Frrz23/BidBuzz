using System.Security.Claims;
using System.Threading.Tasks;
using BidBuzz.Hubs;
using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Models;
using Models.ViewModels;
using NuGet.Packaging.Signing;
using Utility;

namespace BidBuzz.Controllers
{
    [Authorize]
    public class BidController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public BidController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        public async Task<IActionResult> PlaceBid(ItemVM model)
        {
            var itemId = model.ItemId;
            var bidAmount = model.BidModel.BidAmount;


            var item = await _unitOfWork.Items.GetByIdAsync(itemId, includeProperties: "Category");
            if (item == null)
            {
                TempData["Error"] = "Item not found!";
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);
            if (auction == null || auction.Status != AuctionStatus.InAuction)
            {
                TempData["Error"] = "The auction is not active.";
                ModelState.AddModelError(string.Empty, "The auction is not active.");

                var bids = await _unitOfWork.Bids.GetBidsForAuctionAsync(auction?.Id ?? 0); // safe fallback
                model.Item = item;
                model.AuctionStatus = auction?.Status ?? AuctionStatus.PendingApproval;
                model.BidList = bids; // ✅ ADD THIS

                return View("~/Views/Home/Details.cshtml", model);
            }

            var bidsActive = await _unitOfWork.Bids.GetBidsForAuctionAsync(auction.Id);
            var highestBidAmount = bidsActive.FirstOrDefault()?.Amount ?? item.StartingPrice;
            if (bidAmount <= highestBidAmount)
            {
                TempData["Error"] = "Your bid must be higher than the current highest bid.";
                ModelState.AddModelError("BidModel.BidAmount", "Your bid must be higher than the current highest bid.");

                model.Item = item;
                model.AuctionStatus = auction.Status;
                model.BidList = bidsActive; // ✅ ADD THIS

                return View("~/Views/Home/Details.cshtml", model);
            }

            var bid = new Bid
            {
                Amount = bidAmount,
                AuctionId = auction.Id,
                UserId = userId,
                BidTime = DateTime.Now
            };

            await _unitOfWork.Bids.AddAsync(bid);
            await _unitOfWork.CompleteAsync();
            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<BidHub>>();
            await hubContext.Clients.Group($"item-{itemId}").SendAsync("ReceiveBidUpdate", itemId);

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



    }
}
