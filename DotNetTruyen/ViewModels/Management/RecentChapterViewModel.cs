namespace DotNetTruyen.ViewModels.Management
{
    public class RecentChapterViewModel
    {
        public Guid Id { get; set; }
        public string ChapterTitle { get; set; }
        public string ComicTitle { get; set; }
        public DateTime? PublishedDate { get; set; }
        public bool IsPublished { get; set; }
        public string Thumbnail { get; set; }
    }
}
