//using DataAccess.Repositary;
//using Microsoft.AspNetCore.Mvc;
//using Models.Models;

//namespace BidBuzz.Controllers
//{
//    public class ItemController : Controller
//    {
//        private readonly IUnitOfWork _unitOfWork;
//        private readonly IWebHostEnvironment _webHostEnvironment;


//        public ItemController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
//        {
//            _unitOfWork = unitOfWork;
//            _webHostEnvironment = webHostEnvironment;
//        }
//        public async Task<IActionResult> Index()
//        {
//            var item=await _unitOfWork.Item.GetAllAsync();
//            return View(item);
//        }
//        public IActionResult Create()
//        {
//            return View();
//        }

//        // POST: Item/Create
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(Item item, IFormFile? file)
//        {
//            if (!ModelState.IsValid)
//            {
//                // Debugging: Show validation errors
//                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
//                TempData["Errors"] = string.Join(", ", errors);
//                return View(item); // Reload form with errors
//            }
//            string wwwRootPath = _webHostEnvironment.WebRootPath;
//                if (file != null)
//                {
//                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
//                    string productPath = Path.Combine(wwwRootPath, @"images");

//                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
//                    {
//                        file.CopyTo(fileStream);
//                    }
//                    item.ImageUrl = @"\images\" + fileName;
//                }
//                else
//                {
//                    item.ImageUrl = null; // ✅ Allow null images
//                }
//                item.Status = "PendingApproval";
//                await _unitOfWork.Item.AddAsync(item);
//                await _unitOfWork.SaveAsync();
//                return RedirectToAction(nameof(Index));
            
//            return View(item);
//        }

//        // GET: Item/Edit/5
//        public async Task<IActionResult> Edit(int id)
//        {
//            var item = await _unitOfWork.Item.GetByIdAsync(id);
//            if (item == null)
//            {
//                return NotFound();
//            }
//            return View(item);
//        }

//        // POST: Item/Edit/5
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, Item item, IFormFile? file)
//        {
//            if (id != item.ItemID)
//            {
//                return NotFound();
//            }

//            if (ModelState.IsValid)
//            {
//                string wwwRootPath = _webHostEnvironment.WebRootPath;

//                if (file != null)
//                {
//                    // Delete old image if exists
//                    if (!string.IsNullOrEmpty(item.ImageUrl))
//                    {
//                        var oldImagePath = Path.Combine(wwwRootPath, item.ImageUrl.TrimStart('\\'));
//                        if (System.IO.File.Exists(oldImagePath))
//                        {
//                            System.IO.File.Delete(oldImagePath);
//                        }
//                    }

//                    // Save new image
//                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
//                    string filePath = Path.Combine(wwwRootPath, @"images",fileName);

//                    using (var fileStream = new FileStream(filePath, FileMode.Create))
//                    {
//                        await file.CopyToAsync(fileStream);
//                    }

//                    item.ImageUrl = @"\images\" + fileName; // Store relative path
//                }

//                await _unitOfWork.Item.UpdateAsync(item);
//                await _unitOfWork.SaveAsync();
//                return RedirectToAction(nameof(Index));
//            }

//            return View(item);
//        }
    

//        // GET: Item/Delete/5
//        public async Task<IActionResult> Delete(int id)
//        {
//            var item = await _unitOfWork.Item.GetByIdAsync(id);
//            if (item == null)
//            {
//                return NotFound();
//            }
//            return View(item);
//        }
//        public async Task<IActionResult> Approve(int id)
//        {
//            var item = await _unitOfWork.Item.GetByIdAsync(id);
//            if (item == null)
//            {
//                return NotFound();
//            }
//            item.Status = "Approved";
//            await _unitOfWork.Item.UpdateAsync(item);
//            await _unitOfWork.SaveAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        // Start Auction (Admin sets auction times and places item in auction)
//        public async Task<IActionResult> StartAuction(int id)
//        {
//            var item = await _unitOfWork.Item.GetByIdAsync(id);
//            if (item == null)
//            {
//                return NotFound();
//            }

//            // Set auction start and end time
//            item.StartTime = DateTime.Now;
//            item.EndTime = (item.StartTime ?? DateTime.Now).AddHours(2); // Auction duration of 1 hour (can be modified)
//            item.Status = "InAuction"; // Update status to InAuction

//            await _unitOfWork.Item.UpdateAsync(item);
//            await _unitOfWork.SaveAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        // End Auction (Admin ends auction and shows the highest bid)
//        public async Task<IActionResult> EndAuction(int id)
//        {
//            var item = await _unitOfWork.Item.GetByIdAsync(id);
//            if (item == null)
//            {
//                return NotFound();
//            }

//            item.Status = "AuctionEnded";
//            await _unitOfWork.Item.UpdateAsync(item);
//            await _unitOfWork.SaveAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        // POST: Item/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            var item = await _unitOfWork.Item.GetByIdAsync(id);
//            if (item != null)
//            {
//                // Delete the associated image file if it exists
//                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, item.ImageUrl.TrimStart('\\'));
//                if (System.IO.File.Exists(oldImagePath))
//                {
//                    System.IO.File.Delete(oldImagePath);
//                }

//                // Now delete the item from the database
//                await _unitOfWork.Item.DeleteAsync(item);
//                await _unitOfWork.SaveAsync();
//            }

//            return RedirectToAction(nameof(Index));
//        }

//    }
//}
