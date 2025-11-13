using System;
using System.Collections.Generic;
using System.Windows;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Shell32;

namespace FileExplorer.Helpers
{
    public class ContextMenuHelper
    {
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

            IContextMenu iContextMenu = parent!.IShellFolder.GetUIObjectOf<IContextMenu>(HWND.NULL, pidls);
            ShellContextMenu shellContextMenu = new ShellContextMenu(iContextMenu);
            shellContextMenu.ShowContextMenu(new POINT((int)point.X, (int)point.Y), onMenuItemClicked: (m, i, w) =>
            {
                shellContextMenu.InvokeCommand(i, w);
            });
        }
    }
}
