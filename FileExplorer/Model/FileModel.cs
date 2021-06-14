using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using Alphaleonis.Win32.Filesystem;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using FileExplorer.Core;
using FileExplorer.Helpers;
using FileExplorer.Messages;
using FileExplorer.Properties;
using FileExplorer.Resources;

namespace FileExplorer.Model
{
    public class FileModel
    {
        public virtual long Size { get; set; }

        public virtual long FreeSpace { get; set; }

        public virtual string Name { get; set; }

        public virtual string FullName { get; set; }

        public virtual string FullPath { get; set; }

        public virtual string ParentName { get; set; }

        public virtual string ParentPath { get; set; }

        public virtual string Extension { get; set; }

        public virtual string Description { get; set; }

        public virtual DateTime DateCreated { get; set; }

        public virtual DateTime DateModified { get; set; }

        public virtual DateTime DateAccessed { get; set; }

        public virtual object Tag { get; set; }

        public virtual FileModel Parent { get; set; }

        public virtual FileModelCollection Files { get; set; }

        public virtual FileModelCollection Folders { get; set; }

        public bool IsDirectory { get; internal set; }

        public bool IsDrive { get; internal set; }

        public bool IsHidden { get; internal set; }

        public bool IsSystem { get; internal set; }

        public bool IsRoot { get; internal set; }

        public ImageSource SmallIcon
        {
            get { return FileSystemImageHelper.GetImage(this, IconSize.Small); }
        }

        public ImageSource MediumIcon
        {
            get { return FileSystemImageHelper.GetImage(this, IconSize.Medium); }
        }

        public ImageSource LargeIcon
        {
            get { return FileSystemImageHelper.GetImage(this, IconSize.Large); }
        }

        public ImageSource ExtraLargeIcon
        {
            get { return FileSystemImageHelper.GetImage(this, IconSize.ExtraLarge); }
        }

        public static FileModel FromPath(string path)
        {
            FileModel fileModel = FileModelCache.GetOrAdd(path, Create);
            if (fileModel.IsRoot)
                return fileModel;

            if (FileSystemHelper.IsDrive(path))
            {
                DriveInfo driveInfo = new DriveInfo(path);
                fileModel.Initialize(driveInfo);
            }
            else if (FileSystemHelper.IsNetworkHost(path))
            {
                string hostName = FileSystemHelper.GetHostName(path);

                fileModel.Name = hostName;
                fileModel.FullName = hostName;
                fileModel.FullPath = path;
                fileModel.Extension = String.Empty;

                fileModel.IsRoot = true;
                fileModel.IsDirectory = true;

                fileModel.Parent = FileSystemHelper.Network;
                fileModel.ParentPath = FileSystemHelper.NetworkPath;

                FileSystemHelper.Network.Folders.Add(fileModel);
            }
            else if (FileSystemHelper.IsNetworkShare(path))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                fileModel.Initialize(directoryInfo);

                fileModel.ParentPath = FileSystemHelper.GetParentFolderPath(path);
                fileModel.Parent = FromPath(fileModel.ParentPath);
            }
            else if (Directory.Exists(path))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                fileModel.Initialize(directoryInfo);
            }
            else
            {
                FileInfo fileInfo = new FileInfo(path);
                fileModel.Initialize(fileInfo);
            }

