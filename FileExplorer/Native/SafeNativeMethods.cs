using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;

namespace FileExplorer.Native
{
    [SuppressUnmanagedCodeSecurity]
    public static class SafeNativeMethods
    {
        public static IntPtr GetActiveWindowHandle()
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

            IShellItem shellItem;
            SHCreateItemFromParsingName(path, IntPtr.Zero, typeof(IShellItem).GUID, out shellItem);

            return shellItem;
        }

        public static int MultiFileProperties(IDataObject data)
        {
            return SHMultiFileProperties(data, 0);
        }

        public static MemoryStream CreateShellIdList(string[] paths)
        {
            int pos = 0;
            byte[][] pidls = new byte[paths.Length][];
            foreach (string path in paths)
            {
                IntPtr pidl = ILCreateFromPath(path);
                int pidlSize = ILGetSize(pidl);

                pidls[pos] = new byte[pidlSize];
                Marshal.Copy(pidl, pidls[pos++], 0, pidlSize);
                ILFree(pidl);
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

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterDeviceNotification(IntPtr hHandle);

        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr CreateFile(string FileName, uint DesiredAccess, uint ShareMode,
            uint SecurityAttributes, uint CreationDisposition, uint FlagsAndAttributes, int hTemplateFile);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern bool PathFileExists(string path);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SHMultiFileProperties(IDataObject pdtobj, int flags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ILCreateFromPath(string path);

        [DllImport("shell32.dll", CharSet = CharSet.None)]
        private static extern void ILFree(IntPtr pidl);

        [DllImport("shell32.dll", CharSet = CharSet.None)]
        private static extern int ILGetSize(IntPtr pidl);
    }
}
