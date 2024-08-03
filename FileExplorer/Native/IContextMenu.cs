using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FileExplorer.Native
{
    [ComImport()]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214e4-0000-0000-c000-000000000046")]
    public interface IContextMenu
    {
        // Adds commands to a shortcut menu
        [PreserveSig()]
        Int32 QueryContextMenu(
            IntPtr hmenu,
            uint iMenu, 
            uint idCmdFirst, 
            uint idCmdLast, 
            ShellAPI.CMF uFlags);

        // Carries out the command associated with a shortcut menu item
        [PreserveSig()]
        Int32 InvokeCommand(
            ref ShellAPI.CMINVOKECOMMANDINFOEX info);

        // Retrieves information about a shortcut menu command, 
        // including the help string and the language-independent, 
        // or canonical, name for the command
        [PreserveSig()]
        Int32 GetCommandString(
            uint idcmd,
            ShellAPI.GCS uflags, 
            uint reserved,
            [MarshalAs(UnmanagedType.LPArray)]
            byte[] commandstring,
            int cch);
    }
    
    [ComImport, Guid("000214f4-0000-0000-c000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IContextMenu2
    {
        // Adds commands to a shortcut menu
        [PreserveSig()]
        Int32 QueryContextMenu(
            IntPtr hmenu,
            uint iMenu,
            uint idCmdFirst,
            uint idCmdLast,
            ShellAPI.CMF uFlags);

        // Carries out the command associated with a shortcut menu item
        [PreserveSig()]
        Int32 InvokeCommand(
            ref ShellAPI.CMINVOKECOMMANDINFOEX info);

        // Retrieves information about a shortcut menu command, 
        // including the help string and the language-independent, 
        // or canonical, name for the command
        [PreserveSig()]
        Int32 GetCommandString(
            uint idcmd,
            ShellAPI.GCS uflags,
            uint reserved,
            [MarshalAs(UnmanagedType.LPWStr)]
            StringBuilder commandstring,
            int cch);

        // Allows client objects of the IContextMenu interface to 
        // handle messages associated with owner-drawn menu items
        [PreserveSig]
        Int32 HandleMenuMsg(
            uint uMsg, 
            IntPtr wParam, 
            IntPtr lParam);
    }

    [ComImport, Guid("bcfce0a0-ec17-11d0-8d10-00a0c90f2719")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IContextMenu3
    {
        // Adds commands to a shortcut menu
        [PreserveSig()]
        Int32 QueryContextMenu(
            IntPtr hmenu,
            uint iMenu,
            uint idCmdFirst,
            uint idCmdLast,
            ShellAPI.CMF uFlags);

        // Carries out the command associated with a shortcut menu item
        [PreserveSig()]
        Int32 InvokeCommand(
            ref ShellAPI.CMINVOKECOMMANDINFOEX info);

        // Retrieves information about a shortcut menu command, 
        // including the help string and the language-independent, 
        // or canonical, name for the command
        [PreserveSig()]
        Int32 GetCommandString(
            uint idcmd,
            ShellAPI.GCS uflags,
            uint reserved,
            [MarshalAs(UnmanagedType.LPWStr)]
            StringBuilder commandstring,
            int cch);

        // Allows client objects of the IContextMenu interface to 
        // handle messages associated with owner-drawn menu items
        [PreserveSig]
        Int32 HandleMenuMsg(
            uint uMsg, 
            IntPtr wParam, 
            IntPtr lParam);

        // Allows client objects of the IContextMenu3 interface to 
        // handle messages associated with owner-drawn menu items
        [PreserveSig]
        Int32 HandleMenuMsg2(
            uint uMsg, 
            IntPtr wParam, 
            IntPtr lParam, 
            IntPtr plResult);
    }
}
