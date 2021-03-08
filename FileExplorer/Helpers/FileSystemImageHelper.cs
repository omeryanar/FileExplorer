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
            if (fileModel.FullPath == FileSystemHelper.ComputerPath)
                return iconSize == IconSize.Small ? ComputerSmall : ComputerMedium;
            if (fileModel.FullPath == FileSystemHelper.QuickAccessPath)
                return iconSize == IconSize.Small ? QuickAccessSmall : QuickAccessMedium;

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
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHBitmap(intPtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            if (imageSource != null)
                imageSource.Freeze();
            Shell32.DestroyIcon(intPtr);

            return imageSource;
        }

        protected static string GetKey(FileModel fileModel, IconSize iconSize)
        {
            string key = String.Format("{0}_{1}", fileModel.FullPath, iconSize);

            if (fileModel.IsDrive)
                return key;
            if (fileModel.FullPath.OrdinalStartsWith(UserProfile))
                return key;
            if (fileModel.Extension.OrdinalEquals(".exe") ||
                fileModel.Extension.OrdinalEquals(".ico") ||
                fileModel.Extension.OrdinalEquals(".lnk") ||
                fileModel.Extension.OrdinalEquals(".cur"))
                return key;

            if (fileModel.IsDirectory)
                return String.Format("{0}_{1}", Windows, iconSize);
            else
                return String.Format("{0}_{1}", fileModel.Extension, iconSize);
        }

        protected static string Windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        protected static string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        protected static ConcurrentDictionary<String, ImageSource> ImageSourceCache = new ConcurrentDictionary<String, ImageSource>();

        protected static ImageSource ComputerSmall = new BitmapImage(new Uri("pack://application:,,,/FileExplorer;component/Assets/Images/Computer16.png"));

        protected static ImageSource ComputerMedium = new BitmapImage(new Uri("pack://application:,,,/FileExplorer;component/Assets/Images/Computer32.png"));

        protected static ImageSource QuickAccessSmall = new BitmapImage(new Uri("pack://application:,,,/FileExplorer;component/Assets/Images/QuickAccess16.png"));

        protected static ImageSource QuickAccessMedium = new BitmapImage(new Uri("pack://application:,,,/FileExplorer;component/Assets/Images/QuickAccess32.png"));
    }
}
