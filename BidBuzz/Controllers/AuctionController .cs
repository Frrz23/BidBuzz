//using Microsoft.AspNetCore.Mvc;
//using Models.Models;
//using Models.Models.ViewModels;
//using System.Threading.Tasks;
//using DataAccess.Repository.IRepository;
//using Utility;

//namespace BidBuzz.Controllers
//{
//    public class AuctionController : Controller
//    {
//        private readonly IUnitOfWork _unitOfWork;

//        public AuctionController(IUnitOfWork unitOfWork)
//        {
//            _unitOfWork = unitOfWork;
//        }

//        public async Task<IActionResult> Index()
//        {
//            var auctionVMs = await _unitOfWork.Auctions.GetAllForAuctionManagementAsync();
//            return View(auctionVMs);
//        }

//        public async Task<IActionResult> Update(int? id, int? itemId)
//        {
//            AuctionVM auctionVM = new AuctionVM();

//            if (id != null && id > 0)
//            {
//                // Load existing auction using GetByIdAsync
//                var auction = await _unitOfWork.Auctions.GetByIdAsync(id.Value, "Item");
//                if (auction == null)
//                {
//                    return NotFound();
//                }
//                auctionVM.Auction = auction;
//                auctionVM.Item = auction.Item;
//            }
//            else if (itemId != null && itemId > 0)
//            {
//                // Load item using GetByIdAsync
//                var item = await _unitOfWork.Items.GetByIdAsync(itemId.Value);
//                if (item == null)
//                {
//                    return NotFound();
//                }

//                auctionVM.Item = item;
//                auctionVM.Auction = new Auction()
//                {
//                    ItemId = item.Id,
//                    Status = AuctionStatus.PendingApproval, // Default status
//                    StartTime = null,
//                    EndTime = null
//                };
//            }
//            else
//            {
//                return NotFound();
//            }

//            return View(auctionVM);
//        }


//        // 3. Handle form submission to update or create a new auction
//        [HttpPost]
//        public async Task<IActionResult> Update(AuctionVM auctionVM)
//        {
//            if (ModelState.IsValid)
//            {
//                // Check if auction exists or if it's new
//                if (auctionVM.Auction.Id == 0)
//                {
//                    // New auction, setting status to 'Approved' by default
//                    auctionVM.Auction.Status = AuctionStatus.Approved;
//                    await _unitOfWork.Auctions.AddAsync(auctionVM.Auction);
//                }
//                else
//                {
//                    // Existing auction, update the details
//                    var existingAuction = await _unitOfWork.Auctions.GetByIdAsync(auctionVM.Auction.Id);
//                    if (existingAuction != null)
//                    {
//                        existingAuction.StartTime = auctionVM.Auction.StartTime;
//                        existingAuction.EndTime = auctionVM.Auction.EndTime;
//                        existingAuction.Status = auctionVM.Auction.Status;

//                        _unitOfWork.Auctions.Update(existingAuction);
//                    }
//                }

//                await _unitOfWork.CompleteAsync();
//                return RedirectToAction(nameof(Index));
//            }
//            return View(auctionVM);
//        }

//        // 4. Stop an ongoing auction
//        [HttpPost]
//        public async Task<IActionResult> Stop(int id)
//        {
//            var auction = await _unitOfWork.Auctions.GetByIdAsync(id);
//            if (auction != null && auction.Status == AuctionStatus.InAuction)
//            {
//                auction.Status = AuctionStatus.Sold;
//                _unitOfWork.Auctions.Update(auction);
//                await _unitOfWork.CompleteAsync();
//            }

//            return RedirectToAction(nameof(Index));
//        }
//    }
//}
