using System;
using System.Security.Cryptography;
using System.Text;

namespace Village_Manager.Utils
{
    public static class PasswordHelper
    {
        // Hash a password with a salt using PBKDF2
        public static string HashPassword(string password)
        {
            // Generate a random salt
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password with the salt
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            // Combine salt and hash
            byte[] hashBytes = new byte[48];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 32);

            // Convert to base64 for storage
            return Convert.ToBase64String(hashBytes);
        }

        // Verify a password against a stored hash
        public static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                byte[] hashBytes = Convert.FromBase64String(storedHash);
                if (hashBytes.Length != 48)
                {
                    // Fallback to plain text comparison for legacy or invalid hashes
                    return password == storedHash;
                }
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);

                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                byte[] hash = pbkdf2.GetBytes(32);

                for (int i = 0; i < 32; i++)
                {
                    if (hashBytes[i + 16] != hash[i])
                        return false;
                }
                return true;
            }
            catch (FormatException)
            {
                // If not base64, fallback to plain text comparison (for legacy passwords)
                return password == storedHash;
            }
        }
    }
} 