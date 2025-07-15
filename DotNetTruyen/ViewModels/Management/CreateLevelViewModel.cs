using System.ComponentModel.DataAnnotations;

namespace DotNetTruyen.ViewModels.Management
{
    public class CreateLevelViewModel
    {
        [Required(ErrorMessage = "Số cấp độ là bắt buộc.")]
        [Range(0, int.MaxValue, ErrorMessage = "Số cấp độ phải lớn hơn hoặc bằng 0.")]
        public int LevelNumber { get; set; }

        [Required(ErrorMessage = "Tên cấp độ là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên cấp độ không được vượt quá 100 ký tự.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Exp Required là bắt buộc.")]
        [Range(0, int.MaxValue, ErrorMessage = "Exp Required phải lớn hơn hoặc bằng 0.")]
        public int ExpRequired { get; set; }
    }
}
