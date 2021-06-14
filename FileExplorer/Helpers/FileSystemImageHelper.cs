using System;
using System.Collections.Concurrent;
using System.Windows.Media;
using Alphaleonis.Win32.Filesystem;
using FileExplorer.Core;
using FileExplorer.Model;
using FileExplorer.Native;

namespace FileExplorer.Helpers
{
    public class FileSystemImageHelper
    {
        public static ImageSource GetImage(string path, IconSize iconSize)
        {
            if (!File.Exists(path))
                return SafeNativeMethods.GetIcon(Path.GetExtension(path), iconSize.ToSHIL());

            FileModel fileModel = FileModel.FromPath(path);
            return GetImage(fileModel, iconSize);
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
                            path = KnownFolders.QuickAccess;
                            break;
                        case FileSystemHelper.ComputerPath:
                            path = KnownFolders.Computer;
                            break;
                        case FileSystemHelper.NetworkPath:
                            path = KnownFolders.Network;
                            break;
                    }

                    if (fileModel.ParentPath == FileSystemHelper.NetworkPath)
                        path = KnownFolders.Computer;
                }
                else if (FileSystemHelper.IsNetworkShare(fileModel.FullPath))
                    path = @"\\a\b";
                else if (fileModel.IsDrive || fileModel.ParentPath.OrdinalEquals(UserProfile))
                    path = fileModel.FullPath;
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

        protected static string Windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        protected static string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        protected static LockProvider ImageKeyLockProvider = new LockProvider();

        protected static ConcurrentDictionary<String, ImageSource> ImageSourceCache = new ConcurrentDictionary<String, ImageSource>();

        protected const string KeyFormat = "{0}_{1}";
    }
}
