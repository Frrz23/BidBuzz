using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace SHAH.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _unitOfWork.Notifications.GetAllAsync(n => n.UserId == userId && !n.IsRead);
            return Json(new { count = notifications.Count() });
        }

        public async Task<IActionResult> GetRecent()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _unitOfWork.Notifications.GetAllAsync(n => n.UserId == userId);
            var recent = notifications.OrderByDescending(n => n.CreatedAt).Take(10).ToList();
            return Json(recent.Select(n => new
            {
                n.Id,
                n.Message,
                n.IsRead,
                n.CreatedAt,
                n.RelatedItemId,
                n.RelatedItemName,
                timeAgo = GetTimeAgo(n.CreatedAt)
            }));
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
            if (notification != null && notification.UserId == userId)
            {
                notification.IsRead = true;
                _unitOfWork.Notifications.Update(notification);
                await _unitOfWork.CompleteAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _unitOfWork.Notifications.GetAllAsync(n => n.UserId == userId && !n.IsRead);
            foreach (var n in notifications)
            {
                n.IsRead = true;
                _unitOfWork.Notifications.Update(n);
            }
            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        private static string GetTimeAgo(System.DateTime dateTime)
        {
            var span = System.DateTime.UtcNow - dateTime;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            return $"{(int)span.TotalDays}d ago";
        }
    }
}
