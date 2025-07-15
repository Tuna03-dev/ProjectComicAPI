using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DotNetTruyen.Models
{
    public class Advertisement : BaseEnity<Guid>
    {
        [Required(ErrorMessage = "Vị trí là bắt buộc")]
        [RegularExpression("^(top|bot|left|right)$", ErrorMessage = "Vị trí phải là 'top', 'bot', 'left', hoặc 'right'")]
        public string Title { get; set; }
        public string LinkTo { get; set; }
        public string ImageUrl { get; set; }
    }
}
