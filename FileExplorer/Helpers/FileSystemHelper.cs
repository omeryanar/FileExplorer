using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Alphaleonis.Win32.Network;
using FileExplorer.Core;
using FileExplorer.Model;
using FileExplorer.Native;
using FileExplorer.Properties;

namespace FileExplorer.Helpers
{
    public class FileSystemHelper
    {
        public const string Separator = "\\";

        public const string ComputerPath = "Computer";

        public const string QuickAccessPath = "Quick Access";

        public const string NetworkPath = "Network";

        public static FileModel Computer
        {
            get
            {
                if (computer == null)
                {
                    computer = FileModel.FromPath(ComputerPath);
                    computer.Folders = GetDrives();
                }

                return computer;
            }
        }
        private static FileModel computer;

        public static FileModel QuickAccess
        {
            get
            {
                if (quickAccess == null)
                {
                    quickAccess = FileModel.FromPath(QuickAccessPath);
                    quickAccess.Folders = GetQuickAccess();
                }

                return quickAccess;
            }
        }
        private static FileModel quickAccess;

        public static FileModel Network
        {
            get
            {
                if (network == null)
                    network = FileModel.FromPath(NetworkPath);

                return network;
            }
        }
        private static FileModel network;

        public static string[] UserFolders
        {
            get
            {
                if (userFolders == null)
                {
                    userFolders = new string[]
                    {
                        KnownFolders.GetPath(KnownFolder.Desktop),
                        KnownFolders.GetPath(KnownFolder.Documents),
                        KnownFolders.GetPath(KnownFolder.Downloads),
                        KnownFolders.GetPath(KnownFolder.Music),
                        KnownFolders.GetPath(KnownFolder.Pictures),
                        KnownFolders.GetPath(KnownFolder.Videos)
                    };
                }

                return userFolders;
            }
        }
        private static string[] userFolders;

        public static bool IsDrive(string path)
        {
            return Regex.IsMatch(path, @"^[A-Za-z]:\\?$");
        }

        public static bool IsNetworkHost(string path)
        {
            return path.StartsWith(Path.UncPrefix) && path.Split(Separator).Length == 1;
        }

        public static bool IsNetworkShare(string path)
        {
            return path.StartsWith(Path.UncPrefix) && path.Split(Separator).Length == 2;
        }

        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public static bool NetworkHostAccessible(string path)
        {
            if (!IsNetworkHost(path))
                return false;

            foreach (ShareInfo shareInfo in Host.EnumerateShares(path, true))
                return true;

            return false;
        }

        public static string GetHostName(string path)
        {
            return path.Split(Separator).First();
        }

