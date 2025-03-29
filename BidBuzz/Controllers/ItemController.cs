using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Models;

using Models.ViewModels;
using System.Security.Claims;
using Utility;

namespace BidBuzz.Controllers
{
    [Authorize]
    public class ItemController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AuctionScheduleConfig _auctionSchedule;


        public ItemController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, IOptions<AuctionScheduleConfig> auctionScheduleConfig)//ioptions is used to get data from appsetiings without it we cant get it 
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _auctionSchedule = auctionScheduleConfig.Value; // Extract value from IOptions
        }


        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var items = await _unitOfWork.Items.GetAllAsync(u=>u.UserId==userId,includeProperties: "Category,Auctions");

            var itemVMs = items.Select(i => new ItemVM
            {

                Item = i,
                AuctionStatus = i.Auctions.OrderByDescending(a => a.StartTime).FirstOrDefault()?.Status
            }).ToList();

            return View(itemVMs);
        }





        public async Task<IActionResult> Upsert(int? id)
        {
            var categories = await _unitOfWork.Categories.GetAllAsync(null);
            if (categories == null || !categories.Any())
            {
                ModelState.AddModelError("", "No categories available. Please add a category first.");
                return View(new ItemVM { Item = new Item() }); // Ensure Item is not null
            }
            ViewBag.Categories = categories;

            ItemVM itemVM = new ItemVM { Item = new Item() };

            if (id != null && id != 0)
            {
                var item = await _unitOfWork.Items.GetByIdAsync(id.Value);
                if (item == null)
                {
                    return NotFound();
                }

                var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == item.Id);

                itemVM = new ItemVM
                {
                    
                    Item = item,
                    AuctionStatus = auction?.Status ?? AuctionStatus.PendingApproval
                };
            }

            return View(itemVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ItemVM itemVM, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                itemVM.Item.UserId = userId;
                var startTime = AuctionScheduleHelper.GetNextAuctionStart(_auctionSchedule.StartDay, _auctionSchedule.StartHour);
                var endTime = AuctionScheduleHelper.GetNextAuctionEnd(_auctionSchedule.EndDay, _auctionSchedule.EndHour);

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
                        auction.Status = AuctionStatus.Approved; // Update auction status
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
