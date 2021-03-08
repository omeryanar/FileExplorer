using System;
using System.Runtime.InteropServices;

namespace FileExplorer.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DEV_BROADCAST_VOLUME
    {
        public int DBVC_Size;
        public int DBVC_DeviceType;
        public int DBVC_Reserved;
        public int DBVC_UnitMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEV_BROADCAST_HANDLE
    {
        public int DBCH_Size;
        public int DBCH_DeviceType;
        public int DBCH_Reserved;
        public IntPtr DBCH_Handle;
        public IntPtr DBCH_HDevNotify;
        public Guid DBCH_EventGuid;
        public long DBCH_NameOffset;
        public byte DBCH_Data;
    }
}
