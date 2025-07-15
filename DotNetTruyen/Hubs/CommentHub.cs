using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace DotNetTruyen.Hubs
{
    public class CommentHub : Hub
    {
        // Broadcast a message to reload comments for a specific comic
        public async Task ReloadComments(Guid comicId)
        {
            await Clients.Group($"Comic_{comicId}").SendAsync("ReloadComments", comicId);
        }

        // Add client to a group based on the comic ID
        public async Task JoinComicGroup(Guid comicId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Comic_{comicId}");
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendComicUpdate(Guid comicId, int followCount, int likeCount)
        {
            await Clients.All.SendAsync("ReceiveComicUpdate", comicId, followCount, likeCount);
        }
    }
}
