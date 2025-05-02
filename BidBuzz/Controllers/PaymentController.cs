using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ViewModels;
using Stripe.Checkout;
using System.Security.Claims;
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

        // GET: Payment
        public async Task<IActionResult> Index()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(Roles.Admin); // Replace with your admin role key

            var auctions = await _unitOfWork.Auctions.GetAllAsync(includeProperties: "Item,Bids");

            // Filter auctions visible to this user
            var visibleAuctions = auctions.Where(a => 
            {
                if (a.Status != AuctionStatus.Sold) return false;
                var highestBid = a.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
                return highestBid?.UserId == userId // won the auction
                       || a.Item.UserId == userId  // seller
                       || isAdmin;                // admin
            }).ToList();

            return View(visibleAuctions);
        }


        // GET: Payment/Pay/5
        public async Task<IActionResult> Pay(int auctionId)
        {
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(
                a => a.Id == auctionId,
                includeProperties: "Item,Bids"
            );

            if (auction == null || auction.PaymentStatus == PaymentStatus.Paid)
            {
                return NotFound();
            }

            var highestBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            if (highestBid == null || highestBid.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
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
                                Name = auction.Item.Name
                            }
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = $"{domain}/Payment/Success?auctionId={auction.Id}",
                CancelUrl = $"{domain}/Payment/Index",
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }

        // GET: Payment/Success
        public async Task<IActionResult> Success(int auctionId)
        {
            var auction = await _unitOfWork.Auctions.GetByIdAsync(auctionId);

            if (auction != null && auction.PaymentStatus == PaymentStatus.Unpaid)
            {
                auction.PaymentStatus = PaymentStatus.Paid;
                _unitOfWork.Auctions.Update(auction);
                await _unitOfWork.CompleteAsync();
            }

            return View();
        }
    }
}
