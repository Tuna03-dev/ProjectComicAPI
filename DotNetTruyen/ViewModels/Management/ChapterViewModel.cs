using DotNetTruyen.Models;

namespace DotNetTruyen.ViewModels.Management
{
    public class ChapterViewModel
    {
        public Guid Id { get; set; }
        public string ChapterTitle { get; set; }
        public int ChapterNumber { get; set; }
        public DateTime? PublishedDate { get; set; }
        public bool IsPublished { get; set; }
        public int Views { get; set; }
        public Guid ComicId { get; set; }
        

    }
}
