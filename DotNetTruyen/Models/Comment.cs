using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DotNetTruyen.Models
{
    public class Comment
    {

        
        public Guid Id { get; set; }
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public Guid ComicId { get; set; }

        [Column("ReplyId")]
        public Guid? CommentId { get; set; }
        public Guid UserId { get; set; }
        public Comic Comic { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public User User { get; set; }
        [NotMapped]
        public int? ReplyCount { get; set; }
		[NotMapped]
		public string? UserLevel { get; set; }
	}
}