        public static string GetFileParsingName(string path)
        {
            try
            {
                if (IsNetworkHost(path))
                    return path;

                return FileOperation.GetParsingName(path);
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public static string GetParentFolderPath(string path)
        {
            try
            {
                if (IsDrive(path))
                    return Computer.FullPath;

                if (IsNetworkHost(path))
                    return Network.FullPath;

                if (IsNetworkShare(path))
                    return Path.UncPrefix + GetHostName(path);

                return Path.GetDirectoryName(path);
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public static async Task<string[]> GetFolderPaths(string path)
        {
            try
            {
                return await Task.Run(() => Directory.GetDirectories(path));
            }
            catch (Exception)
            {
                return new string[0];
            }
        }

        public static string[] GetFolderPaths(string path, bool includeHidden, bool includeSystem)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                DirectoryInfo[] directories = directoryInfo.GetDirectories();

                if (includeHidden && includeSystem)
                    return directories.Select(x => x.FullName).ToArray();
                if (includeHidden && !includeSystem)
                    return directories.Where(x => x.EntryInfo.IsSystem == false).Select(x => x.FullName).ToArray();
                if (!includeHidden && includeSystem)
                    return directories.Where(x => x.EntryInfo.IsHidden == false).Select(x => x.FullName).ToArray();
                else
                    return directories.Where(x => x.EntryInfo.IsHidden == false && x.EntryInfo.IsSystem == false).Select(x => x.FullName).ToArray();
            }
            catch (Exception)
            {
                return new string[0];
            }
        }

        public static async Task<FileModelCollection> GetFiles(FileModel fileModel)
        {
            List<FileModel> fileModelList = new List<FileModel>();
            FileInfo[] files = await GetFiles(fileModel.FullPath);
            if (files != null)
            {
                foreach (FileInfo file in files)
                {
                    FileModel childFileModel = FileModel.FromFileInfo(file);
                    childFileModel.Parent = fileModel;

                    fileModelList.Add(childFileModel);
                }
            }

            return new FileModelCollection(fileModelList);
        }

        public static async Task<FileModelCollection> GetFolders(FileModel fileModel)
        {
            if (fileModel == Computer)
                return GetDrives();
            if (fileModel == QuickAccess)
                return GetQuickAccess();
            if (fileModel == Network)
                return Network.Folders;
            if (fileModel.Parent == Network)
                return await GetNetworkShares(fileModel);

            List<FileModel> fileModelList = new List<FileModel>();
            DirectoryInfo[] folders = await GetFolders(fileModel.FullPath);
            if (folders != null)
            {
                foreach (DirectoryInfo folder in folders)
                {
                    FileModel childFileModel = FileModel.FromDirectoryInfo(folder);
                    childFileModel.Parent = fileModel;

                    fileModelList.Add(childFileModel);
                }
            }

            return new FileModelCollection(fileModelList);
        }

        public static async Task<FileModelCollection> GetNetworkShares(FileModel host)
        {
            List<DirectoryInfo> folders = new List<DirectoryInfo>();
            await Task.Run(() =>
            {
                try
                {
                    foreach (ShareInfo shareInfo in Host.EnumerateShares(host.FullPath, ShareType.DiskTree, true))
                    {
                        if (shareInfo.DirectoryInfo.Exists)
                            folders.Add(shareInfo.DirectoryInfo);
                    }
                }
                catch { }
            }).ConfigureAwait(false);

            List<FileModel> fileModelList = new List<FileModel>();
            foreach (DirectoryInfo directoryInfo in folders)
            {
                FileModel fileModel = FileModel.FromDirectoryInfo(directoryInfo);
                fileModel.Parent = host;

                fileModelList.Add(fileModel);
                FileSystemWatcherHelper.RegisterDirectoryWatcher(fileModel.FullPath);
            }

            return new FileModelCollection(fileModelList);
        }

        public static async Task<FileModelCollection> SearchFolder(string path, string searchPattern)
        {
            List<FileModel> fileModelList = new List<FileModel>();

            DirectoryInfo[] folders = await GetFolders(path, searchPattern);
            if (folders != null)
            {
                foreach (DirectoryInfo folder in folders)
                {
                    FileModel childFileModel = FileModel.FromDirectoryInfo(folder);
                    childFileModel.Parent = FileModel.FromDirectoryInfo(folder.Parent);

                    fileModelList.Add(childFileModel);
                }
            }

            FileInfo[] files = await GetFiles(path, searchPattern);
            if (files != null)
            {
                foreach (FileInfo file in files)
                {
                    FileModel childFileModel = FileModel.FromFileInfo(file);
                    childFileModel.Parent = FileModel.FromDirectoryInfo(file.Directory);

                    fileModelList.Add(childFileModel);
                }
            }

            return new FileModelCollection(fileModelList);
        }

        private static async Task<FileInfo[]> GetFiles(string path, string searchPattern = null)
        {
            FileInfo[] files = null;
            await Task.Run(() =>
            {
                try
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    if (directoryInfo.Exists)
                    {
                        bool emptySearchPattern = String.IsNullOrEmpty(searchPattern);
                        files = emptySearchPattern ? directoryInfo.GetFiles() : directoryInfo.GetFiles(searchPattern);
                    }
                }
                catch { }
            }).ConfigureAwait(false);

            return files;
        }

        private static async Task<DirectoryInfo[]> GetFolders(string path, string searchPattern = null)
        {
            DirectoryInfo[] folders = null;
            await Task.Run(() =>
            {
                try
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    if (directoryInfo.Exists)
                    {
                        bool emptySearchPattern = String.IsNullOrEmpty(searchPattern);
                        folders = emptySearchPattern ? directoryInfo.GetDirectories() : directoryInfo.GetDirectories(searchPattern);
                    }
                }
                catch { }
            }).ConfigureAwait(false);

            return folders;
        }

        private static FileModelCollection GetDrives()
        {
            List<FileModel> fileModelList = new List<FileModel>();

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    FileModel childFileModel = FileModel.FromDriveInfo(drive);
                    childFileModel.Parent = Computer;

                    fileModelList.Add(childFileModel);
                }
            }

            return new FileModelCollection(fileModelList);
        }

        private static FileModelCollection GetQuickAccess()
        {
            List<FileModel> fileModelList = new List<FileModel>();

            foreach (string folder in UserFolders)
            {
                FileModel childFileModel = FileModel.FromDirectoryInfo(new DirectoryInfo(folder));
                childFileModel.Parent = QuickAccess;

                fileModelList.Add(childFileModel);
            }

            if (!String.IsNullOrEmpty(Settings.Default.QuickAccessFolders))
            {
                foreach (string folder in Settings.Default.QuickAccessFolders.Split(';'))
                {
                    DirectoryInfo directory = new DirectoryInfo(folder);
                    if (directory.Exists)
                    {
                        FileModel childFileModel = FileModel.FromDirectoryInfo(directory);
                        childFileModel.Parent = QuickAccess;

                        fileModelList.Add(childFileModel);
                    }
                }
            }

            return new FileModelCollection(fileModelList);
        }
    }
}
