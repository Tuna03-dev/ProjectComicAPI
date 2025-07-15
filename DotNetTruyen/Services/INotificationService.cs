namespace DotNetTruyen.Services
{
    public interface INotificationService
    {
        Task<int> GetUnreadNotificationCountAsync(string userId = null, bool isAdmin = false);
        Task SendFollowerNotificationsAsync(Guid chapterId, Guid comicId, string chapterTitle, string comicTitle);
    }
}
