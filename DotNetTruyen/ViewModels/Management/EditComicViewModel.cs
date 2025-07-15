using DotNetTruyen.Models;

namespace DotNetTruyen.ViewModels.Management
{
    public class EditComicViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string? CoverImage { get; set; }
        //cho phep null
        public IFormFile? CoverImageFile { get; set; }
        public string Author { get; set; }

        public bool Status { get; set; }
        public List<Guid> SelectedGenres { get; set; }
        public List<GenreViewModel>? Genres { get; set; }


    }
}
