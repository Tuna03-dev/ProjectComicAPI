using Microsoft.AspNetCore.SignalR;

namespace DotNetTruyen.Hubs
{
    public class ComicHub : Hub
    {
        public async Task SendComicUpdate(Guid comicId, int followCount, int likeCount)
        {
            await Clients.All.SendAsync("ReceiveComicUpdate", comicId, followCount, likeCount);
        }
    }
}
