using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FileExplorer.Native
{
    public class DeviceWatcher
    {
        public delegate void DeviceNotificationEventHandler(object sender, DeviceNotificationEventArgs e);
        public event DeviceNotificationEventHandler DeviceArrived;
        public event DeviceNotificationEventHandler DeviceQueryRemove;
        public event DeviceNotificationEventHandler DeviceRemoveComplete;

        public DeviceWatcher()
        {
            SpongeWindow = new SpongeWindow();
            SpongeWindow.WndProcCalled += (s, e) => ProcessMessage(e);

            DeviceHandles = new Dictionary<IntPtr, string>();
        }

        private void ProcessMessage(Message message)
        {
            if (message.Msg == WM_DEVICECHANGE)
            {
                switch (message.WParam.ToInt32())
                {
                    case DBT_DEVICEARRIVAL:
                        if (Marshal.ReadInt32(message.LParam, 4) == DBT_DEVTYP_VOLUME)
                        {
                            DEV_BROADCAST_VOLUME volume = Marshal.PtrToStructure<DEV_BROADCAST_VOLUME>(message.LParam);
                            string deviceName = DeviceMaskToDeviceName(volume.DBVC_UnitMask);

                            DeviceArrived?.Invoke(this, new DeviceNotificationEventArgs(deviceName));
                            RegisterDeviceNotification(deviceName);
                        }
                        break;

                    case DBT_DEVICEQUERYREMOVE:
                        if (Marshal.ReadInt32(message.LParam, 4) == DBT_DEVTYP_HANDLE)
                        {
                            DEV_BROADCAST_HANDLE data = Marshal.PtrToStructure<DEV_BROADCAST_HANDLE>(message.LParam);
                            if (DeviceHandles.ContainsKey(data.DBCH_HDevNotify))
                                DeviceQueryRemove?.Invoke(this, new DeviceNotificationEventArgs(DeviceHandles[data.DBCH_HDevNotify]));

                            UnregisterDeviceNotification(data.DBCH_HDevNotify, data.DBCH_Handle);
                        }
                        break;

                    case DBT_DEVICEREMOVECOMPLETE:
                        if (Marshal.ReadInt32(message.LParam, 4) == DBT_DEVTYP_VOLUME)
                        {
                            DEV_BROADCAST_VOLUME volume = Marshal.PtrToStructure<DEV_BROADCAST_VOLUME>(message.LParam);
                            string deviceName = DeviceMaskToDeviceName(volume.DBVC_UnitMask);

                            DeviceRemoveComplete?.Invoke(this, new DeviceNotificationEventArgs(deviceName));
                        }
                        break;
                }
            }
        }

        public void RegisterDeviceNotification(string deviceName)
        {
            IntPtr fileHandle = CreateFile(deviceName, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE,
                                    0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS | FILE_ATTRIBUTE_NORMAL, 0);

            if (fileHandle != INVALID_HANDLE_VALUE)
            {
                DEV_BROADCAST_HANDLE data = new DEV_BROADCAST_HANDLE
                {
                    DBCH_DeviceType = DBT_DEVTYP_HANDLE,
                    DBCH_Reserved = 0,
                    DBCH_NameOffset = 0,
                    DBCH_Handle = fileHandle,
                    DBCH_HDevNotify = IntPtr.Zero
                };

                int size = Marshal.SizeOf(data);
                data.DBCH_Size = size;
                IntPtr buffer = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(data, buffer, true);

                IntPtr deviceHandle = RegisterDeviceNotification(SpongeWindow.Handle, buffer, 0);
                if (deviceHandle != IntPtr.Zero)
                    DeviceHandles.Add(deviceHandle, deviceName);
            }
        }

        private void UnregisterDeviceNotification(IntPtr deviceHandle, IntPtr fileHandle)
        {
            if (DeviceHandles.ContainsKey(deviceHandle))
            {
                CloseHandle(fileHandle);
                UnregisterDeviceNotification(deviceHandle);
            }
        }

        private static string DeviceMaskToDeviceName(int mask)
        {
            int log = Convert.ToInt32(Math.Log(mask, 2));
            if (log < DeviceLetters.Length)
                return String.Format("{0}:\\", DeviceLetters[log]);
            else
                return String.Empty;
        }

        private const string DeviceLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private const int WM_DEVICECHANGE = 0x0219;

        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEQUERYREMOVE = 0x8001;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

        private const int DBT_DEVTYP_VOLUME = 0x0002;
        private const int DBT_DEVTYP_HANDLE = 0x0006;

        private const uint GENERIC_READ = 0x80000000;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint FILE_ATTRIBUTE_NORMAL = 128;
        private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private readonly SpongeWindow SpongeWindow;
        private readonly Dictionary<IntPtr, string> DeviceHandles;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterDeviceNotification(IntPtr hHandle);

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr CreateFile(string FileName, uint DesiredAccess, uint ShareMode,
            uint SecurityAttributes, uint CreationDisposition, uint FlagsAndAttributes, int hTemplateFile);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }

    public sealed class SpongeWindow : NativeWindow
    {
        public event EventHandler<Message> WndProcCalled;

        public SpongeWindow()
        {
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            WndProcCalled?.Invoke(this, m);
            base.WndProc(ref m);
        }
    }

    public class DeviceNotificationEventArgs : EventArgs
    {
        public DeviceNotificationEventArgs(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

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