            return fileModel;
        }

        public static FileModel FromDirectoryInfo(DirectoryInfo directoryInfo)
        {
            FileModel fileModel = FileModelCache.GetOrAdd(directoryInfo.FullName, Create);
            fileModel.Initialize(directoryInfo);

            return fileModel;
        }

        public static FileModel FromFileInfo(FileInfo fileInfo)
        {
            FileModel fileModel = FileModelCache.GetOrAdd(fileInfo.FullName, Create);
            fileModel.Initialize(fileInfo);

            return fileModel;
        }

        public static FileModel FromDriveInfo(DriveInfo driveInfo)
        {
            FileModel fileModel = FileModelCache.GetOrAdd(driveInfo.Name, Create);
            fileModel.Initialize(driveInfo);

            return fileModel;
        }

        public override string ToString()
        {
            return Name;
        }

        private void Initialize(DirectoryInfo info)
        {
            Name = info.Name;
            FullName = info.Name;
            FullPath = info.FullName;
            Extension = String.Empty;

            ParentName = info.Parent?.Name;
            ParentPath = info.Parent?.FullName;

            Description = "Folder";

            DateCreated = info.CreationTime;
            DateModified = info.LastWriteTime;
            DateAccessed = info.LastAccessTime;

            if (info.EntryInfo != null)
            {
                IsHidden = info.EntryInfo.IsHidden;
                IsSystem = info.EntryInfo.IsSystem;
            }

            IsDirectory = true;
        }

        private void Initialize(FileInfo info)
        {
            Name = Path.GetFileNameWithoutExtension(info.FullName);
            FullName = info.Name;
            FullPath = info.FullName;
            Extension = info.Extension;

            ParentName = info.Directory.Name;
            ParentPath = info.Directory.FullName;

            DateCreated = info.CreationTime;
            DateModified = info.LastWriteTime;
            DateAccessed = info.LastAccessTime;

            if (info.EntryInfo != null)
            {
                IsHidden = info.EntryInfo.IsHidden;
                IsSystem = info.EntryInfo.IsSystem;
            }

            try
            {
                Description = Shell32.GetFileFriendlyDocName(info.FullName);
                Size = info.Length;
            }
            catch { }
        }

        private void Initialize(DriveInfo info)
        {
            Name = info.Name;
            FullName = info.Name;
            FullPath = info.Name;
            Extension = String.Empty;

            ParentName = Properties.Resources.Computer;
            ParentPath = FileSystemHelper.ComputerPath;

            if (info.DriveType == System.IO.DriveType.Removable)
                Description = "USB Drive";
            else if (info.DriveType == System.IO.DriveType.CDRom)
                Description = "CD Drive";
            else
                Description = "Local Disk";

            if (!String.IsNullOrEmpty(info.VolumeLabel))
            {
                Name = String.Format("{0} ({1})", info.VolumeLabel, Path.RemoveTrailingDirectorySeparator(Name));
                FullName = Name;
            }
            else
            {
                Name = String.Format("{0} ({1})", Description, Path.RemoveTrailingDirectorySeparator(Name));
                FullName = Name;
            }

            DateCreated = info.RootDirectory.CreationTime;
            DateModified = info.RootDirectory.LastWriteTime;
            DateAccessed = info.RootDirectory.LastAccessTime;

            IsDrive = true;
            IsDirectory = true;

            Size = info.TotalSize;
            FreeSpace = info.TotalFreeSpace;
        }

        private void Rename(string oldPath, string newPath)
        {
            FileModelCache.TryRemove(FullPath, out _);

            FullPath = FullPath.Replace(oldPath, newPath);
            ParentPath = ParentPath.Replace(oldPath, newPath);

            FileModelCache.TryAdd(FullPath, this);

            if (Folders != null)
            {
                foreach (FileModel folder in Folders)
                    folder.Rename(oldPath, newPath);
            }

            if (Files != null)
            {
                foreach (FileModel file in Files)
                    file.Rename(oldPath, newPath);
            }
        }

        private static FileModel Create(string path)
        {
            FileModel fileModel = ViewModelSource.Create<FileModel>();

            switch (path)
            {
                case FileSystemHelper.ComputerPath:
                    fileModel.IsRoot = true;
                    fileModel.IsDirectory = true;
                    fileModel.ParentPath = String.Empty;
                    fileModel.Name = Properties.Resources.Computer;
                    fileModel.FullName = Properties.Resources.Computer;
                    fileModel.FullPath = FileSystemHelper.ComputerPath;
                    break;

                case FileSystemHelper.QuickAccessPath:
                    fileModel.IsRoot = true;
                    fileModel.IsDirectory = true;
                    fileModel.ParentPath = String.Empty;
                    fileModel.Name = Properties.Resources.QuickAccess;
                    fileModel.FullName = Properties.Resources.QuickAccess;
                    fileModel.FullPath = FileSystemHelper.QuickAccessPath;
                    break;

                case FileSystemHelper.NetworkPath:
                    fileModel.IsRoot = true;
                    fileModel.IsDirectory = true;
                    fileModel.ParentPath = String.Empty;
                    fileModel.Name = Properties.Resources.Network;
                    fileModel.FullName = Properties.Resources.Network;
                    fileModel.FullPath = FileSystemHelper.NetworkPath;
                    fileModel.Folders = new FileModelCollection();
                    break;
            }

            return fileModel;
        }

        private static ConcurrentDictionary<String, FileModel> FileModelCache = new ConcurrentDictionary<String, FileModel>(StringComparer.OrdinalIgnoreCase);

        static FileModel()
        {
            Messenger.Default.Register(Application.Current, (NotificationMessage message) =>
            {
                if (message.NotificationType == NotificationType.Add)
                {
                    string parentPath = FileSystemHelper.GetParentFolderPath(message.Path);
                    if (FileModelCache.TryGetValue(parentPath, out FileModel parentFileModel))
                    {
                        string parsingName = FileSystemHelper.GetFileParsingName(message.Path);
                        if (String.IsNullOrEmpty(parsingName))
                            return;

                        FileModel fileModel = FromPath(parsingName);
                        if (fileModel.IsDirectory)
                            parentFileModel.Folders?.Add(fileModel);
                        else
                            parentFileModel.Files?.Add(fileModel);
                    }
                }
                else if (message.NotificationType == NotificationType.Remove)
                {
                    string parentPath = FileSystemHelper.GetParentFolderPath(message.Path);
                    if (FileModelCache.TryGetValue(parentPath, out FileModel parentFileModel) && FileModelCache.ContainsKey(message.Path))
                    {
                        FileModelCache.TryRemove(message.Path, out FileModel fileModel);
                        if (fileModel.IsDirectory)
                            parentFileModel.Folders?.Remove(fileModel);
                        else
                            parentFileModel.Files?.Remove(fileModel);
                    }
                }
                else if (message.NotificationType == NotificationType.Rename)
                {
                    if (FileModelCache.ContainsKey(message.Path))
                    {
                        FileModelCache.TryRemove(message.Path, out FileModel fileModel);

                        string parsingName = FileSystemHelper.GetFileParsingName(message.NewPath);
                        if (String.IsNullOrEmpty(parsingName))
                            return;

                        FileModelCache.TryAdd(parsingName, fileModel);
                        FromPath(parsingName);

                        if (fileModel.Folders != null)
                        {
                            foreach (FileModel folder in fileModel.Folders)
                            {
                                folder.ParentName = fileModel.Name;
                                folder.Rename(message.Path, message.NewPath);
                            }
                        }

                        if (fileModel.Files != null)
                        {
                            foreach (FileModel file in fileModel.Files)
                            {
                                file.ParentName = fileModel.Name;
                                file.Rename(message.Path, message.NewPath);
                            }
                        }
                    }
                }
                else if (message.NotificationType == NotificationType.Update)
                {
                    if (FileModelCache.ContainsKey(message.Path))
                    {
                        string parsingName = FileSystemHelper.GetFileParsingName(message.Path);
                        if (String.IsNullOrEmpty(parsingName))
                            return;

                        FromPath(parsingName);
                    }
                }
            });

            Settings.Default.SettingsSaving += (s, e) =>
            {
                if (Properties.Resources.Culture.Name != Settings.Default.Language)
                {
                    CultureResources.ChangeCulture(Settings.Default.Language);

                    FileSystemHelper.Computer.Name = Properties.Resources.Computer;
                    FileSystemHelper.Computer.FullName = Properties.Resources.Computer;

                    FileSystemHelper.QuickAccess.Name = Properties.Resources.QuickAccess;
                    FileSystemHelper.QuickAccess.FullName = Properties.Resources.QuickAccess;

                    FileSystemHelper.Network.Name = Properties.Resources.Network;
                    FileSystemHelper.Network.FullName = Properties.Resources.Network;
                }
            };
        }
    }
}
