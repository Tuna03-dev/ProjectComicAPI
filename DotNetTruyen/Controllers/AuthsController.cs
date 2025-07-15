using DotNetTruyen.Models;
using DotNetTruyen.Services;
using DotNetTruyen.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DotNetTruyen.Controllers
{

    public class AuthsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly EmailService _emailService;
        private readonly OtpService _otpService;

        public AuthsController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            EmailService emailService,
            OtpService otpService
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _otpService = otpService;
        }

        [HttpGet("/login/")]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost("/login/")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.UserNameOrEmail, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (!result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.UserNameOrEmail);
                    if (user != null)
                    {
                        result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);
                    }
                    else
                    {
                        if (await _userManager.FindByNameAsync(model.UserNameOrEmail) == null)
                        {
                            ViewBag.ErrorMessage = "Tên đăng nhập không chính xác";
                            return View();
                        }
                    }
                }

                if (result.Succeeded)
                {
                    var userRoles = await _userManager.GetRolesAsync(await _userManager.GetUserAsync(User));

                    foreach (var roleName in userRoles)
                    {
                        var role = await _roleManager.FindByNameAsync(roleName);
                        if (role != null)
                        {
                            var roleClaims = await _roleManager.GetClaimsAsync(role);
                            if (roleClaims.Any(c => c.Type == "Permission" && c.Value == "Vào bảng điều khiển"))
                            {
                                if (returnUrl == ("/"))
                                {
                                    return LocalRedirect("/DashBoard");
                                }
                            }
                        }
                    }
                    return LocalRedirect(returnUrl);
                }
                if (result.IsLockedOut)
                {
                    ViewBag.ErrorMessage = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
                    return View();
                }
                if (result.IsNotAllowed)
                {
                    var email = model.UserNameOrEmail;
                    if(await _userManager.FindByNameAsync(model.UserNameOrEmail) != null)
                    {
                        var user = await _userManager.FindByNameAsync(model.UserNameOrEmail);
                        email = user.Email;
                    }
                    ViewBag.Email = email;
                    ViewBag.ConfirmEmailMessage = "Tài khoản của bạn cần xác thực email. Nhập mã Otp được gửi đến email của bạn để xác thực.";
                    string otp = _otpService.GenerateOtp(email);
                    await _emailService.SendEmailAsync(email, "Mã OTP xác thực tài khoản", $"Mã OTP của bạn là: <b>{otp}</b>");
                    return View("OtpConfirmRegister");
                }
                else
                {
                    ViewBag.ErrorMessage = "Mật khẩu không chính xác";
                    return View();
                }
            }
            ViewBag.ErrorMessage = "Đã xảy ra lỗi khi đăng nhập";
            return View();
        }

        [HttpGet("/loginWithGoogle/")]
        public async Task LoginWithGoogle(string returnUrl = null)
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action("GoogleResponse", new {returnUrl}),
                    Items = { { "prompt", "select_account" } }
                });
        }

        public async Task<IActionResult> GoogleResponse(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;

            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                ViewBag.ErrorMessage = "Đăng nhập bằng Google không thành công";
                return View("Login");
            }

            var claims = result.Principal.Identities.FirstOrDefault().Claims.Select(claim => new
            {
                claim.Issuer,
                claim.OriginalIssuer,
                claim.Type,
                claim.Value
            });

            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            string userName = email.Split("@")[0];
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                var newUser = new Models.User { UserName = userName, Email = email };
                newUser.NameToDisplay = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var creatResult = await _userManager.CreateAsync(newUser);
                if (!creatResult.Succeeded)
                {
                    ViewBag.ErrorMessage = "Đăng nhập bằng Google không thành công";
                    return View("Login");
                }

                var addRoleResult = await _userManager.AddToRoleAsync(newUser, "Reader");
                if (!addRoleResult.Succeeded)
                {
                    ViewBag.ErrorMessage = "Có lỗi khi cấp quyền cho tài khoản này";
                    return View("Login");
                }

                if (creatResult.Succeeded)
                {
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                    await _userManager.ConfirmEmailAsync(newUser, code);
                    await _signInManager.SignInAsync(newUser, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }
            else
            {
                if (existingUser.LockoutEnd != null && existingUser.LockoutEnd > DateTime.UtcNow)
                {
                    ViewBag.ErrorMessage = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
                    return View("Login");
                }
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                var userRoles = await _userManager.GetRolesAsync(existingUser);

                foreach (var roleName in userRoles)
                {
                    var role = await _roleManager.FindByNameAsync(roleName);
                    if (role != null)
                    {
                        var roleClaims = await _roleManager.GetClaimsAsync(role);
                        if (roleClaims.Any(c => c.Type == "Permission" && c.Value == "Vào bảng điều khiển"))
                        {
                            if (returnUrl == ("/"))
                            {
                                return LocalRedirect("/DashBoard");
                            }
                        }
                    }
                }
                return LocalRedirect(returnUrl);
            }
            return LocalRedirect("/login");
        }

        [HttpGet("/register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User { UserName = model.UserName, Email = model.Email,NameToDisplay = model.UserName };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    var addRoleResult = await _userManager.AddToRoleAsync(user, "Reader");
                    if (!addRoleResult.Succeeded)
                    {
                        ViewBag.ErrorRegisterMessage = "Có lỗi khi cấp quyền cho tài khoản này";
                        return View();
                    }

                    string otp = _otpService.GenerateOtp(model.Email);
                    await _emailService.SendEmailAsync(model.Email, "Mã OTP xác thực đăng ký tài khoản", $"Mã OTP của bạn là: <b>{otp}</b>");
                    ViewBag.Email = model.Email;
                    ViewBag.ConfirmEmailMessage = "Nhập mã Otp được gửi đến email của bạn để xác thực đăng ký.";
                    return View("OtpConfirmRegister");
                }
                else
                {
                    ViewBag.ErrorRegisterMessage = result.Errors.FirstOrDefault().Description;
                }
            }

            return View();
        }

        [HttpPost("/otpConfirmRegister")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OtpConfirmRegister(string emailConfirm, string otp)
        {
            ViewBag.Email = emailConfirm;
            ViewBag.Otp = otp;
            if (!string.IsNullOrEmpty(emailConfirm) && !string.IsNullOrEmpty(otp))
            {
                var user = await _userManager.FindByEmailAsync(emailConfirm);
                if (user == null)
                {
                    ViewBag.ErrorOtpConfirmMessage = "Email không tồn tại";
                    return View();
                }
                if (!_otpService.ValidateOtp(emailConfirm, otp))
                {
                    ViewBag.ErrorOtpConfirmMessage = "Mã OTP không hợp lệ hoặc đã hết hạn!";
                    return View();
                }
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var result = await _userManager.ConfirmEmailAsync(user, code);
                if (result.Succeeded)
                {
                    ViewBag.SuccessMessage = "Đăng ký thành công, hãy đăng nhập ngay";
                    return View("Login");
                }
            }
            ViewBag.ErrorOtpConfirmMessage = "Email không hợp lệ hoặc mã OTP không hợp lệ";
            return View();
        }


        [HttpGet("/forgotPassword")]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost("/forgotPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string emailConfirm)
        {
            ViewBag.Email = emailConfirm;
            if (!string.IsNullOrEmpty(emailConfirm))
            {
                var user = await _userManager.FindByEmailAsync(emailConfirm);
                if (user == null)
                {
                    ViewBag.ErrorEmailConfirmMessage = "Email không tồn tại";
                    return View();
                }

                string otp = _otpService.GenerateOtp(emailConfirm); 
                await _emailService.SendEmailAsync(emailConfirm, "Mã OTP đặt lại mật khẩu", $"Mã OTP của bạn là: <b>{otp}</b>");
                return View("OtpForgotPassword");
            }
            ViewBag.ErrorEmailConfirmMessage = "Email không hợp lệ";
            return View();
        }

        [HttpPost("/otpForgotPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OtpForgotPassword(string emailConfirm, string otp)
        {
            ViewBag.Email = emailConfirm;
            ViewBag.Otp = otp;
            if (!string.IsNullOrEmpty(emailConfirm) && !string.IsNullOrEmpty(otp))
            {
                var user = await _userManager.FindByEmailAsync(emailConfirm);
                if (user == null)
                {
                    ViewBag.ErrorOtpConfirmMessage = "Email không tồn tại";
                    return View();
                }
                if (!_otpService.IsValidate(emailConfirm, otp))
                {
                    ViewBag.ErrorOtpConfirmMessage = "Mã OTP không hợp lệ hoặc đã hết hạn!";
                    return View();
                }
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var result = await _userManager.ConfirmEmailAsync(user, code);
                if (result.Succeeded)
                {
                    return View("ResetPassword");
                }
            }
            ViewBag.ErrorOtpConfirmMessage = "Email không hợp lệ hoặc mã OTP không hợp lệ";
            return View();
        }

        [HttpPost("/resetPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel resetPasswordViewModel,string email, string otp)
        {
            ViewBag.Email = email;
            ViewBag.Otp = otp;
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(otp))
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    ViewBag.ErrorResetMessage = "Email không tồn tại";
                    return View();
                }
                if (!_otpService.IsValidate(email, otp))
                {
                    ViewBag.ErrorResetMessage = "Mã OTP không hợp lệ hoặc đã hết hạn!";
                    return View();
                }
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, code,resetPasswordViewModel.NewPassword);
                if (result.Succeeded)
                {
                    ViewBag.SuccessMessage = "Đặt lại mật khẩu thành công, hãy đăng nhập ngay";
                    return View("Login");
                }
            }
            ViewBag.ErrorResetMessage = "Email không hợp lệ hoặc mã OTP không hợp lệ";
            return View();
        }

        [HttpPost("/reSendOtpAuth")]
        public async Task<IActionResult> ReSendOtp(string emailConfirm,string request)
        {
            ViewBag.Email = emailConfirm;
            if (!string.IsNullOrEmpty(emailConfirm))
            {
                var user = await _userManager.FindByEmailAsync(emailConfirm);
                if (user == null)
                {
                    ViewBag.ErrorEmailConfirmMessage = "Email không tồn tại";
                    return View();
                }

                string otp = _otpService.GenerateOtp(emailConfirm);
                await _emailService.SendEmailAsync(emailConfirm, "Gửi lại mã OTP", $"Mã OTP mới của bạn là: <b>{otp}</b>");
                return View(request);
            }
            ViewBag.ErrorEmailConfirmMessage = "Email không hợp lệ";
            return View(request);

        }

        [HttpGet("/logout/")]
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync();
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");

        }

		[HttpGet("/accessDenied")]
		public IActionResult AccessDenied()
		{
			return View();

		}
	}
}
