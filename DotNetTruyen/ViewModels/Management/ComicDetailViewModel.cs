namespace DotNetTruyen.ViewModels.Management
{
    public class ComicDetailViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string? CoverImage { get; set; }
        public string Author { get; set; }
        public int View { get; set; }
        public bool Status { get; set; }
        public int Likes { get; set; }
        public int Follows { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        //public List<ChapterViewModel> RecentChapters { get; set; } = new List<ChapterViewModel>();
        //public List<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();
    }
}
