
using DotNetTruyen.Models;
using DotNetTruyen.Services;
using DotNetTruyen.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace DotNetTruyen.Controllers
{

    [Authorize]
    public class UserController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly EmailService _emailService;
        private readonly OtpService _otpService;
        private readonly IPhoToService _photoService;
        private readonly UserService _userService;

        public UserController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            EmailService emailService,
            OtpService otpService,
            IPhoToService photoService,
            UserService userService
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _otpService = otpService;
            _photoService = photoService;
            _userService = userService;
        }

        [HttpGet("/userProfile")]
        public IActionResult UserProfile()
        {
            ViewBag.ProfileTab = "active";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeDisplayName(string newDisplayName)
        {
            ViewBag.ProfileTab = "active";
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.NameToDisplay = newDisplayName;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    ViewBag.UpdateSuccessMess = "Thay đổi tên hiển thị thành công";
                    return View("UserProfile");
                }
            }
            ViewBag.UpdateErrorMess = "Thay đổi tên hiển thị không thành công";
            return View("UserProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserName(string newUserName,string confirmPassword)
        {
            ViewBag.ProfileTab = "active";
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                if (!await _userManager.HasPasswordAsync(user))
                {
                    ViewBag.UpdateErrorMess = "Tài khoản này chưa có mật khẩu";
                    return View("UserProfile");
                }
                if (!await _userManager.CheckPasswordAsync(user, confirmPassword))
                {
                    ViewBag.UpdateErrorMess = "Mật khẩu không chính xác";
                    return View("UserProfile");
                }
                var existingUser = await _userManager.FindByNameAsync(newUserName);
                if (existingUser != null)
                {
                    ViewBag.UpdateErrorMess = "Tên đăng nhập đã tồn tại.";
                    return View("UserProfile");
                }

                var result = await _userManager.SetUserNameAsync(user, newUserName);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    ViewBag.UpdateSuccessMess = "Thay đổi tên đăng nhập thành công";
                    return View("UserProfile");
                }
            }
            ViewBag.UpdateErrorMess = "Lỗi khi cập nhật tên đăng nhập.";
            return View("UserProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(UpdateProfileViewModel changePassword)
        {
            ViewBag.ProfileTab = "active";
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                if (!await _userManager.CheckPasswordAsync(user, changePassword.UpdatePasswordViewModel.OldPassword))
                {
                    ViewBag.UpdateErrorMess = "Mật khẩu không chính xác";
                    return View("UserProfile");
                }
                var result = await _userManager.ChangePasswordAsync(user, changePassword.UpdatePasswordViewModel.OldPassword, changePassword.UpdatePasswordViewModel.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    ViewBag.UpdateSuccessMess = "Thay đổi mật khẩu thành công";
                    return View("UserProfile");
                }
            }
            ViewBag.UpdateErrorMess = "Thay đổi mật khẩu không thành công";
            return View("UserProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPassword(UpdateProfileViewModel changePassword)
        {
            ViewBag.ProfileTab = "active";
            var user = await _userManager.GetUserAsync(User);
            if (user != null && !await _userManager.HasPasswordAsync(user))
            {
                var result = await _userManager.AddPasswordAsync(user,changePassword.AddPasswordViewModel.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    await _emailService.SendEmailAsync(user.Email, "Thêm mật khẩu", $"Tài khoản đăng nhập bằng google của bạn vừa được thêm mật khẩu");
                    ViewBag.UpdateSuccessMess = "Đã thêm mật khẩu cho tài khoản";
                    return View("UserProfile");
                }
            }
            ViewBag.UpdateErrorMess = "Thêm mật khẩu không thành công";
            return View("UserProfile");
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ChangeEmail(string newEmail,string confirmPassword)
        //{
        //    ViewBag.ProfileTab = "active";
        //    var user = await _userManager.GetUserAsync(User);
            
        //    if (user != null)
        //    {
        //        if(!await _userManager.HasPasswordAsync(user))
        //        {
        //            ViewBag.UpdateErrorMess = "Tài khoản này chưa có mật khẩu";
        //            return View("UserProfile");
        //        }
        //        if (!await _userManager.CheckPasswordAsync(user, confirmPassword))
        //        {
        //            ViewBag.UpdateErrorMess = "Mật khẩu không chính xác";
        //            return View("UserProfile");
        //        }
        //        string otp = _otpService.GenerateOtp(newEmail);
        //        await _emailService.SendEmailAsync(newEmail, "Mã OTP xác thực đổi email", $"Mã OTP của bạn là: <b>{otp}</b>");
        //        ViewBag.Email = newEmail;
        //        ViewBag.ConfirmEmailMessage = "Nhập mã Otp được gửi đến email mới của bạn.";
        //        return View("OtpChangeEmail");
        //    }
        //    ViewBag.UpdateErrorMess = "Thay đổi email không thành công";
        //    return View("UserProfile");
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> OtpChangeEmail(string newEmail, string otp)
        //{
        //    ViewBag.ProfileTab = "active";
        //    ViewBag.Email = newEmail;
        //    ViewBag.Otp = otp;
        //    if (!string.IsNullOrEmpty(newEmail) && !string.IsNullOrEmpty(otp))
        //    {
        //        var user = await _userManager.GetUserAsync(User);
        //        if (user == null)
        //        {
        //            ViewBag.ErrorOtpChangeEmail = "Phiên đăng nhập của bạn đã kết thúc";
        //            return View();
        //        }
        //        if (!_otpService.ValidateOtp(newEmail, otp))
        //        {
        //            ViewBag.ErrorOtpChangeEmail = "Mã OTP không hợp lệ hoặc đã hết hạn!";
        //            return View();
        //        }
        //        var code = await _userManager.GenerateChangeEmailTokenAsync(user,newEmail);
        //        var result = await _userManager.ChangeEmailAsync(user,newEmail, code);
        //        if (result.Succeeded)
        //        {
        //            await _signInManager.RefreshSignInAsync(user);
        //            ViewBag.UpdateSuccessMess = "Đã thay đổi email thành công";
        //            return View("UserProfile");
        //        }
        //    }
        //    ViewBag.UpdateErrorMess = "Thay đổi email không thành công";
        //    return View("UserProfile");
        //}

        //[HttpPost("/reSendOtpSetting")]
        //public async Task<IActionResult> ReSendOtp(string newEmail)
        //{
        //    ViewBag.Email = newEmail;
        //    if (!string.IsNullOrEmpty(newEmail))
        //    {
        //        var user = await _userManager.FindByEmailAsync(newEmail);
        //        if (user == null)
        //        {
        //            ViewBag.ErrorOtpChangeEmail = "Email không tồn tại";
        //            return View("OtpChangeEmail");
        //        }

        //        string otp = _otpService.GenerateOtp(newEmail);
        //        await _emailService.SendEmailAsync(newEmail, "Gửi lại mã OTP", $"Mã OTP mới của bạn là: <b>{otp}</b>");
        //        return View("OtpChangeEmail");
        //    }
        //    ViewBag.ErrorOtpChangeEmail = "Email không hợp lệ";
        //    return View("OtpChangeEmail");

        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(UploadAvatarViewModel uploadAvatarViewModel)
        {
            ViewBag.ProfileTab = "active";
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    if (uploadAvatarViewModel.AvatarImage != null)
                    {
                        var imageUrl = await _photoService.AddPhotoAsync(uploadAvatarViewModel.AvatarImage);
                        if (imageUrl != null)
                        {
                            user.ImageUrl = imageUrl;
                            var result = await _userManager.UpdateAsync(user);
                            if (result.Succeeded)
                            {
                                ViewBag.UpdateSuccessMess = "Đã thay đổi ảnh đại diện thành công";
                                return View("UserProfile");
                            }
                        }
                    }
                }
            }
            ViewBag.ErrorOtpChangeEmail = "Thay đổi ảnh đại diện không thành công";
            return View("UserProfile");
        }
    }
}
