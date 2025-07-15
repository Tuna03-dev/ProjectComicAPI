using DotNetTruyen.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotNetTruyen.ViewComponents
{
    public class RecentNotificationsViewComponent : ViewComponent
    {
        private readonly DotNetTruyenDbContext _context;

        public RecentNotificationsViewComponent(DotNetTruyenDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            
            var notifications = await _context.Notifications
                .Where(n => n.DeletedAt == null && n.UserId == null)
                .OrderByDescending(n => n.CreatedAt)
                .Take(3)
                .ToListAsync();

           
            int unreadCount = await _context.Notifications
                .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == null);

            ViewBag.UnreadCount = unreadCount;
            ViewBag.HasUnreadNotifications = unreadCount > 0;
            return View("~/Views/Shared/NotificationPartial.cshtml", notifications);
        }
    }
}