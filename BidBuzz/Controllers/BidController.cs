using System.Security.Claims;
using System.Threading.Tasks;
using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
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
        public async Task<IActionResult> PlaceBid(int itemId, decimal bidAmount)
        {
            // Get the item from the database
            var item = await _unitOfWork.Items.GetByIdAsync(itemId);
            if (item == null)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Get the associated auction
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);
            if (auction == null || auction.Status != AuctionStatus.InAuction)
            {
                // If no active auction is found, we return an error
                return BadRequest("The auction is not active.");
            }

            // Get the current highest bid
            var highestBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            if (bidAmount <= highestBid?.Amount)
            {
                // Ensure the bid is higher than the current highest bid
                return BadRequest("Your bid must be higher than the current highest bid.");
            }

            // Create a new Bid
            var bid = new Bid
            {
                Amount = bidAmount,
                AuctionId = auction.Id,
                UserId=userId,
                BidTime = DateTime.Now
            };

            // Add the bid to the auction and save changes to the database
            await _unitOfWork.Bids.AddAsync(bid);
            await _unitOfWork.CompleteAsync();

            return RedirectToAction("Details", "Home", new { itemId });
        }
    }
}
