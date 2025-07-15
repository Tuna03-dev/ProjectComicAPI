using DotNetTruyen.Models;

namespace DotNetTruyen.ViewModels.Management
{
    public class ChapterIndexViewModel
    {
        public List<ChapterViewModel> ChapterViewModels { get; set; }
        public string SearchQuery { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public Guid ComicId { get; set; }
    }
}
