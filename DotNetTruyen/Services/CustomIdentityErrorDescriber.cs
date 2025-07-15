using Microsoft.AspNetCore.Identity;

namespace DotNetTruyen.Services
{
    public class CustomIdentityErrorDescriber : IdentityErrorDescriber
    {

        public override IdentityError DuplicateUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = $"Tên đăng nhập '{userName}' đã được sử dụng."
            };
        }

        public override IdentityError InvalidUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(InvalidUserName),
                Description = $"Tên đăng nhập '{userName}' chứa ký tự không hợp lệ."
            };
        }

        public override IdentityError DuplicateEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = $"Email '{email}' đã được sử dụng."
            };
        }
    }

}
