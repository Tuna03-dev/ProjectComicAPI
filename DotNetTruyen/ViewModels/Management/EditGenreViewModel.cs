using DotNetTruyen.Models;

namespace DotNetTruyen.ViewModels.Management
{
    public class EditGenreViewModel
    {
        public Guid Id { get; set; }
        public string GenreName { get; set; }
        public string SelectedStoryIds { get; set; }
        public List<Comic> Comics { get; set; } = new List<Comic>();

    }
}
