namespace DotNetTruyen.Models
{
    public class ChapterImage : BaseEnity<Guid>
    {
        public string ImageUrl { get; set; }
        public int ImageNumber  { get; set; }
        public Guid ChapterId { get; set; }
        public Chapter Chapter { get; set; }
    }
}
