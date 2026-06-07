using System.Security.Cryptography;
using System.Text;

namespace AI_Resume_Analyzer_API.Infrastructure.Security
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            var hashed = HashPassword(password);
            return hashed == hashedPassword;
        }
    }
}
