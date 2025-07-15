using DotNetTruyen.Data;
using DotNetTruyen.Hubs;
using DotNetTruyen.Models;
using DotNetTruyen.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetTruyen.Controllers.UserLevel
{
    public class UserNotificationController : Controller
    {
        private readonly DotNetTruyenDbContext _context;
        private readonly UserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;
        private const int PageSize = 10;

        public UserNotificationController(DotNetTruyenDbContext context, UserService userService, UserManager<User> userManager, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _userService = userService;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        [HttpGet("/userProfile/Notifications")]
        public async Task<IActionResult> Index(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(); 
            }


            int unreadCount = await _context.Notifications
                .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == user.Id);
            ViewBag.UnreadCount = unreadCount;

            
            int totalNotifications = await _context.Notifications
                .CountAsync(n => n.DeletedAt == null && n.UserId == user.Id);

            int totalPages = (int)Math.Ceiling((double)totalNotifications / PageSize);
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            
            var notifications = await _context.Notifications
                .Where(n => n.DeletedAt == null && n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.NotificationTab = "true";
            return View("~/Views/User/UserNotification/Index.cshtml", notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            if (request == null || request.Id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid notification ID." });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == user.Id);
            if (notification == null || notification.DeletedAt != null)
            {
                return Json(new { success = false, message = "Notification not found or not accessible." });
            }

            if (notification.IsRead)
            {
                return Json(new { success = true, message = "Notification already marked as read." });
            }

            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;
            _context.Update(notification);
            await _context.SaveChangesAsync();

            int unreadCount = await _context.Notifications
                .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == user.Id);

            await _hubContext.Clients.User(user.Id.ToString()).SendAsync("UpdateUnreadCount", unreadCount);
            await _hubContext.Clients.User(user.Id.ToString()).SendAsync("MarkNotificationAsRead", request.Id.ToString());

            return Json(new { success = true, unreadCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var unreadNotifications = await _context.Notifications
                .Where(n => n.DeletedAt == null && !n.IsRead && n.UserId == user.Id)
                .ToListAsync();

            if (!unreadNotifications.Any())
            {
                TempData["SuccessMessage"] = "Không có thông báo chưa đọc nào để đánh dấu.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.UpdatedAt = DateTime.UtcNow;
                _context.Update(notification);
            }

            await _context.SaveChangesAsync();

            int unreadCount = await _context.Notifications
                .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == user.Id);

            await _hubContext.Clients.User(user.Id.ToString()).SendAsync("UpdateUnreadCount", unreadCount);
            foreach (var notification in unreadNotifications)
            {
                await _hubContext.Clients.User(user.Id.ToString()).SendAsync("MarkNotificationAsRead", notification.Id.ToString());
            }

            TempData["SuccessMessage"] = "Tất cả thông báo được đánh dấu là đã đọc.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAll()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var notifications = await _context.Notifications
                .Where(n => n.DeletedAt == null && n.UserId == user.Id)
                .ToListAsync();

            if (!notifications.Any())
            {
                TempData["SuccessMessage"] = "Không có thông báo nào cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var notification in notifications)
            {
                notification.DeletedAt = DateTime.UtcNow;
                _context.Update(notification);
            }

            await _context.SaveChangesAsync();

            int unreadCount = await _context.Notifications
                .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == user.Id);

            await _hubContext.Clients.User(user.Id.ToString()).SendAsync("UpdateUnreadCount", unreadCount);

            TempData["SuccessMessage"] = "Đã xóa tất cả thông báo.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Dismiss(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);
            if (notification == null || notification.DeletedAt != null)
            {
                return Json(new { success = false, message = "Notification not found or not accessible." });
            }

            notification.DeletedAt = DateTime.UtcNow;
            _context.Update(notification);
            await _context.SaveChangesAsync();

            int unreadCount = await _context.Notifications
                .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == user.Id);

            await _hubContext.Clients.User(user.Id.ToString()).SendAsync("UpdateUnreadCount", unreadCount);

            return Json(new { success = true });
        }

        public class MarkAsReadRequest
        {
            public Guid Id { get; set; }
        }
    }
}