namespace DotNetTruyen.Models
{
    public class Genre : BaseEnity<Guid>
    {
        public string GenreName { get; set; }
        public ICollection<ComicGenre> ComicGenres { get; set; } = new List<ComicGenre>();
    }
}
