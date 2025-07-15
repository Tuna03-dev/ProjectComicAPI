using System.ComponentModel.DataAnnotations;

namespace DotNetTruyen.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Phải nhập {0}")]
        [Display(Name = "Tên tài khoản", Prompt = "Tên tài khoản")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Phải nhập {0}")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu", Prompt = "Mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).{6,}$",
        ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ thường, chữ hoa, số và ký tự đặc biệt.")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Phải nhập {0}")]
        [EmailAddress(ErrorMessage = "Vui lòng nhập đúng định dạng email.")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Địa chỉ email", Prompt = "Địa chỉ email")]
        public string Email { get; set; }
    }
}
