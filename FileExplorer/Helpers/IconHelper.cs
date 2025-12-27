using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using AsyncKeyedLock;
using FileExplorer.Core;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.Gdi32;
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
            if (String.IsNullOrEmpty(iconPath) || FileSystemHelper.SpecialExtenions.Any(x => x.OrdinalEquals(iconPath)))
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

        public static async Task<ImageSource> GetIconAsync(string path, string extension, int dimension)
        {
            string iconPath = extension;
            if (String.IsNullOrEmpty(iconPath) || FileSystemHelper.SpecialExtenions.Any(x => x.OrdinalEquals(iconPath)))
                iconPath = path;

            int iconSize = Convert.ToInt32(Math.Ceiling(dimension * App.Dpi));
            if (iconSize > 256)
                iconSize = 256;

            string key = $"{iconPath}_{iconSize}";
            using (ImageKeyLockProvider.Lock(key))
            {
                if (ImageSourceDictionary.TryGetValue(key, out ImageSource imageSource))
                    return imageSource;
            }

            if (extension?.OrdinalEquals(".url") == true)
                return GetIconCore(path, iconSize, key);

            return await Task.Run(() => GetIconCore(path, iconSize, key));
        }

        private static ImageSource GetIconCore(SHFILEINFO fileInfo, SHIL iconSize = SHIL.SHIL_SMALL)
        {
            SHGetImageList(iconSize, ImageListId, out object image);
            IImageList imageList = image as IImageList;
            if (imageList == null)
                return null;

            using (SafeHICON hIcon = imageList.GetIcon(fileInfo.iIcon, IMAGELISTDRAWFLAGS.ILD_IMAGE))
            {
                ImageSource imageSource = hIcon.ToBitmapSource();
                imageSource.Freeze();

                return imageSource;
            }
        }

        private static ImageSource GetIconCore(string path, int iconSize, string key)
        {
            ImageSource imageSource = null;

            using(ShellItem shellItem = new ShellItem(path))
            {
                using (SafeHBITMAP hBitmap = shellItem.GetImage(new SIZE(iconSize, iconSize), ShellItemGetImageOptions.IconOnly))
                {
                    if (hBitmap?.IsInvalid == false)
                    {
                        imageSource = hBitmap.ToBitmapSource();
                        imageSource.Freeze();

                        using (ImageKeyLockProvider.Lock(key))
                        {
                            ImageSourceDictionary.TryAdd(key, imageSource);
                        }
                    }
                }
            }

            return imageSource;
        }

        private static readonly ConcurrentDictionary<string, ImageSource> ImageSourceDictionary = new ConcurrentDictionary<string, ImageSource>();

        private static readonly AsyncKeyedLocker<string> ImageKeyLockProvider = new AsyncKeyedLocker<string>();

        private static readonly Guid ImageListId = typeof(IImageList).GUID;
    }
}
