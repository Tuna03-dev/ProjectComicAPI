namespace DotNetTruyen.Models
{
    public class Like
    {
        public Guid ComicId { get; set; }
        public Guid UserId { get; set; }
        public Comic Comic { get; set; }
        public User User { get; set; }
    }
}
