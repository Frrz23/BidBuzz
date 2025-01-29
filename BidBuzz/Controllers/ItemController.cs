using DataAccess.Repositary;
using Microsoft.AspNetCore.Mvc;

namespace BidBuzz.Controllers
{
    public class ItemController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ItemController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IActionResult> Index()
        {
            var item=_unitOfWork.Item.GetAllAsync();
            return View(item);
        }

    }
}
