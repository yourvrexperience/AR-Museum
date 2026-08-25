using UnityEngine;
using System;
using System.Text;
using System.Security.Cryptography;
using System.Linq;
using System.IO;

namespace yourvrexperience.Utils
{
    public static class RJEncryptor
    {
		public static string DecryptStringWithKey(string _textToDecrypt, string _key)
		{
			string sEncryptedString = _textToDecrypt;

			var myRijndael = new RijndaelManaged()
			{
				Padding = PaddingMode.Zeros,
				Mode = CipherMode.CBC,
				KeySize = 256,
				BlockSize = 256
			};

			string[] elementsEncrypted = _textToDecrypt.Split('|');

			byte[] sEncrypted = Convert.FromBase64String(elementsEncrypted[0]);
			byte[] IV = Convert.FromBase64String(elementsEncrypted[1]);
			byte[] key = Encoding.ASCII.GetBytes(_key);
			var decryptor = myRijndael.CreateDecryptor(key, IV);

			byte[] fromEncrypt = new byte[sEncrypted.Length];

			MemoryStream msDecrypt = new MemoryStream(sEncrypted);
			CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);

			csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);

			string encrytpedText = Encoding.ASCII.GetString(fromEncrypt).Trim('\x0');
			return (encrytpedText);
		}

		public static string EncryptStringWithKey(string _textToEncrypt, string _key)
		{
			string sToEncrypt = _textToEncrypt;

			var myRijndael = new RijndaelManaged()
			{
				Padding = PaddingMode.Zeros,
				Mode = CipherMode.CBC,
				KeySize = 256,
				BlockSize = 256
			};

			byte[] key = Encoding.ASCII.GetBytes(_key);
			string ivGenerated = yourvrexperience.Utils.Utilities.RandomCodeIV(32);
			byte[] IV = Encoding.ASCII.GetBytes(ivGenerated);
			var encryptor = myRijndael.CreateEncryptor(key, IV);

			var msEncrypt = new MemoryStream();
			var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

			var toEncrypt = Encoding.ASCII.GetBytes(sToEncrypt);

			csEncrypt.Write(toEncrypt, 0, toEncrypt.Length);
			csEncrypt.FlushFinalBlock();

			var encrypted = msEncrypt.ToArray();

			string encryptedResult = "";
			encryptedResult = Convert.ToBase64String(encrypted) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(ivGenerated));
			return (encryptedResult);
		}
    }
}