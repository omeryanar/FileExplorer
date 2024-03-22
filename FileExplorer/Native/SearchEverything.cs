using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using Alphaleonis.Win32.Filesystem;

namespace FileExplorer.Native
{
    public class SearchEverything : IDisposable
    {
        public static bool IsAvailable { get; private set; }

        public const string ExecutableName = "Everything";

        static SearchEverything()
        {
            ServiceController[] services = ServiceController.GetServices();
            if (services.Any(x => x.ServiceName == ExecutableName))
            {
                IsAvailable = true;
                return;
            }

            Process[] processes = Process.GetProcessesByName(ExecutableName);
            IsAvailable = processes.Any();
        }

        public FileSystemInfo[] Search(string query)
        {
            EverythingNative.Everything_SetSearch(query);
            EverythingNative.Everything_Query(true);

            StringBuilder filePath = new StringBuilder(260);
            uint numResults = EverythingNative.Everything_GetNumResults();

            FileSystemInfo[] results = new FileSystemInfo[numResults];

            for (uint index = 0U; index < numResults; index = index + 1U)
            {
                filePath.Clear();
                EverythingNative.Everything_GetResultFullPathName(index, filePath, 260U);

                if (EverythingNative.Everything_IsFileResult(index))
                    results[index] = new FileInfo(filePath.ToString());
                else if (EverythingNative.Everything_IsFolderResult(index))
                    results[index] = new DirectoryInfo(filePath.ToString());
            }

            return results;
        }

        public void Dispose()
        {
            EverythingNative.Everything_Reset();
        }

        private class EverythingNative
        {
            public static int Everything_SetSearch(string lpSearchString) => Environment.Is64BitProcess ?
                EverythingNative64.Everything_SetSearch(lpSearchString) : EverythingNative32.Everything_SetSearch(lpSearchString);

            public static bool Everything_Query(bool bWait) => Environment.Is64BitProcess ?
                EverythingNative64.Everything_Query(bWait) : EverythingNative32.Everything_Query(bWait);

            public static uint Everything_GetNumResults() => Environment.Is64BitProcess ?
                EverythingNative64.Everything_GetNumResults() : EverythingNative32.Everything_GetNumResults();

            public static bool Everything_IsFileResult(uint nIndex) => Environment.Is64BitProcess ?
                EverythingNative64.Everything_IsFileResult(nIndex) : EverythingNative32.Everything_IsFileResult(nIndex);

            public static bool Everything_IsFolderResult(uint nIndex) => Environment.Is64BitProcess ?
                EverythingNative64.Everything_IsFolderResult(nIndex) : EverythingNative32.Everything_IsFolderResult(nIndex);

            public static void Everything_GetResultFullPathName(uint nIndex, StringBuilder lpString, uint nMaxCount)
            {
                if (Environment.Is64BitProcess)
                    EverythingNative64.Everything_GetResultFullPathName(nIndex, lpString, nMaxCount);
                else
                    EverythingNative32.Everything_GetResultFullPathName(nIndex, lpString, nMaxCount);
            }

            public static void Everything_Reset()
            {
                if (Environment.Is64BitProcess)
                    EverythingNative64.Everything_Reset();
                else
                    EverythingNative32.Everything_Reset();
            }
        }

        private static class EverythingNative32
        {
            [DllImport("Everything32.dll", CharSet = CharSet.Unicode)]
            public static extern int Everything_SetSearch(string lpSearchString);

            [DllImport("Everything32.dll", CharSet = CharSet.Unicode)]
            public static extern bool Everything_Query(bool bWait);

            [DllImport("Everything32.dll")]
            public static extern uint Everything_GetNumResults();

            [DllImport("Everything32.dll")]
            public static extern bool Everything_IsFileResult(uint nIndex);

            [DllImport("Everything32.dll")]
            public static extern bool Everything_IsFolderResult(uint nIndex);

            [DllImport("Everything32.dll", CharSet = CharSet.Unicode)]
            public static extern void Everything_GetResultFullPathName(uint nIndex, StringBuilder lpString, uint nMaxCount);

            [DllImport("Everything32.dll")]
            public static extern void Everything_Reset();
        }

        private static class EverythingNative64
        {
            [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
            public static extern int Everything_SetSearch(string lpSearchString);

            [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
            public static extern bool Everything_Query(bool bWait);

            [DllImport("Everything64.dll")]
            public static extern uint Everything_GetNumResults();

            [DllImport("Everything64.dll")]
            public static extern bool Everything_IsFileResult(uint nIndex);

            [DllImport("Everything64.dll")]
            public static extern bool Everything_IsFolderResult(uint nIndex);

            [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
            public static extern void Everything_GetResultFullPathName(uint nIndex, StringBuilder lpString, uint nMaxCount);            

            [DllImport("Everything64.dll")]
            public static extern void Everything_Reset();
        }
    }
}
