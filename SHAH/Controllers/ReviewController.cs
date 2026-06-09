using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Utility;

namespace SHAH.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> UserReviews(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return NotFound();

            var reviews = await _unitOfWork.Reviews.GetAllAsync(
                r => r.ReviewedUserId == userId,
                includeProperties: "Reviewer,Item"
            );

            var ordered = reviews.OrderByDescending(r => r.CreatedAt).ToList();
            var averageRating = ordered.Any() ? (decimal)ordered.Average(r => r.Rating) : 0m;

            ViewBag.AverageRating = averageRating;
            ViewBag.ReviewCount = ordered.Count;
            ViewBag.ReviewedUserId = userId;

            return View(ordered);
        }

        [HttpGet]
        public IActionResult Create(int itemId)
        {
            return View(itemId);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int itemId, int rating, string comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var item = await _unitOfWork.Items.GetByIdAsync(itemId);
            if (item == null)
                return NotFound();

            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);
            if (auction == null || auction.Status != AuctionStatus.Sold)
            {
                TempData["Error"] = "You can only review items that have been sold.";
                return RedirectToAction("Details", "Home", new { itemId });
            }

            var bids = await _unitOfWork.Bids.GetBidsForAuctionAsync(auction.Id);
            var winningBid = bids.FirstOrDefault();

            if (userId != item.UserId)
            {
                if (winningBid == null || winningBid.UserId != userId)
                {
                    TempData["Error"] = "Only the winning bidder or the seller can leave a review.";
                    return RedirectToAction("Details", "Home", new { itemId });
                }
            }

            var existing = await _unitOfWork.Reviews.GetFirstOrDefaultAsync(
                r => r.ReviewerId == userId && r.ItemId == itemId
            );

            if (existing != null)
            {
                TempData["Error"] = "You have already reviewed this transaction.";
                return RedirectToAction("UserReviews", new { userId });
            }

            var reviewedUserId = userId == item.UserId ? winningBid.UserId : item.UserId;

            var review = new Review
            {
                Rating = rating,
                Comment = comment,
                ReviewerId = userId,
                ReviewedUserId = reviewedUserId,
                ItemId = itemId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Reviews.AddAsync(review);
            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "Review submitted successfully!";
            return RedirectToAction("UserReviews", new { userId = reviewedUserId });
        }
    }
}
