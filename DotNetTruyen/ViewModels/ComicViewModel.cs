using DotNetTruyen.Models;

namespace DotNetTruyen.ViewModels
{
    public class ComicViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverImage { get; set; }
        public string Author { get; set; }
        public int ViewCount { get; set; }
        public string Status { get; set; }
        public List<string> Genres { get; set; }
        public int ChapterCount { get; set; }
    }
}
