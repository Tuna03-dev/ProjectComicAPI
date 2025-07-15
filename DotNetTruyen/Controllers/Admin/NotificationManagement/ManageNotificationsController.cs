using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DotNetTruyen.Data;
using DotNetTruyen.Models;
using Microsoft.AspNetCore.SignalR;
using DotNetTruyen.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace DotNetTruyen.Controllers.Admin.NotificationManagement
{
	[Authorize(Policy = "CanManageNotification")]
	public class ManageNotificationsController : Controller
    {
        private readonly DotNetTruyenDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private const int PageSize = 10;

        public ManageNotificationsController(DotNetTruyenDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index(int page = 1)
        {

            int unreadCount = await _context.Notifications
                .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == null);
            ViewBag.UnreadCount = unreadCount;


            int totalNotifications = await _context.Notifications
                .CountAsync(n => n.DeletedAt == null && n.UserId == null);

            int totalPages = (int)Math.Ceiling((double)totalNotifications / PageSize);
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;


            var notifications = await _context.Notifications
                .Where(n => n.DeletedAt == null && n.UserId == null)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return View("~/Views/Admin/ManageNotifications/Index.cshtml", notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
        {
            if (request == null || request.Id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid notification ID." });
            }

            var notification = await _context.Notifications.FindAsync(request.Id);
            if (notification == null || notification.DeletedAt != null || notification.UserId != null)
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
                .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == null);


            await _hubContext.Clients.Group("Admins").SendAsync("UpdateUnreadCount", unreadCount);
            await _hubContext.Clients.Group("Admins").SendAsync("MarkNotificationAsRead", request.Id.ToString());

            return Json(new { success = true, unreadCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {

            var unreadNotifications = await _context.Notifications
                .Where(n => n.DeletedAt == null && !n.IsRead && n.UserId == null)
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
                .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == null);


            await _hubContext.Clients.Group("Admins").SendAsync("UpdateUnreadCount", unreadCount);
            foreach (var notification in unreadNotifications)
            {
                await _hubContext.Clients.Group("Admins").SendAsync("MarkNotificationAsRead", notification.Id.ToString());
            }

            TempData["SuccessMessage"] = "Tất cả thông báo được đánh dấu là đã đọc.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAll()
        {

            var notifications = await _context.Notifications
                .Where(n => n.DeletedAt == null && n.UserId == null)
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
                .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == null);


            await _hubContext.Clients.Group("Admins").SendAsync("UpdateUnreadCount", unreadCount);

            TempData["SuccessMessage"] = "Đã xóa tất cả thông báo.";
            return RedirectToAction(nameof(Index));
        }

        // GET: ManageNotifications/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == null);
            if (notification == null)
            {
                return NotFound();
            }

            return View(notification);
        }

        [HttpPost]
        public async Task<IActionResult> Dismiss(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null || notification.DeletedAt != null || notification.UserId != null)
            {
                return Json(new { success = false, message = "Notification not found or not accessible." });
            }

            notification.DeletedAt = DateTime.Now;
            _context.Update(notification);
            await _context.SaveChangesAsync();


            int unreadCount = await _context.Notifications
                .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == null);


            await _hubContext.Clients.Group("Admins").SendAsync("UpdateUnreadCount", unreadCount);

            return Json(new { success = true });
        }

        // POST: ManageNotifications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null && notification.UserId == null)
            {
                notification.DeletedAt = DateTime.Now;
                _context.Update(notification);
                await _context.SaveChangesAsync();


                int unreadCount = await _context.Notifications
                    .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == null);

 
                await _hubContext.Clients.Group("Admins").SendAsync("UpdateUnreadCount", unreadCount);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool NotificationExists(Guid id)
        {
            return _context.Notifications.Any(e => e.Id == id && e.DeletedAt == null && e.UserId == null);
        }

        public class MarkAsReadRequest
        {
            public Guid Id { get; set; }
        }
    }
}