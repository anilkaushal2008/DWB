using Microsoft.AspNetCore.Identity;

namespace DWB.Utility
{
    public class PasswordHelper
    {
        public static string ConvertHashPassword(string plainPassword)
        {
            var hasher = new PasswordHasher<object>();
            return hasher.HashPassword(null, plainPassword);
        }

        public static bool VerifyPassword(string hashedPassword, string plainPassword)
        {
            var hasher = new PasswordHasher<object>();
            var result = hasher.VerifyHashedPassword(null, hashedPassword, plainPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}
