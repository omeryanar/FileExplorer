using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FileExplorer.Native
{
    public class FileOperation : IDisposable
    {
        private bool disposed;
        private IFileOperation fileOperation;

        public FileOperation()
        {
            fileOperation = (IFileOperation)Activator.CreateInstance(fileOperationType);
            fileOperation.SetOperationFlags(FileOperationFlags.FOF_NOCONFIRMMKDIR);
            fileOperation.SetOwnerWindow(SafeNativeMethods.GetActiveWindowHandle());
        }        

        public static string GetParsingName(string source)
        {
            IShellItem sourceItem = SafeNativeMethods.CreateShellItem(source);
                
            return sourceItem?.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING);
        }

        public static IntPtr GetIcon(string source, int width, int height)
        {
            IShellItem sourceItem = SafeNativeMethods.CreateShellItem(source);
            SIIGBF options = SIIGBF.IconOnly;
            Size size = new Size { Width = width, Height = height };
            HResult hr = ((IShellItemImageFactory)sourceItem).GetImage(size, options, out IntPtr hBitmap);

            if (hr == HResult.Ok)
                return hBitmap;

            return IntPtr.Zero;
        }

        public void CopyItem(string source, string destination, string newName)
        {
            IShellItem sourceItem = SafeNativeMethods.CreateShellItem(source);
            IShellItem destinationItem = SafeNativeMethods.CreateShellItem(destination);

            fileOperation.CopyItem(sourceItem, destinationItem, newName, null);
        }

        public void MoveItem(string source, string destination, string newName)
        {
            IShellItem sourceItem = SafeNativeMethods.CreateShellItem(source);
            IShellItem destinationItem = SafeNativeMethods.CreateShellItem(destination);

            fileOperation.MoveItem(sourceItem, destinationItem, newName, null);
        }

        public void RenameItem(string source, string newName)
        {
            IShellItem sourceItem = SafeNativeMethods.CreateShellItem(source);

            fileOperation.RenameItem(sourceItem, newName, null);
        }

        public void DeleteItem(string source)
        {
            IShellItem sourceItem = SafeNativeMethods.CreateShellItem(source);

            fileOperation.DeleteItem(sourceItem, null);
        }

        public void NewItem(string folderName, string name, FileAttributes attrs)
        {
            IShellItem folderItem = SafeNativeMethods.CreateShellItem(folderName);

            fileOperation.NewItem(folderItem, attrs, name, string.Empty, null);
        }

        public void PerformOperations()
        {
            try
            {
                fileOperation.PerformOperations();
            }
            catch (COMException) { }
        }

        public void AllowUndo()
        {
            fileOperation.SetOperationFlags(FileOperationFlags.FOF_ALLOWUNDO);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Marshal.FinalReleaseComObject(fileOperation);
            }
        }

        private static readonly Type fileOperationType = Type.GetTypeFromCLSID(new Guid(Constants.FileOperationGuid));
    }
}
