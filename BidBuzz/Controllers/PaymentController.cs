using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ViewModels;
using Stripe.Checkout;
using Utility;

namespace BidBuzz.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public PaymentController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: Payment - Show payments relevant to the user
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(Roles.Admin);

            // 1) pull in seller + bidder nav props via string‐based includes:
            var auctions = await _unitOfWork.Auctions.GetAllAsync(
                a => a.Status == AuctionStatus.Sold &&
                     (a.PaymentStatus == PaymentStatus.ToPay || a.PaymentStatus == PaymentStatus.Paid),
                includeProperties: "Item,Item.User,Bids.User"
            );

            // Filter auctions visible to this user
            var visibleAuctions = auctions.Where(a =>
            {
                var highestBid = a.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
                return highestBid?.UserId == userId // won the auction
                       || a.Item.UserId == userId  // seller
                       || isAdmin;                // admin
            }).ToList();

            var viewModel = new PaymentVM
            {
                ToPay = visibleAuctions.Where(a => a.PaymentStatus == PaymentStatus.ToPay).ToList(),
                Paid = visibleAuctions.Where(a => a.PaymentStatus == PaymentStatus.Paid).ToList()
            };

            return View(viewModel);
        }

        // GET: Payment/Pay/5
        public async Task<IActionResult> Pay(int auctionId)
        {
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(
                a => a.Id == auctionId,
                includeProperties: "Item,Bids"
            );

            if (auction == null || auction.Status != AuctionStatus.Sold || auction.PaymentStatus != PaymentStatus.ToPay)
            {
                TempData["Error"] = "This auction is not available for payment.";
                return RedirectToAction(nameof(Index));
            }

            var highestBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            if (highestBid == null)
            {
                TempData["Error"] = "No bids found for this auction.";
                return RedirectToAction(nameof(Index));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (highestBid.UserId != userId)
            {
                TempData["Error"] = "You are not the winning bidder for this auction.";
                return RedirectToAction(nameof(Index));
            }

            var domain = $"{Request.Scheme}://{Request.Host}";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(highestBid.Amount * 100), // Convert to cents
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = auction.Item.Name,
                                Description = $"Auction #{auction.Id} - {auction.Item.Description?.Substring(0, Math.Min(auction.Item.Description.Length, 100))}"
                            }
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = $"{domain}/Payment/Success?auctionId={auction.Id}",
                CancelUrl = $"{domain}/Payment/Cancel?auctionId={auction.Id}",
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Store the session ID in TempData for reference
            TempData["StripeSessionId"] = session.Id;

            return Redirect(session.Url);
        }

        // GET: Payment/Success
        public async Task<IActionResult> Success(int auctionId)
        {
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(
       a => a.Id == auctionId,
       includeProperties: "Item,Bids,Bids.User"
   );
            if (auction != null && auction.PaymentStatus == PaymentStatus.ToPay)
            {
                auction.PaymentStatus = PaymentStatus.Paid;
                _unitOfWork.Auctions.Update(auction);
                await _unitOfWork.CompleteAsync();

                TempData["Success"] = "Payment completed successfully!";
            }

            return View(auction);
        }

        // GET: Payment/Cancel
        public IActionResult Cancel(int auctionId)
        {
            TempData["Message"] = "Payment was canceled. You can try again later.";
            return RedirectToAction(nameof(Index));
        }
    }
}