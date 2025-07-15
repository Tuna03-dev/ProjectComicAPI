using System.ComponentModel.DataAnnotations;

namespace DotNetTruyen.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Phải nhập {0}")]
        [Display(Name = "Địa chỉ email hoặc tên tài khoản", Prompt = "Địa chỉ email hoặc username")]
        public string UserNameOrEmail { get; set; }


        [Required(ErrorMessage = "Phải nhập {0}")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu", Prompt = "Mật khẩu")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
