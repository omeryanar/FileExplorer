using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Media;
using Vanara.PInvoke;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;
using static Vanara.PInvoke.User32;

namespace FileExplorer.Native
{
    public static class SafeNativeMethods
    {
        public static HWND GetActiveWindowHandle()
        {
            return GetActiveWindow();
        }   

        public static IShellItem CreateShellItem(string path)
        {
            if (!PathFileExists(path))
                return null;

            return ShellUtil.GetShellItemForPath(path);
        }

        public static void MultiFileProperties(IDataObject data)
        {
            SHMultiFileProperties(data, 0);
        }

        public static ImageSource GetIcon(string path, SHIL size)
        {
            IntPtr result = IntPtr.Zero;
            SHFILEINFO shFileInfo = new SHFILEINFO();

            if (path.StartsWith(".") || path.StartsWith(@"\\"))
            {
                result = SHGetFileInfo(path, FileAttributes.Normal, ref shFileInfo, FileInfoSize, SHGFI.SHGFI_SYSICONINDEX | SHGFI.SHGFI_USEFILEATTRIBUTES);
            }
            else
            {
                PIDL pidl = ILCreateFromPath(path);
                result = SHGetFileInfo(pidl, FileAttributes.Normal, ref shFileInfo, FileInfoSize, SHGFI.SHGFI_SYSICONINDEX | SHGFI.SHGFI_PIDL);
            }

            if (result == IntPtr.Zero)
                return null;

            if (SHGetImageList(size, ImageListId, out object image) != HRESULT.S_OK)
                return null;

            IImageList iImageList = image as IImageList;
            if (iImageList == null)
                return null;

            SafeHICON hIcon = iImageList.GetIcon(shFileInfo.iIcon, IMAGELISTDRAWFLAGS.ILD_IMAGE);            

            ImageSource imageSource = hIcon.ToBitmapSource();
            imageSource.Freeze();
            DestroyIcon(hIcon);

            return imageSource;
        }

        public static MemoryStream CreateShellIdList(string[] paths)
        {
            byte[][] pidls = new byte[paths.Length][];

            for (int i = 0; i < paths.Length; i++)
            {
                PIDL pidl = ILCreateFromPath(paths[i]);
                pidls[i] = pidl.GetBytes();
            }

            int pidlOffset = 4 * (paths.Length + 2);

            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(paths.Length);
            binaryWriter.Write(pidlOffset);
            pidlOffset += 4;

            foreach (byte[] pidl in pidls)
            {
                binaryWriter.Write(pidlOffset);
                pidlOffset += pidl.Length;
            }

            binaryWriter.Write(0);
            foreach (byte[] pidl in pidls)
                binaryWriter.Write(pidl);

            return memoryStream;
        }

        private static readonly Guid ImageListId = typeof(IImageList).GUID;

        private static readonly int FileInfoSize = Marshal.SizeOf(typeof(SHFILEINFO));
    }

    public static class RootFolers
    {
        public const string QuickAccess = "shell:::{679F85CB-0220-4080-B29B-5540CC05AAB6}";

        public const string Computer = "shell:::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";

        public const string Network = "shell:::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}";
    }
}
