using System;
using System.Collections.Generic;
using System.Windows;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;

namespace FileExplorer.Helpers
{
    public class ContextMenuHelper
    {
        private const int CmdFirst = 0x8000;

        private BasicMessageWindow messageWindow;

        private IContextMenu iContextMenu;
        private IContextMenu2 iContextMenu2;
        private IContextMenu3 iContextMenu3;

        public ContextMenuHelper()
        {
            messageWindow = new BasicMessageWindow(WindowMessageFilter);
        }

        public void ShowNativeContextMenu(IList<string> filePaths, Point point)
        {
            ShellItem[] shellItems = new ShellItem[filePaths.Count];
            for (int i = 0; i < filePaths.Count; i++)
                shellItems[i] = new ShellItem(filePaths[i]);

            if (filePaths.Count > 1 && Array.IndexOf(shellItems, ShellFolder.Desktop) != -1)
                throw new Exception("If the desktop folder is specified, it must be the only item.");

            IntPtr[] pidls = new IntPtr[shellItems.Length];
            ShellFolder parent = null;

            for (int i = 0; i < shellItems.Length; ++i)
            {
                if (i == 0)
                    parent = shellItems[i].Parent;
                else if (shellItems[i].Parent != parent)
                    throw new Exception("All shell shellItems must have the same parent");
                
                pidls[i] = (IntPtr)shellItems[i].PIDL.LastId;
            }

            iContextMenu = parent!.IShellFolder.GetUIObjectOf<IContextMenu>(HWND.NULL, pidls);
            iContextMenu2 = iContextMenu as IContextMenu2;
            iContextMenu3 = iContextMenu as IContextMenu3;

            using (SafeHMENU menu = CreatePopupMenu())
            {
                iContextMenu.QueryContextMenu(menu, 0, CmdFirst, Int32.MaxValue, CMF.CMF_EXTENDEDVERBS).ThrowIfFailed();
                
                uint command = TrackPopupMenuEx(menu, TrackPopupMenuFlags.TPM_RETURNCMD, (int)point.X, (int)point.Y, messageWindow.Handle);
                if (command > 0)
                    InvokeCommand((int)command - CmdFirst);
            }
        }

        public void InvokeCommand(ResourceId verb, ShowWindowCommand show = ShowWindowCommand.SW_SHOWNORMAL, HWND parent = default,
            POINT? location = default, bool allowAsync = false, bool shiftDown = false, bool ctrlDown = false, uint hotkey = 0,
            bool logUsage = false, bool noZoneChecks = false, string parameters = null)
        {
            CMINVOKECOMMANDINFOEX invoke = new(verb, show, parent, location, allowAsync, shiftDown, ctrlDown, hotkey, logUsage, noZoneChecks, parameters);
            iContextMenu.InvokeCommand(invoke).ThrowIfFailed();
        }

        private bool WindowMessageFilter(HWND hwnd, uint msg, IntPtr wParam, IntPtr lParam, out IntPtr lReturn)
        {
            lReturn = default;
            try
            {
                switch (msg)
                {
                    case (uint)WindowMessage.WM_COMMAND:
                        if ((int)wParam >= CmdFirst)
                        {
                            InvokeCommand((int)wParam - CmdFirst);
                            return true;
                        }
                        break;

                    case (uint)WindowMessage.WM_MENUCHAR:
                        if (iContextMenu3 is not null)
                        {
                            if (iContextMenu3.HandleMenuMsg2(msg, wParam, lParam, out var lRet).Succeeded)
                                lReturn = lRet;

                            return true;
                        }
                        break;

                    case (uint)WindowMessage.WM_INITMENUPOPUP:
                    case (uint)WindowMessage.WM_MEASUREITEM:
                    case (uint)WindowMessage.WM_DRAWITEM:
                        if (iContextMenu2 is not null)
                        {
                            if (iContextMenu2.HandleMenuMsg(msg, wParam, lParam).Succeeded)
                                lReturn = default;

                            return true;
                        }
                        break;
                }
            }
            catch { }
            
            return false;
        }
    }
}
