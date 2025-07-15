using DotNetTruyen.Models;

namespace DotNetTruyen.ViewModels.Management
{
    public class GenreDetailViewModel
    {
        public Guid Id { get; set; }
        public string GenreName { get; set; }
        public List<Guid> SelectedStoryIds { get; set; } = new List<Guid>();

        public List<Comic> Comics { get; set; } = new List<Comic>();


    }
}
