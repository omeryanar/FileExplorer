using System;
using System.IO;
using System.Runtime.InteropServices;
using static Vanara.PInvoke.Shell32;

namespace FileExplorer.Native
{
    public class FileOperation : IDisposable
    {
        private bool disposed;
        private IFileOperation fileOperation;

        public FileOperation()
        {
            fileOperation = (IFileOperation)Activator.CreateInstance(typeof(CFileOperations));
            fileOperation.SetOperationFlags(FILEOP_FLAGS.FOF_NOCONFIRMMKDIR);
            fileOperation.SetOwnerWindow(SafeNativeMethods.GetActiveWindowHandle());
        }

        public static string GetParsingName(string source)
        {
            IShellItem sourceItem = SafeNativeMethods.CreateShellItem(source);

            return sourceItem?.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING);
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
            fileOperation.SetOperationFlags(FILEOP_FLAGS.FOF_ALLOWUNDO);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Marshal.FinalReleaseComObject(fileOperation);
            }
        }
    }
}
