using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Windows.Media;
using AsyncKeyedLock;
using FileExplorer.Core;
using FileExplorer.Model;
using Vanara.PInvoke;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;

namespace FileExplorer.Helpers
{
    public class IconHelper
    {
        public static ImageSource GetIcon(string path, SHIL iconSize = SHIL.SHIL_SMALL)
        {
            bool usePIDL = false;
            string iconPath = Path.GetExtension(path);
            if (String.IsNullOrEmpty(iconPath) || FileModel.SpecialExtenions.Any(x => x.OrdinalEquals(iconPath)))
            {
                iconPath = path;
                usePIDL = true;
            }

            string key = $"{iconPath}_{iconSize}";
            using (ImageKeyLockProvider.Lock(key))
            {
                if (ImageSourceDictionary.TryGetValue(key, out ImageSource imageSource))
                    return imageSource;

                SHFILEINFO fileInfo = new SHFILEINFO();
                if (usePIDL)
                {
                    PIDL pidl = ILCreateFromPath(iconPath);
                    SHGetFileInfo(pidl, FileAttributes.Normal, ref fileInfo, SHFILEINFO.Size, SHGFI.SHGFI_SYSICONINDEX | SHGFI.SHGFI_PIDL);
                }
                else
                {
                    SHGetFileInfo(iconPath, FileAttributes.Normal, ref fileInfo, SHFILEINFO.Size, SHGFI.SHGFI_SYSICONINDEX | SHGFI.SHGFI_USEFILEATTRIBUTES);
                }

                imageSource = GetIconCore(fileInfo, iconSize);
                if (imageSource != null)
                    ImageSourceDictionary.TryAdd(key, imageSource);

                return imageSource;
            }
        }        

        public static ImageSource GetIcon(SHFILEINFO fileInfo, SHIL iconSize = SHIL.SHIL_SMALL)
        {
            string key = $"{fileInfo.iIcon}_{iconSize}";
            if (fileInfo.hIcon != HICON.NULL)
                key = $"{fileInfo.hIcon.DangerousGetHandle()}_{fileInfo.iIcon}_{iconSize}";

            ImageSource imageSource;
            using (ImageKeyLockProvider.Lock(key))
            {
                if (ImageSourceDictionary.TryGetValue(key, out imageSource))
                    return imageSource;

                imageSource = GetIconCore(fileInfo, iconSize);
                if (imageSource != null)
                    ImageSourceDictionary.TryAdd(key, imageSource);
            }

            return imageSource;
        }

        private static ImageSource GetIconCore(SHFILEINFO fileInfo, SHIL iconSize = SHIL.SHIL_SMALL)
        {
            SHGetImageList(iconSize, ImageListId, out object image);
            IImageList imageList = image as IImageList;
            if (imageList == null)
                return null;

            SafeHICON hIcon = imageList.GetIcon(fileInfo.iIcon, IMAGELISTDRAWFLAGS.ILD_IMAGE);

            ImageSource imageSource = hIcon.ToBitmapSource();
            imageSource.Freeze();
            DestroyIcon(hIcon);

            return imageSource;
        }

        private static readonly ConcurrentDictionary<string, ImageSource> ImageSourceDictionary = new ConcurrentDictionary<string, ImageSource>();

        private static readonly AsyncKeyedLocker<string> ImageKeyLockProvider = new AsyncKeyedLocker<string>();

        private static readonly Guid ImageListId = typeof(IImageList).GUID;
    }
}
