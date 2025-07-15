namespace DotNetTruyen.Models
{
    public class Notification : BaseEnity<Guid>
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } 
        public string Icon { get; set; } 
        public string Link { get; set; }
        public bool IsRead { get; set; }
        public Guid? UserId { get; set; }
    }
}
