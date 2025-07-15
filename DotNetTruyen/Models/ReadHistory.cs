using System.ComponentModel.DataAnnotations;

namespace DotNetTruyen.Models
{
    public class ReadHistory
    {
        [Key]
        public Guid ReadHistoryId { get; set; }
        public Guid ChapterId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ReadDate { get; set; }
        public bool IsRead { get; set; }
        public Chapter Chapter { get; set; }
        public User User { get; set; }
    }
}
