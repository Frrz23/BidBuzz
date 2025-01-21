using Microsoft.AspNetCore.Mvc;

namespace BidBuzz.Controllers
{
    public class CategoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
