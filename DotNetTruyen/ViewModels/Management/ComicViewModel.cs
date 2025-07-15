using DotNetTruyen.Models;

namespace DotNetTruyen.ViewModels.Management
{
    public class ComicViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string? CoverImage { get; set; }
        public string Author { get; set; }
        public int View { get; set; }
        public bool Status { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int Likes { get; set; }
        public ICollection<Chapter> Chapters { get; set; }
        public ICollection<Follow> Follows { get; set; } = new List<Follow>();
    }
}

