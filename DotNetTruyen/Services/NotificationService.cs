
using DotNetTruyen.Data;
using DotNetTruyen.Hubs;
using DotNetTruyen.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DotNetTruyen.Services
{
    public class NotificationService : INotificationService
    {
        private readonly DotNetTruyenDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(DotNetTruyenDbContext context, IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId = null, bool isAdmin = false)
        {
            if (isAdmin)
            {

                return await _context.Notifications
                    .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == null);
            }
            else if (!string.IsNullOrEmpty(userId))
            {

                return await _context.Notifications
                    .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == Guid.Parse(userId));
            }
            else
            {

                return 0;
            }
        }

        public async Task SendFollowerNotificationsAsync(Guid chapterId, Guid comicId, string chapterTitle, string comicTitle)
        {
            try
            {
                var followers = await _context.Follows
                    .Where(f => f.ComicId == comicId)
                    .Select(f => f.UserId)
                    .ToListAsync();

                var notifications = followers.Select(userId => new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = "Chương mới",
                    Message = $"Chương mới '{chapterTitle}' của truyện '{comicTitle}' đã được đăng!",
                    Type = "success",
                    Icon = "check-circle",
                    Link = $"/ReadChapter/Index/{chapterId}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                if (notifications.Any())
                {
                    _context.Notifications.AddRange(notifications);
                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.Users(followers.Select(id => id.ToString()))
                        .SendAsync("ReceiveNotification", notifications.Select(n => new
                        {
                            id = n.Id,
                            title = n.Title,
                            message = n.Message,
                            type = n.Type,
                            icon = n.Icon,
                            link = n.Link
                        }).ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notifications for chapter {ChapterId}", chapterId);
            }
        }
    }
}
