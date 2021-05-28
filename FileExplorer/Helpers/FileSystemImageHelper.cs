using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Alphaleonis.Win32.Filesystem;
using FileExplorer.Core;
using FileExplorer.Model;
using FileExplorer.Native;

namespace FileExplorer.Helpers
{
    public class FileSystemImageHelper
    {
        public static ImageSource GetImage(FileModel fileModel, IconSize iconSize)
        {
            string key = GetKey(fileModel, iconSize);
            if (ImageSourceCache.TryGetValue(key, out ImageSource imageSource))
                return imageSource;

            switch (iconSize)
            {
                case IconSize.Small:
                    imageSource = GetImage(fileModel.FullPath, 16);
                    break;

                case IconSize.Medium:
                    imageSource = GetImage(fileModel.FullPath, 32);
                    break;

                case IconSize.Large:
                    imageSource = GetImage(fileModel.FullPath, 128);
                    break;

                case IconSize.ExtraLarge:
                    imageSource = GetImage(fileModel.FullPath, 256);
                    break;
            }

            if (imageSource != null)
                ImageSourceCache.TryAdd(key, imageSource);

            return imageSource;
        }

        public static ImageSource GetImage(string path, int size)
        {
            IntPtr intPtr = FileOperation.GetIcon(path, size, size);
            if (intPtr == IntPtr.Zero)
                return null;

            ImageSource imageSource = Imaging.CreateBitmapSourceFromHBitmap(intPtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            if (imageSource != null)
                imageSource.Freeze();
            Shell32.DestroyIcon(intPtr);

            return imageSource;
        }

        protected static string GetKey(FileModel fileModel, IconSize iconSize)
        {
            string key = String.Format(KeyFormat, fileModel.FullPath, iconSize);
            if (fileModel.ParentPath == FileSystemHelper.NetworkPath)
                key = String.Format(KeyFormat, FileSystemHelper.ComputerPath, iconSize);

            if (fileModel.IsRoot)
                return key;
            if (fileModel.IsDrive)
                return key;
            if (fileModel.FullPath.OrdinalStartsWith(UserProfile))
                return key;
            if (fileModel.IsDirectory)
                return String.Format(KeyFormat, Windows, iconSize);
            if (fileModel.Extension.OrdinalEquals(".exe") ||
                fileModel.Extension.OrdinalEquals(".ico") ||
                fileModel.Extension.OrdinalEquals(".lnk") ||
                fileModel.Extension.OrdinalEquals(".cur"))
                return key;

            return String.Format(KeyFormat, fileModel.Extension, iconSize);
        }

        protected static string Windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        protected static string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        protected static ConcurrentDictionary<String, ImageSource> ImageSourceCache = new ConcurrentDictionary<String, ImageSource>();

        protected const string RootUrl = "pack://application:,,,/FileExplorer;component/Assets/Images/";

        protected const string KeyFormat = "{0}_{1}";

        static FileSystemImageHelper()
        {
            ImageSourceCache.TryAdd(String.Format(KeyFormat, FileSystemHelper.ComputerPath, IconSize.Small), new BitmapImage(new Uri(RootUrl + "Computer16.png")));
            ImageSourceCache.TryAdd(String.Format(KeyFormat, FileSystemHelper.ComputerPath, IconSize.Medium), new BitmapImage(new Uri(RootUrl + "Computer32.png")));
            ImageSourceCache.TryAdd(String.Format(KeyFormat, FileSystemHelper.QuickAccessPath, IconSize.Small), new BitmapImage(new Uri(RootUrl + "QuickAccess16.png")));
            ImageSourceCache.TryAdd(String.Format(KeyFormat, FileSystemHelper.QuickAccessPath, IconSize.Medium), new BitmapImage(new Uri(RootUrl + "QuickAccess32.png")));
            ImageSourceCache.TryAdd(String.Format(KeyFormat, FileSystemHelper.NetworkPath, IconSize.Small), new BitmapImage(new Uri(RootUrl + "Network16.png")));
            ImageSourceCache.TryAdd(String.Format(KeyFormat, FileSystemHelper.NetworkPath, IconSize.Medium), new BitmapImage(new Uri(RootUrl + "Network32.png")));

            ImageSourceCache.TryAdd(String.Format(KeyFormat, Windows, IconSize.Small), GetImage(Windows, 16));
            ImageSourceCache.TryAdd(String.Format(KeyFormat, Windows, IconSize.Medium), GetImage(Windows, 32));
            ImageSourceCache.TryAdd(String.Format(KeyFormat, Windows, IconSize.Large), GetImage(Windows, 128));
            ImageSourceCache.TryAdd(String.Format(KeyFormat, Windows, IconSize.ExtraLarge), GetImage(Windows, 256));
        }
    }
}
