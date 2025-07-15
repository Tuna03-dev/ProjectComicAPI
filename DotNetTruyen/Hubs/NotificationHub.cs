using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DotNetTruyen.Hubs
{
    public class NotificationHub : Hub
    {

        public override async Task OnConnectedAsync()
        {

            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }


            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnConnectedAsync();
        }


        public async Task SendNotificationToUser(string userId, string title, string message, string type, string icon, string link)
        {
            await Clients.Group(userId).SendAsync("ReceiveNotification", new
            {
                title,
                message,
                type,
                icon,
                link
            });
        }

        public async Task SendNotificationToAdmins(string title, string message, string type, string icon, string link)
        {
            await Clients.Group("Admins").SendAsync("ReceiveNotification", new
            {
                title,
                message,
                type,
                icon,
                link
            });
        }


        public async Task UpdateUnreadCountForUser(string userId, int unreadCount)
        {
            await Clients.Group(userId).SendAsync("UpdateUnreadCount", unreadCount);
        }


        public async Task UpdateUnreadCountForAdmins(int unreadCount)
        {
            await Clients.Group("Admins").SendAsync("UpdateUnreadCount", unreadCount);
        }


        public async Task MarkNotificationAsReadForUser(string userId, string notificationId)
        {
            await Clients.Group(userId).SendAsync("MarkNotificationAsRead", notificationId);
        }


        public async Task MarkNotificationAsReadForAdmins(string notificationId)
        {
            await Clients.Group("Admins").SendAsync("MarkNotificationAsRead", notificationId);
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {

            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }


            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}