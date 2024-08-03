using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FileExplorer.Native;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static FileExplorer.Native.ShellAPI;

namespace FileExplorer.Helpers
{
    public class ContextMenuHelper
    {
        private BasicMessageWindow basicMessageWindow;

        private IShellFolder desktopFolder;

        private IContextMenu iContextMenu;
        private IContextMenu2 iContextMenu2;
        private IContextMenu3 iContextMenu3;

        public ContextMenuHelper()
        {
            basicMessageWindow = new BasicMessageWindow(WindowMessageFilter);

            if (ShellAPI.SHGetDesktopFolder(out IntPtr desktopFolderPtr) == ShellAPI.S_OK)
                desktopFolder = (IShellFolder)Marshal.GetTypedObjectForIUnknown(desktopFolderPtr, typeof(IShellFolder));
        }

        public void ShowNativeContextMenu(IList<string> filePaths, Point point)
        {
            if (desktopFolder == null)
                return;

            ShellItem[] shellItems = new ShellItem[filePaths.Count];
            for (int i = 0; i < filePaths.Count; i++)
                shellItems[i] = new ShellItem(filePaths[i]);

            IShellFolder shellFolder = null;
            ShellFolder parent = shellItems.FirstOrDefault().Parent;

            IntPtr shellFolderPIDL = (IntPtr)parent.PIDL;
            if (desktopFolder.BindToObject(shellFolderPIDL, IntPtr.Zero, ref ShellAPI.IID_IShellFolder, out IntPtr ptrShellFolder) == ShellAPI.S_OK)
                shellFolder = (IShellFolder)Marshal.GetTypedObjectForIUnknown(ptrShellFolder, typeof(IShellFolder));
            else
                return;

            IntPtr[] pidls = new IntPtr[shellItems.Length];
            for (int i = 0; i < pidls.Length; i++)
                pidls[i] = (IntPtr)shellItems[i].PIDL.LastId;

            if (shellFolder.GetUIObjectOf(IntPtr.Zero, (uint)pidls.Length, pidls, ref ShellAPI.IID_IContextMenu, IntPtr.Zero, out IntPtr iContextMenuPtr) == ShellAPI.S_OK)
                iContextMenu = (IContextMenu)Marshal.GetTypedObjectForIUnknown(iContextMenuPtr, typeof(IContextMenu));
            else
                return;

            if (Marshal.QueryInterface(iContextMenuPtr, ref ShellAPI.IID_IContextMenu2, out IntPtr iContextMenu2Ptr) == ShellAPI.S_OK)
                iContextMenu2 = (IContextMenu2)Marshal.GetTypedObjectForIUnknown(iContextMenu2Ptr, typeof(IContextMenu2));
            else
                return;

            if (Marshal.QueryInterface(iContextMenuPtr, ref ShellAPI.IID_IContextMenu3, out IntPtr iContextMenu3Ptr) == ShellAPI.S_OK)
                iContextMenu3 = (IContextMenu3)Marshal.GetTypedObjectForIUnknown(iContextMenu3Ptr, typeof(IContextMenu3));
            else
                return;

            IntPtr contextMenu = ShellAPI.CreatePopupMenu();

            iContextMenu.QueryContextMenu(contextMenu, 0,
                  ShellAPI.CMD_FIRST,
                  ShellAPI.CMD_LAST,
                  CMF.EXPLORE |
                  ((Control.ModifierKeys & Keys.Shift) != 0 ?
                    CMF.EXTENDEDVERBS : 0));

            uint selected = TrackPopupMenuEx(
                                contextMenu,
                                TPM.RETURNCMD,
                                point.X,
                                point.Y,
                                (IntPtr)basicMessageWindow.Handle,
                                IntPtr.Zero);

            if (selected > 0)
                InvokeCommand(selected - ShellAPI.CMD_FIRST, parent.ParsingName, point);
        }

        private bool WindowMessageFilter(HWND hwnd, uint msg, IntPtr wParam, IntPtr lParam, out IntPtr lReturn)
        {
            lReturn = default;

            #region IContextMenu2

            if (iContextMenu2 != null &&
                (msg == (int)WM.INITMENUPOPUP ||
                 msg == (int)WM.MEASUREITEM ||
                 msg == (int)WM.DRAWITEM))
            {
                if (iContextMenu2.HandleMenuMsg((uint)msg, wParam, lParam) == HRESULT.S_OK)
                    return true;
            }

            #endregion

            #region IContextMenu3

            if (iContextMenu3 != null &&
                msg == (int)WM.MENUCHAR)
            {
                if (iContextMenu3.HandleMenuMsg2((uint)msg, wParam, lParam, IntPtr.Zero) == HRESULT.S_OK)
                    return true;
            }

            #endregion

            return false;

        }

        private void InvokeCommand(uint cmd, string parentDir, Point ptInvoke)
        {
            CMINVOKECOMMANDINFOEX invoke = new CMINVOKECOMMANDINFOEX();
            invoke.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));
            invoke.lpVerb = (IntPtr)cmd;
            invoke.lpDirectory = parentDir;
            invoke.lpVerbW = (IntPtr)cmd;
            invoke.lpDirectoryW = parentDir;
            invoke.fMask = CMIC.UNICODE | CMIC.PTINVOKE;
            invoke.ptInvoke = new ShellAPI.POINT(ptInvoke.X, ptInvoke.Y);
            invoke.nShow = SW.NORMAL;

            iContextMenu.InvokeCommand(ref invoke);
        }
    }
}
