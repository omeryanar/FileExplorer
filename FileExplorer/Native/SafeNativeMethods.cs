using System.IO;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
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

        public static string[] GetKnownFolderPaths()
        {
            string[] paths = 
            [
                ShellUtil.GetPathForKnownFolder(Shell32.KNOWNFOLDERID.FOLDERID_Desktop.Guid()),
                ShellUtil.GetPathForKnownFolder(Shell32.KNOWNFOLDERID.FOLDERID_Documents.Guid()),
                ShellUtil.GetPathForKnownFolder(Shell32.KNOWNFOLDERID.FOLDERID_Downloads.Guid()),
                ShellUtil.GetPathForKnownFolder(Shell32.KNOWNFOLDERID.FOLDERID_Music.Guid()),
                ShellUtil.GetPathForKnownFolder(Shell32.KNOWNFOLDERID.FOLDERID_Pictures.Guid()),
                ShellUtil.GetPathForKnownFolder(Shell32.KNOWNFOLDERID.FOLDERID_Videos.Guid())
            ];

            return paths;
        }

        public static void MultiFileProperties(IDataObject data)
        {
            SHMultiFileProperties(data, 0);
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
    }
}
