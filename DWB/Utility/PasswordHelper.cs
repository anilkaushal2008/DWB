using Microsoft.AspNetCore.Identity;

namespace DWB.Utility
{
    public class PasswordHelper
    {
        public static string ConvertHashPassword(string plainPassword)
        {
            var hasher = new PasswordHasher<object>();
            return hasher.HashPassword("", plainPassword);
        }
        public static bool VerifyPassword(string hashedPassword, string plainPassword)
        {
            var hasher = new PasswordHasher<object>();
            var result = hasher.VerifyHashedPassword("", hashedPassword, plainPassword);
            return result == PasswordVerificationResult.Success;
        }
        public static bool VerifyPassword(string hashedPassword, string plainPassword, out bool needsRehash)
        {
            var hasher = new PasswordHasher<object>();
            var result = hasher.VerifyHashedPassword("", hashedPassword, plainPassword);
            needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
            return result == PasswordVerificationResult.Success || needsRehash;
        }
    }
}
