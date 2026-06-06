using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AsyncKeyedLock;
using PhotoSauce.MagicScaler;
using static Vanara.PInvoke.Kernel32;

namespace FileExplorer.Common.Helper
{
    public class ImageCache
    {
		public static Task<ImageSource> TryGetValue(string filePath, MetadataProperties properties)
        {
            return TryGetValue(filePath, properties, null);
        }

		public static Task<ImageSource> TryGetValue(string filePath, ProcessImageSettings settings)
		{
			return TryGetValue(filePath, null, settings);
		}

		public static async Task<ImageSource> TryGetValue(string filePath, MetadataProperties properties = null, ProcessImageSettings settings = null)
        {
            string cacheKey = GetCacheKey(filePath, properties, settings);
            if (!Cache.Storage.Exists(cacheKey))
                return null;

            try
            {
                using (MemoryStream outputStream = new MemoryStream())
                {
                    await Task.Run(() =>
                    {
                        Cache.Storage.Download(cacheKey, outputStream);
                        outputStream.Position = 0;
                    });

                    return StreamToImage(outputStream);
                }
            }
            catch
            {
                return null;
            }
        }

		public static Task<ImageSource> GetOrAddValue(string filePath, Stream inputStream, MetadataProperties properties)
		{
			return GetOrAddValue(filePath, inputStream, properties, null);
		}

		public static Task<ImageSource> GetOrAddValue(string filePath, Stream inputStream, ProcessImageSettings settings)
		{
			return GetOrAddValue(filePath, inputStream, null, settings);
		}

		public static async Task<ImageSource> GetOrAddValue(string filePath, Stream inputStream, MetadataProperties properties = null, ProcessImageSettings settings = null)
        {
            ImageSource image = await TryGetValue(filePath, properties, settings);
            if (image != null)
                return image;
            
            try
            {
                using (MemoryStream outputStream = new MemoryStream())
                {
                    await Task.Run(() =>
                    {
                        ProcessImageSettings processImageSettings = settings != null ? settings : DefaultProcessImageSettings;
						MagicImageProcessor.ProcessImage(inputStream, outputStream, processImageSettings);
                        outputStream.Position = 0;

                        string cacheKey = GetCacheKey(filePath, properties, settings);
                        using (ImageKeyLockProvider.Lock(cacheKey))
                        {
                            LiteDB.BsonDocument metadata = properties?.ToDocument();
                            if (metadata == null)
                                metadata = new LiteDB.BsonDocument();

							FileInfo fileInfo = new FileInfo(filePath);
							metadata["Directory"] = fileInfo.DirectoryName;

							if (processImageSettings.Width > 0)
                                metadata["Width"] = processImageSettings.Width;
                            if (processImageSettings.Height > 0)
                                metadata["Height"] = processImageSettings.Height;

                            Cache.Storage.Upload(cacheKey, fileInfo.Name, outputStream, metadata);
                            outputStream.Position = 0;
                        }
                    });

                    return StreamToImage(outputStream);
                }
            }
            catch
            {
                return null;
            }
        }

        private static string GetCacheKey(string filePath, MetadataProperties properties = null, ProcessImageSettings settings = null)
        {
			FILE_STAT_INFORMATION fileInfo = GetFileInformationByName<FILE_STAT_INFORMATION>(filePath, FILE_INFO_BY_NAME_CLASS.FileStatByNameInfo);
            if (properties == null && settings == null)
                return $"{fileInfo.FileId}";

            List<string> keys = new List<string>();
            if (properties != null && properties.Count > 0)
                keys.Add(properties.ToString());
			if (settings != null && settings != DefaultProcessImageSettings)
				keys.Add(JsonSerializer.Serialize(settings));

            if (keys.Count == 0)
				return $"{fileInfo.FileId}";

			using (SHA256 sha256 = SHA256.Create())
            {
				StringBuilder stringBuilder = new StringBuilder();
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(String.Join("_", keys).ToLowerInvariant()));
                for (int i = 0; i < bytes.Length; i++)
                    stringBuilder.Append(bytes[i].ToString("x2"));

                return $"{fileInfo.FileId}_{stringBuilder}";
            }
        }

		private static ImageSource StreamToImage(Stream stream)
        {
            BitmapImage bitmapImage = new BitmapImage();

            bitmapImage.BeginInit();
            bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        private static readonly AsyncKeyedLocker<string> ImageKeyLockProvider = new AsyncKeyedLocker<string>();

        private static readonly ProcessImageSettings DefaultProcessImageSettings = new ProcessImageSettings { Width = 960, ResizeMode = CropScaleMode.Max };
    }
}
