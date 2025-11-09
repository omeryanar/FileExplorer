using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Alphaleonis.Win32.Filesystem;
using AsyncKeyedLock;
using FileExplorer.Core;
using FileExplorer.Model;
using FileExplorer.Native;
using FileExplorer.Properties;
using PhotoSauce.MagicScaler;

namespace FileExplorer.Helpers
{
    public class FileSystemImageHelper
    {
        public static ImageSource GetImage(string path, IconSize iconSize)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {

                FileModel fileModel = FileModel.FromPath(path);
                return GetImage(fileModel, iconSize);
            }

            return SafeNativeMethods.GetIcon(Path.GetExtension(path), iconSize.ToSHIL());
        }

        public static ImageSource GetImage(FileModel fileModel, IconSize iconSize)
        {
            string path = Windows;
            if (fileModel.IsDirectory)
            {
                if (fileModel.IsRoot)
                {
                    switch (fileModel.FullPath)
                    {
                        case FileSystemHelper.QuickAccessPath:
                            path = RootFolders.QuickAccess;
                            break;
                        case FileSystemHelper.ComputerPath:
                            path = RootFolders.Computer;
                            break;
                        case FileSystemHelper.NetworkPath:
                            path = RootFolders.Network;
                            break;
                    }

                    if (fileModel.ParentPath == FileSystemHelper.NetworkPath)
                        path = RootFolders.Computer;
                }
                else if (FileSystemHelper.IsNetworkShare(fileModel.FullPath))
                    path = @"\\a\b";
                else if (fileModel.IsDrive || File.Exists(Path.Combine(fileModel.FullPath, "desktop.ini")))
                    path = fileModel.FullPath;
            }
            else if (fileModel.Extension.OrdinalEquals(".url"))
            {
                path = fileModel.Extension;

                foreach (string line in File.ReadLines(fileModel.FullPath))
                {
                    if (line.OrdinalStartsWith("IconFile="))
                    {
                        string[] split = line.Split('=');
                        if (split.Length == 2)
                        {
                            path = split[1];
                            break;
                        }
                    }
                }                    
            }
            else
            {
                if (fileModel.Extension.OrdinalEquals(".exe") ||
                    fileModel.Extension.OrdinalEquals(".ico") ||
                    fileModel.Extension.OrdinalEquals(".lnk") ||
                    fileModel.Extension.OrdinalEquals(".cur"))
                    path = fileModel.FullPath;
                else
                    path = fileModel.Extension;
            }

            string key = String.Format(KeyFormat, path, iconSize);
            if (ImageSourceCache.TryGetValue(key, out ImageSource imageSource))
                return imageSource;

            using (ImageKeyLockProvider.Lock(key))
            {
                if (ImageSourceCache.TryGetValue(key, out imageSource))
                    return imageSource;

                imageSource = SafeNativeMethods.GetIcon(path, iconSize.ToSHIL());
                if (imageSource != null)
                    ImageSourceCache.TryAdd(key, imageSource);
            }

            return imageSource;
        }

        public static async Task<BitmapImage> GetThumbnailImage(string path)
        {
            BitmapImage bitmapImage = null;

            await Task.Run(() =>
            {
                try
                {
                    ProcessImageSettings settings = new ProcessImageSettings();
                    settings.Anchor = (CropAnchor)Settings.Default.ThumbnailAnchor;
                    settings.ResizeMode = (CropScaleMode)Settings.Default.ThumbnailMode;
                    settings.Width = Settings.Default.ThumbnailHeight;
                    settings.Height = Settings.Default.ThumbnailHeight;

                    System.IO.MemoryStream stream = new System.IO.MemoryStream();
                    MagicImageProcessor.ProcessImage(path, stream, settings);
                    stream.Position = 0;

                    bitmapImage = new BitmapImage
                    {
                        CreateOptions = BitmapCreateOptions.IgnoreColorProfile,
                        CacheOption = BitmapCacheOption.OnLoad
                    };

                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }
                catch
                {
                    bitmapImage = null;
                }
            });

            return bitmapImage;
        }

        public const string SupportedThumbnailImageFormats = @"^.+\.(?:(?:avif)|(?:bmp)|(?:dip)|(?:gif)|(?:heic)|(?:heif)|(?:jfif)|(?:jpe)|(?:jpe?g)|(?:jxr)|(?:png)|(?:rle)|(?:tiff?)|(?:wdp)|(?:webp))$";

        protected static string Windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        protected static AsyncKeyedLocker<string> ImageKeyLockProvider = new(o =>
        {
            o.PoolSize = 20;
            o.PoolInitialFill = 1;
        });

        protected static ConcurrentDictionary<String, ImageSource> ImageSourceCache = new ConcurrentDictionary<String, ImageSource>();

        protected const string KeyFormat = "{0}_{1}";
    }
}
