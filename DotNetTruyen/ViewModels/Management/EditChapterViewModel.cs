using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace DotNetTruyen.ViewModels.Management
{
    public class EditChapterViewModel
    {
        public Guid Id { get; set; }
        public Guid ComicId { get; set; }

        
        public string? ChapterTitle { get; set; }

        [Required(ErrorMessage = "Số chapter là bắt buộc.")]
        [Range(0, int.MaxValue, ErrorMessage = "Số chapter phải lớn hơn hoặc bằng 0.")]
        public int ChapterNumber { get; set; }

        public DateTime? PublishedDate { get; set; }

        public List<ChapterImageViewModel> ExistingImages { get; set; } = new List<ChapterImageViewModel>();

        public List<IFormFile>? Images { get; set; }
    }
}
