using System;
using System.Collections.Generic;
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

        public static int NaturalCompare(string value1, string value2)
        {
            return StrCmpLogicalW(value1, value2);
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
                result = SHGetFileInfo(path, FileAttributes.Normal, ref shFileInfo, Marshal.SizeOf(shFileInfo), SHGFI.SHGFI_SYSICONINDEX | SHGFI.SHGFI_USEFILEATTRIBUTES);
            }
            else
            {
                PIDL pidl = ILCreateFromPath(path);
                result = SHGetFileInfo(pidl, FileAttributes.Normal, ref shFileInfo, Marshal.SizeOf(shFileInfo), SHGFI.SHGFI_SYSICONINDEX | SHGFI.SHGFI_PIDL);
            }

            if (result == IntPtr.Zero)
                return null;

            SHGetImageList(size, ImageListId, out IImageList iImageList);
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

    }

    public static class KnownFolders
    {
        public const string QuickAccess = "shell:::{679F85CB-0220-4080-B29B-5540CC05AAB6}";

        public const string Computer = "shell:::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";

        public const string Network = "shell:::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}";

        public static string GetPath(KnownFolder knownFolder)
        {
            Guid guid = FolderIdentifiers[knownFolder];
            SHGetKnownFolderPath(guid, KnownFolderGetFlags, HTOKEN.NULL, out string path);
            return path;
        }

        private static Dictionary<KnownFolder, Guid> FolderIdentifiers = new Dictionary<KnownFolder, Guid>
        {
            { KnownFolder.Desktop, new Guid("{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}") },
            { KnownFolder.Documents, new Guid("{FDD39AD0-238F-46AF-ADB4-6C85480369C7}") },
            { KnownFolder.Downloads, new Guid("{374DE290-123F-4565-9164-39C4925E467B}") },
            { KnownFolder.Music, new Guid("{4BD8D571-6D19-48D3-BE97-422220080E43}") },
            { KnownFolder.Pictures, new Guid("{33E28130-4E1E-4676-835A-98395C3BC3BB}") },
            { KnownFolder.Videos, new Guid("{18989B1D-99B5-455B-841C-AB7C74E4DDFC}") }
        };

        private const KNOWN_FOLDER_FLAG KnownFolderGetFlags =
            KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT_PATH | KNOWN_FOLDER_FLAG.KF_FLAG_NOT_PARENT_RELATIVE |
            KNOWN_FOLDER_FLAG.KF_FLAG_NO_ALIAS | KNOWN_FOLDER_FLAG.KF_FLAG_DONT_VERIFY;
    }

    public enum KnownFolder
    {
        Desktop,
        Documents,
        Downloads,
        Music,
        Pictures,
        Videos
    }
}
