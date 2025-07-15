using System.ComponentModel.DataAnnotations;

namespace DotNetTruyen.ViewModels.Management
{
    public class CreateComicViewModel
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public string Author { get; set; }

        [Required]
        public IFormFile CoverImage { get; set; }

        public List<Guid> GenreIds { get; set; }

        public bool Status { get; set; } = true;

        public List<GenreViewModel> Genres { get; set; }
    }
}
