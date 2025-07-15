using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace DotNetTruyen.Models
{
    public class BaseEnity<TId>
    {
        [Key]
        public TId Id { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
