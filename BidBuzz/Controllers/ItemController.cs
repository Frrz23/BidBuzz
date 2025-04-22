using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Models;

using Models.ViewModels;
using System.Linq;
using System.Security.Claims;
using Utility;

namespace BidBuzz.Controllers
{
    [Authorize]
    public class ItemController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAuctionScheduleRepository _scheduleRepo;   // ← new
        private readonly IAuctionRepository _auctionRepo;



        public ItemController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, IAuctionScheduleRepository schedlueRepo, IAuctionRepository auctionRepo)//ioptions is used to get data from appsetiings without it we cant get it 
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _scheduleRepo = schedlueRepo; // Extract value from IOptions
            _auctionRepo = auctionRepo;
        }


        public async Task<IActionResult> Index(string status = "All")
        {
            ViewBag.SelectedStatus = status;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            var isAdmin = User.IsInRole(Roles.Admin);

            var items = isAdmin
         ? await _unitOfWork.Items.GetAllAsync(includeProperties: "Category,User,Auctions.Bids")
         : await _unitOfWork.Items.GetAllAsync(i => i.UserId == userId,
                                               includeProperties: "Category,User,Auctions.Bids");

            // 2) Project into your VM
            var itemVMs = items.Select(i => {
                var latestAuct = i.Auctions
                                   .OrderByDescending(a => a.StartTime)
                                   .FirstOrDefault();
                return new ItemVM
                {
                    Item = i,
                    AuctionStatus = latestAuct?.Status ?? AuctionStatus.PendingApproval,
                    UserName = i.User?.UserName
                };
            })
            .ToList();

            // 3) Filter
            IEnumerable<ItemVM> filtered = status switch
            {
                "PendingApproval" => itemVMs.Where(vm => vm.AuctionStatus == AuctionStatus.PendingApproval),
                "Approved" => itemVMs.Where(vm => vm.AuctionStatus == AuctionStatus.Approved),
                "InAuction" => itemVMs.Where(vm => vm.AuctionStatus == AuctionStatus.InAuction),
                "Sold" => itemVMs.Where(vm => vm.AuctionStatus == AuctionStatus.Sold),
                "NotApproved" => itemVMs.Where(vm => vm.AuctionStatus == AuctionStatus.Cancelled),
                "Unsold" => itemVMs.Where(vm => {
                    var last = vm.Item.Auctions
                                  .OrderByDescending(a => a.StartTime)
                                  .FirstOrDefault();
                    return last != null
                        && last.Status == AuctionStatus.Sold
                        && !last.Bids.Any();
                }),
                _ => itemVMs  // "All"
            };


            return View(filtered);
        }





        public async Task<IActionResult> Upsert(int? id)
        {
            var categories = await _unitOfWork.Categories.GetAllAsync(null);
            // … your existing category-check logic …

            var itemVM = new ItemVM { Item = new Item() };

            if (id != null && id != 0)
            {
                var item = await _unitOfWork.Items.GetByIdAsync(id.Value);
                if (item == null) return NotFound();

                // Get how many times this item ended unsold
                var unsoldAuctions = await _unitOfWork.Auctions
                    .GetAllAsync(a => a.ItemId == item.Id
                                     && a.Status == AuctionStatus.Unsold);

                int maxAttempts = 3;
                int usedAttempts = unsoldAuctions.Count();
                int remaining = Math.Max(0, maxAttempts - usedAttempts);

                itemVM = new ItemVM
                {
                    Item = item,
                    AuctionStatus = (await _unitOfWork.Auctions
                                        .GetFirstOrDefaultAsync(a => a.ItemId == item.Id))
                                        ?.Status
                                  ?? AuctionStatus.PendingApproval,
                    RemainingRelistAttempts = remaining
                };
            }
            else
            {
                // New item: all 3 attempts still available
                itemVM.RemainingRelistAttempts = 3;
            }

            ViewBag.Categories = categories;
            return View(itemVM);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ItemVM itemVM, IFormFile? file)
        {
          

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (itemVM.Item.Id == 0)
                {
                    // Only set UserId when creating a new item
                    itemVM.Item.UserId = userId;
                }
                else
                {
                    // Editing an existing item - preserve the original UserId
                    var existingItem = await _unitOfWork.Items.GetByIdAsNoTrackingAsync(itemVM.Item.Id);
                    if (existingItem != null)
                    {
                        itemVM.Item.UserId = existingItem.UserId; // Keep original UserId
                    }
                }
                var schedule = await _scheduleRepo.GetScheduleAsync("Current")
                                                 ?? throw new InvalidOperationException("No current auction schedule found");

                var startDayOfWeek = (int)Enum.Parse<DayOfWeek>(schedule.StartDay);
                var endDayOfWeek = (int)Enum.Parse<DayOfWeek>(schedule.EndDay);

                var startTime = AuctionScheduleHelper.GetNextAuctionStart(startDayOfWeek, schedule.StartHour);
                var endTime = AuctionScheduleHelper.GetNextAuctionEnd(endDayOfWeek, schedule.EndHour);

                string wwwRootPath = _webHostEnvironment.WebRootPath;

                if (file != null)
                {
                    string productPath = Path.Combine(wwwRootPath, @"images");

                    if (!Directory.Exists(productPath))
                    {
                        Directory.CreateDirectory(productPath);
                    }

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                    // Delete old image if editing
                    if (!string.IsNullOrEmpty(itemVM.Item.ImageUrl) && itemVM.Item.Id != 0)
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, itemVM.Item.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    itemVM.Item.ImageUrl = @"\images\" + fileName;
                }
                else if (itemVM.Item.Id != 0) // Editing case, no new file uploaded
                {
                    var existingItem = await _unitOfWork.Items.GetByIdAsNoTrackingAsync(itemVM.Item.Id);

                    if (existingItem != null)
                    {
                        itemVM.Item.ImageUrl = existingItem.ImageUrl; // Keep existing image
                    }
                }



                if (itemVM.Item.Id == 0)
                {

                    // Creating new item
                    await _unitOfWork.Items.AddAsync(itemVM.Item);
                    await _unitOfWork.CompleteAsync();

                    // Create auction for the item
                    var auction = new Auction
                    {
                        ItemId = itemVM.Item.Id,
                        StartTime = startTime,
                        EndTime = endTime,
                        Status = AuctionStatus.PendingApproval
                    };
                    await _unitOfWork.Auctions.AddAsync(auction);
                }
                else
                {
                    // Editing existing item
                    var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemVM.Item.Id);
                    if (auction != null)
                    {
                        if (itemVM.AuctionStatus.HasValue)
                        {
                            auction.Status = itemVM.AuctionStatus.Value;
                        }

                        _unitOfWork.Auctions.Update(auction);
                    }

                    _unitOfWork.Items.Update(itemVM.Item);
                }

                await _unitOfWork.CompleteAsync();
                return RedirectToAction(nameof(Index));
            }
          
            var categories = await _unitOfWork.Categories.GetAllAsync(null);
            ViewBag.Categories = categories;
            return View(itemVM); // Ensure correct type is returned
        }


        // GET: Item/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _unitOfWork.Items.GetByIdAsync(id);
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == item.Id);

            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Item/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _unitOfWork.Items.GetByIdAsync(id);
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == item.Id);
            if (item != null)
            {
                // Delete the associated image file if it exists
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, item.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                // Now delete the item from the database
                _unitOfWork.Items.Delete(item.Id);
                if (auction != null)
                {
                    _unitOfWork.Auctions.Delete(auction.Id);
                }

                await _unitOfWork.CompleteAsync();
            }
           
            

            return RedirectToAction(nameof(Index));
        }

    }
}
