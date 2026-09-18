using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using AsyncKeyedLock;
using DevExpress.Xpf.Editors;
using FileExplorer.Core;
using MahApps.Metro.IconPacks;
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

            using (ShellItem shellItem = new ShellItem(path))
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

	public class FontIconHelper
	{
		public static List<FontIcon> AwesomeIcons
		{
			get
			{
				if (awesomeIcons == null)
					awesomeIcons = GetIcons(typeof(PackIconFontAwesomeKind));

				return awesomeIcons;
			}
		}
		private static List<FontIcon> awesomeIcons;

		public static List<FontIcon> GetIcons(Type enumType)
		{
			List<FontIcon> fontIcons = new List<FontIcon>();
			Array array = Enum.GetValues(enumType);

			for (int i = 1; i < array.Length; i++)
			{
				object icon = array.GetValue(i);
				FieldInfo fieldInfo = enumType.GetField(icon.ToString());
				DescriptionAttribute attribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();

				string name = attribute.Description;
				int startIndex = name.IndexOf('(');
				if (startIndex != -1)
					name = name.Substring(0, startIndex);

				string description = attribute.Description;
				if (startIndex != -1)
				{
					int endIndex = description.IndexOf(')');
					if (endIndex == -1)
						endIndex = description.Length;

					description = description.Substring(startIndex + 1, endIndex - startIndex - 1);
				}

				FontIcon fontIcon = new FontIcon();
				fontIcon.Data = GetIconData((PackIconFontAwesomeKind)icon);
				fontIcon.Name = name.Trim();
				fontIcon.Description = description.Trim();

				fontIcons.Add(fontIcon);
			}

			return fontIcons;
		}

		public static string GetIconData(PackIconFontAwesomeKind packIcon)
        {
			string iconData = null;
			PackIconDataFactory<PackIconFontAwesomeKind>.DataIndex.Value?.TryGetValue(packIcon, out iconData);

            return iconData;
		}

		public static ImageSource GetIconImage(PackIconFontAwesomeKind packIcon, string iconColor)
        {
            string iconData = GetIconData(packIcon);
			return GetIconImage(iconData, iconColor);
		}

		public static ImageSource GetIconImage(string iconData, string iconColor)
		{
			if (iconData == null)
                return null;

			GeometryDrawing geometryDrawing = new GeometryDrawing
			{
				Geometry = Geometry.Parse(iconData),
				Brush = new SolidColorBrush(ColorHelper.ColorFromHex(iconColor))
			};

			DrawingImage drawingImage = new DrawingImage(geometryDrawing);
			drawingImage.Freeze();

			return drawingImage;
		}

		public class FontIcon
		{
			public string Name { get; set; }
			public string Data { get; set; }
			public string Description { get; set; }
		}
	}
}
