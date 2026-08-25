using UnityEngine;
using System;
using System.Text;
using System.Security.Cryptography;
using System.Linq;
using System.IO;

namespace yourvrexperience.Utils
{
	public static class AESEncryption
	{
		public struct AESEncryptedText
		{
			public string IV;
			public byte[] EncryptedData;
		}

		public static AESEncryptedText Encrypt(byte[] plainData, string password)
		{
			using (var aes = Aes.Create())
			{
				aes.GenerateIV();
				aes.Key = ConvertToKeyBytes(aes, password);

				var aesEncryptor = aes.CreateEncryptor();
				var encryptedBytes = aesEncryptor.TransformFinalBlock(plainData, 0, plainData.Length);

				return new AESEncryptedText
				{
					IV = Convert.ToBase64String(aes.IV),
					EncryptedData = encryptedBytes
				};
			}
		}

		public static AESEncryptedText Encrypt(byte[] plainData, string password, byte[] iv)
		{
			using (var aes = Aes.Create())
			{
				aes.Key = ConvertToKeyBytes(aes, password);
				aes.IV = iv;

				var aesEncryptor = aes.CreateEncryptor(aes.Key, aes.IV);
				var encryptedBytes = aesEncryptor.TransformFinalBlock(plainData, 0, plainData.Length);

				return new AESEncryptedText
				{
					IV = Convert.ToBase64String(aes.IV),
					EncryptedData = encryptedBytes
				};
			}
		}

		public static byte[] Decrypt(byte[] encryptedData, string iv, string password)
		{
			using (Aes aes = Aes.Create())
			{
				var ivBytes = Convert.FromBase64String(iv);
				var encryptedTextBytes = encryptedData;

				var decryptor = aes.CreateDecryptor(ConvertToKeyBytes(aes, password), ivBytes);
				var decryptedBytes = decryptor.TransformFinalBlock(encryptedTextBytes, 0, encryptedTextBytes.Length);

				return decryptedBytes;
			}
		}

		private static byte[] ConvertToKeyBytes(SymmetricAlgorithm algorithm, string password)
		{
			algorithm.GenerateKey();

			var keyBytes = Encoding.UTF8.GetBytes(password);
			var validKeySize = algorithm.Key.Length;

			if (keyBytes.Length != validKeySize)
			{
				var newKeyBytes = new byte[validKeySize];
				Array.Copy(keyBytes, newKeyBytes, Math.Min(keyBytes.Length, newKeyBytes.Length));
				keyBytes = newKeyBytes;
			}

			return keyBytes;
		}

		// This method will encrypt the byte array using AES.
		public static byte[] Encrypt(byte[] dataToEncrypt, byte[] key, byte[] iv)
		{
			using (Aes aes = Aes.Create())
			{
				aes.Key = key;
				aes.IV = iv;

				using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
				using (var memoryStream = new MemoryStream())
				{
					using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
					{
						cryptoStream.Write(dataToEncrypt, 0, dataToEncrypt.Length);
						cryptoStream.FlushFinalBlock();
						return memoryStream.ToArray();
					}
				}
			}
		}

		// This method will decrypt the byte array back to its original form.
		public static byte[] Decrypt(byte[] encryptedData, byte[] key, byte[] iv)
		{
			using (Aes aes = Aes.Create())
			{
				aes.Key = key;
				aes.IV = iv;

				using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
				using (var memoryStream = new MemoryStream(encryptedData))
				using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
				using (var resultStream = new MemoryStream())
				{
					cryptoStream.CopyTo(resultStream);
					return resultStream.ToArray();
				}
			}
		}

		// Generate a random key and IV (for demonstration purposes).
		public static void GenerateKeyAndIV(out byte[] key, out byte[] iv)
		{
			using (Aes aes = Aes.Create())
			{
				aes.GenerateKey();
				aes.GenerateIV();
				key = aes.Key;
				iv = aes.IV;
			}
		}

		public static string GenerateIVFromPassword(string password)
		{
			using (SHA256 sha256 = SHA256.Create())
			{
				byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password)).Take(16).ToArray();

				// Convert the byte array to a Base64 string
				return Convert.ToBase64String(hash);
			}
		}
	}
}