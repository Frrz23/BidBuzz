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

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(Roles.Admin);

            var auctions = await _unitOfWork.Auctions.GetAllAsync(
                a => a.Status == AuctionStatus.Sold &&
                     (a.PaymentStatus == PaymentStatus.ToPay || a.PaymentStatus == PaymentStatus.Paid),
                includeProperties: "Item,Item.User,Bids.User"
            );

            var visibleAuctions = auctions.Where(a =>
            {
                var highestBid = a.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
                return highestBid?.UserId == userId 
                       || a.Item.UserId == userId  
                       || isAdmin;               
            }).ToList();

            var viewModel = new PaymentVM
            {
                ToPay = visibleAuctions.Where(a => a.PaymentStatus == PaymentStatus.ToPay).ToList(),
                Paid = visibleAuctions.Where(a => a.PaymentStatus == PaymentStatus.Paid).ToList()
            };

            return View(viewModel);
        }

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
                            UnitAmount = (long)(highestBid.Amount * 100), 
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

            TempData["StripeSessionId"] = session.Id;

            return Redirect(session.Url);
        }

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

        public IActionResult Cancel(int auctionId)
        {
            TempData["Message"] = "Payment was canceled. You can try again later.";
            return RedirectToAction(nameof(Index));
        }
    }
}