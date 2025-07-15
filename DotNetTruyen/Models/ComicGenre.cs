namespace DotNetTruyen.Models
{
    public class ComicGenre
    {
        public Guid ComicId { get; set; }
        public Guid GenreId { get; set; }
        public Comic Comic { get; set; }
        public Genre Genre { get; set; }
    }
}
