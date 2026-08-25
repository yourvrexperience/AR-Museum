using UnityEngine;
using System;
using System.Text;
using System.Security.Cryptography;
using System.Linq;

namespace yourvrexperience.Utils
{
	public static class SHAEncryption
	{
        public static int GetStableHash(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Take first 4 bytes and convert to int
                return BitConverter.ToInt32(data, 0);
            }
        }

        public static string GenerateShortHash(string email, string code, int total)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string combined = email + code;
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                string base64Hash = Convert.ToBase64String(hashBytes);

                // Remove non-alphanumeric characters for easy typing
                string alphanumericHash = RemoveSpecialCharacters(base64Hash);

                // Return only the first total characters
                return alphanumericHash.Substring(0, total);
            }
        }

        static string RemoveSpecialCharacters(string input)
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c)) // Only keep letters and digits
                {
                    result.Append(c);
                }
            }
            return result.ToString();
        }

        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = Encoding.UTF8.GetBytes(password + salt);
                var hash = sha256.ComputeHash(saltedPassword);
                return Convert.ToBase64String(hash);
            }
        }

        public static string GenerateSalt()
        {
            
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        public static string GenerateSaltWithTimestamp(string salt, long timestamp)
        {
            // Get the current timestamp
            string saltWithTimestamp = $"{salt}{timestamp}";

            // Hash the salt and timestamp together
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltWithTimestamp));
                return Convert.ToBase64String(hashBytes); // Opaque combined value
            }
        }

        public static string HashWithSalt(string input, string combinedSalt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] combinedBytes = Encoding.UTF8.GetBytes(input + combinedSalt);
                byte[] hashBytes = sha256.ComputeHash(combinedBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        public static long GetUniqueId(string username, int length = 9)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Compute SHA-256 hash
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(username));

                // Convert hash to Base64 string
                string base64Hash = Convert.ToBase64String(hashBytes);

                // Remove non-digit characters and take the first 'length' digits
                string numericPart = string.Concat(base64Hash.Where(char.IsDigit));
                string truncated;
                if (username.Length > length)
                {
                    truncated = numericPart.Substring(0, Math.Min(numericPart.Length, length));
                }
                else
                {
                    truncated = numericPart.Substring(0, Math.Min(numericPart.Length, username.Length));
                }

                return long.Parse(truncated);
            }
        }
    }
}