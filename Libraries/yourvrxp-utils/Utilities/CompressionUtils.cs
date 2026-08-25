using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Unity.SharpZipLib.Zip;
using UnityEngine;
using UnityEngine.Networking;

namespace yourvrexperience.Utils
{
	public static class CompressionUtils
	{
		public static byte[] CompressByteArray(byte[] data)
		{
			using (var compressedStream = new MemoryStream())
			using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
			{
				zipStream.Write(data, 0, data.Length);
				zipStream.Close();
				return compressedStream.ToArray();
			}
		}

		public static byte[] DecompressByteArray(byte[] data)
		{
			using (var compressedStream = new MemoryStream(data))
			using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
			using (var resultStream = new MemoryStream())
			{
				zipStream.CopyTo(resultStream);
				return resultStream.ToArray();
			}
		}

		public static byte[] CompressWithBrotli(byte[] data)
		{
			using (var compressedStream = new MemoryStream())
			{
				using (var brotliStream = new BrotliStream(compressedStream, CompressionMode.Compress))
				{
					brotliStream.Write(data, 0, data.Length);
				}
				return compressedStream.ToArray();
			}
		}

		public static byte[] DecompressWithBrotli(byte[] compressedData)
		{
			using (var compressedStream = new MemoryStream(compressedData))
			using (var brotliStream = new BrotliStream(compressedStream, CompressionMode.Decompress))
			using (var resultStream = new MemoryStream())
			{
				brotliStream.CopyTo(resultStream);
				return resultStream.ToArray();
			}
		}

		public static void CompressFile(string sourceFile, string compressedFile)
		{
			using (FileStream sourceFileStream = File.OpenRead(sourceFile))
			using (FileStream compressedFileStream = File.Create(compressedFile))
			using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
			{
				sourceFileStream.CopyTo(compressionStream);
			}
		}

		public static void DecompressFile(string compressedFile, string targetFile)
		{
			using (FileStream compressedFileStream = File.OpenRead(compressedFile))
			using (FileStream targetFileStream = File.Create(targetFile))
			using (GZipStream decompressionStream = new GZipStream(compressedFileStream, CompressionMode.Decompress))
			{
				decompressionStream.CopyTo(targetFileStream);
			}
		}

		public static IEnumerator WebUploadCompressedFile(string url, string filePath)
		{
			byte[] fileData = File.ReadAllBytes(filePath);
			UnityWebRequest request = UnityWebRequest.Put(url, fileData);
			request.SetRequestHeader("Content-Type", "application/octet-stream");
			yield return request.SendWebRequest();

			if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
			{
				Debug.Log(request.error);
			}
			else
			{
				Debug.Log("Upload completed!");
			}
		}

		public static List<(string FileName, byte[] Content)> ReadZipFromBytes(byte[] zipData)
		{
			var files = new List<(string FileName, byte[] Content)>();

			using (var memoryStream = new MemoryStream(zipData))
			using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
			{
				foreach (var entry in archive.Entries)
				{
					using (var entryStream = entry.Open())
					using (var ms = new MemoryStream())
					{
						entryStream.CopyTo(ms);
						files.Add((entry.FullName, ms.ToArray()));
					}
				}
			}

			return files;
		}

		public static List<(string FileName, byte[] Content)> ReadEncryptedZipFromBytes(byte[] zipData, string password)
		{
			var files = new List<(string FileName, byte[] Content)>();

			using (var memoryStream = new MemoryStream(zipData))
			using (var zipFile = new Unity.SharpZipLib.Zip.ZipFile(memoryStream))
			{
				if (!string.IsNullOrEmpty(password))
					zipFile.Password = password;

				foreach (ZipEntry entry in zipFile)
				{
					if (!entry.IsFile) continue;

					using (var stream = zipFile.GetInputStream(entry))
					using (var ms = new MemoryStream())
					{
						stream.CopyTo(ms);
						files.Add((entry.Name, ms.ToArray()));
					}
				}
			}

			return files;
		}

	}
}
