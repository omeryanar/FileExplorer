using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Shell32;

namespace FileExplorer.Helpers
{
    public class ContextMenuHelper
    {
        public static void ShowNativeContextMenu(IList<string> filePaths, Point point)
        {
            List<ShellItem> shellItems = filePaths.Select(x => new ShellItem(x)).ToList();
            if (shellItems.Count > 1 && shellItems.Contains(ShellFolder.Desktop))
                throw new Exception("If the desktop folder is specified, it must be the only item.");

            IntPtr[] pidls = new IntPtr[shellItems.Count];
            ShellFolder parent = null;

            for (int i = 0; i < shellItems.Count; ++i)
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
