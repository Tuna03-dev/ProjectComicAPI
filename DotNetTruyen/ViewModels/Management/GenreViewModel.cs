namespace DotNetTruyen.ViewModels.Management
{
    public class GenreViewModel
    {
        public Guid Id { get; set; }
        public string GenreName { get; set; }
        public int TotalStories { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
