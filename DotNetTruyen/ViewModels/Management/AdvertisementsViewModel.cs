using System.ComponentModel.DataAnnotations;

namespace DotNetTruyen.ViewModels.Management
{
    public class AdvertisementsViewModel
    {
        public Guid Id { get; set; }
        [Required(ErrorMessage = "Vị trí là bắt buộc")]
        [RegularExpression("^(top|bot|left|right)$", ErrorMessage = "Vị trí phải là 'top', 'bot', 'left', hoặc 'right'")]
        public string Title { get; set; }
        public IFormFile? ImageUrl { get; set; }
        public string? LinkTo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public string? ImageUrlPath { get; set; }
    }
}
