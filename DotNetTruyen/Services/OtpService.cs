using Microsoft.Extensions.Caching.Memory;
using NuGet.Protocol;
using System.Security.Cryptography;

namespace DotNetTruyen.Services
{
    public class OtpService
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _expiry = TimeSpan.FromMinutes(5);

        public OtpService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string GenerateOtp(string email)
        {
            var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[4];
            rng.GetBytes(bytes);
            int value = BitConverter.ToInt32(bytes, 0) & int.MaxValue;
            string otp = (value % 1000000).ToString("D6");
            _cache.Set(email, otp, _expiry);
            return otp;
        }
        public bool IsValidate(string email, string otp)
        {
            if (_cache.TryGetValue(email, out string cachedOtp))
            {
                if (cachedOtp == otp)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ValidateOtp(string email, string otp)
        {
            if (_cache.TryGetValue(email, out string cachedOtp))
            {
                if (cachedOtp == otp)
                {
                    _cache.Remove(email);
                    return true;
                }
            }
            return false;
        }

        public string RandomOtp()
        {
            var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[4];
            rng.GetBytes(bytes);
            int value = BitConverter.ToInt32(bytes, 0) & int.MaxValue;
            string otp = (value % 1000000).ToString("D6");
            return otp;
        }
    }
}
