using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Alphaleonis.Win32.Filesystem;
using DevExpress.Mvvm;
using DevExpress.Mvvm.CodeGenerators;
using FileExplorer.Core;
using FileExplorer.Helpers;
using FileExplorer.Messages;
using FileExplorer.Properties;
using FileExplorer.Resources;
using MimeTypes;
using Shellify;
using TagLib;

namespace FileExplorer.Model
{
    [GenerateViewModel(ImplementIDataErrorInfo = true)]
    public partial class FileModel : IEditableObject
    {
        [GenerateProperty]
        private long size;

        [GenerateProperty]
        private long freeSpace;

        [GenerateProperty]
        private long largestFileSize;

        [GenerateProperty]
        [Required(ErrorMessageResourceName = "InvalidFileNameMessage", ErrorMessageResourceType = typeof(Properties.Resources))]
        [RegularExpression(@"^[^\/<>*?:""|\\]*$", ErrorMessageResourceName = "InvalidFileNameMessage", ErrorMessageResourceType = typeof(Properties.Resources))]
        private string name;

        [GenerateProperty]
        private string fullName;

        [GenerateProperty]
        private string fullPath;

        [GenerateProperty]
        private string parentName;

        [GenerateProperty]
        private string parentPath;

        [GenerateProperty]
        private string extension;

        [GenerateProperty]
        private string mimeType;

        [GenerateProperty]
        private string description;

        [GenerateProperty]
        private string linkPath;

        [GenerateProperty]
        private DateTime dateCreated;

        [GenerateProperty]
        private DateTime dateModified;

        [GenerateProperty]
        private DateTime dateAccessed;

        [GenerateProperty]
        private object tag;

        [GenerateProperty]
        private FileModel parent;

        [GenerateProperty]
        private FileModelCollection files;

        [GenerateProperty]
        private FileModelCollection folders;

        [GenerateProperty]
        private FileModelReadOnlyCollection content;

        [GenerateProperty]
        private bool isImage;

        [GenerateProperty]
        private bool isVideo;

        [GenerateProperty]
        private bool isAudio;

        public bool IsDirectory { get; internal set; }

        public bool IsDrive { get; internal set; }

        public bool IsHidden { get; internal set; }

        public bool IsSystem { get; internal set; }

        public bool IsRoot { get; internal set; }

       public bool IsMedia => IsImage || IsVideo || IsAudio;

        #region MediaInfo

        private TagLib.File mediaInfo;

        public int? Width
        {
            get
            {
                if (ReadMediaInfo())
                {
                    if (mediaInfo.Properties.MediaTypes.HasFlag(MediaTypes.Photo))
                        return mediaInfo.Properties.PhotoWidth;
                    else if (mediaInfo.Properties.MediaTypes.HasFlag(MediaTypes.Video))
                        return mediaInfo.Properties.VideoWidth;
                }

                return null;
            }
        }
        
        public int? Height
        {
            get
            {
                if (ReadMediaInfo())
                {
                    if (mediaInfo.Properties.MediaTypes.HasFlag(MediaTypes.Photo))
                        return mediaInfo.Properties.PhotoHeight;
                    else if (mediaInfo.Properties.MediaTypes.HasFlag(MediaTypes.Video))
                        return mediaInfo.Properties.VideoHeight;
                }

                return null;
            }
        }
        
        public TimeSpan? Duration
        {
            get
            {
                if (ReadMediaInfo())
                {
                    if (mediaInfo.Properties.MediaTypes.HasFlag(MediaTypes.Video) || mediaInfo.Properties.MediaTypes.HasFlag(MediaTypes.Audio))
                        return mediaInfo.Properties.Duration;
                }

                return null;
            }
        }
        
        public int? AudioBitrate
        {
            get
            {
                if (ReadMediaInfo())
                {
                    if (mediaInfo.Properties.MediaTypes.HasFlag(MediaTypes.Audio))
                        return mediaInfo.Properties.AudioBitrate;
                }

                return null;
            }
        }
        
        public int? AudioChannels
        {
            get
            {
                if (ReadMediaInfo())
                {
                    if (mediaInfo.Properties.MediaTypes.HasFlag(MediaTypes.Audio))
                        return mediaInfo.Properties.AudioChannels;
                }

                return null;
            }
        }
        
        public int? AudioSampleRate
        {
            get
            {
                if (ReadMediaInfo())
                {
                    if (mediaInfo.Properties.MediaTypes.HasFlag(MediaTypes.Audio))
                        return mediaInfo.Properties.AudioSampleRate;
                }

                return null;
            }
        }
        
        public string Album
        {
            get
            {
                if (ReadMediaInfo())
                    return mediaInfo.Tag.Album;

                return null;
            }
        }
        
        public string Title
        {
            get
            {
                if (ReadMediaInfo())
                    return mediaInfo.Tag.Title;

                return null;
            }
        }
        
        public string Genre
        {
            get
            {
                if (ReadMediaInfo())
                    return mediaInfo.Tag.JoinedGenres;

                return null;
            }
        }
        
        public string AlbumArtists
        {
            get
            {
                if (ReadMediaInfo())
                    return mediaInfo.Tag.JoinedAlbumArtists;

                return null;
            }
        }
        
        public string ContributingArtists
        {
            get
            {
                if (ReadMediaInfo())
                    return mediaInfo.Tag.JoinedPerformers;

                return null;
            }
        }
        
        public string Composers
        {
            get
            {
                if (ReadMediaInfo())
                    return mediaInfo.Tag.JoinedComposers;

                return null;
            }
        }
        
        public uint? Year
        {
            get
            {
                if (ReadMediaInfo() && mediaInfo.Tag.Year > 0)
                    return mediaInfo.Tag.Year;

                return null;
            }
        }
        
        public uint? Track
        {
            get
            {
                if (ReadMediaInfo() && mediaInfo.Tag.Track > 0)
                    return mediaInfo.Tag.Track;

                return null;
            }
        }
        
        public uint? TrackCount
        {
            get
            {
                if (ReadMediaInfo() && mediaInfo.Tag.TrackCount > 0)
                    return mediaInfo.Tag.TrackCount;

                return null;
            }
        }

        public byte[] Picture
        {
            get
            {
                if (ReadMediaInfo() && mediaInfo.Tag.Pictures?.Length > 0)
                    return mediaInfo.Tag.Pictures[0].Data.Data;

                return null;
            }
        }

        public bool ReadMediaInfo()
        {
            if (IsMedia == false)
                return false;

            if (mediaInfo == null)
            {
                try
                {
                    mediaInfo = TagLib.File.Create(FullPath);

                    if (IsImage && mediaInfo.Properties == null)
                        IsImage = false;

                    if (IsVideo && mediaInfo.Properties == null)
                        IsVideo = false;

                    if (IsAudio && mediaInfo.Tag == null)
                        IsAudio = false;
                }
                catch (Exception)
                {
                    IsImage = false;
                    IsVideo = false;
                    IsAudio = false;
                }
            }

            if (IsImage || IsVideo)
                return mediaInfo?.Properties != null;
            if (IsAudio)
                return mediaInfo?.Tag != null;

            return false;
        }

        public static readonly string[] MediaInfoFields =
        [
            nameof(Width), nameof(Height),nameof(Duration),
            nameof(AudioBitrate), nameof(AudioChannels), nameof(AudioSampleRate),
            nameof(Album), nameof(Title), nameof(Genre), nameof(AlbumArtists), nameof(ContributingArtists), nameof(Composers),nameof(Year), nameof(Track), nameof(TrackCount)
        ];

        #endregion

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

        public ImageSource ThumbnailImage
        {
            get
            {
                if (Regex.Match(FullPath, FileSystemImageHelper.SupportedThumbnailImageFormats, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Success == false)
                    return ExtraLargeIcon;

                if (notifyTask == null)
                {
                    notifyTask = NotifyTask.Create<BitmapImage>(FileSystemImageHelper.GetThumbnailImage(FullPath));
                    notifyTask.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(NotifyTask.IsCompleted) && notifyTask.IsCompleted)
                            RaisePropertyChanged(new PropertyChangedEventArgs(nameof(ThumbnailImage)));
                    };
                }

                return notifyTask.IsCompleted ? notifyTask.Result: ExtraLargeIcon;
            }
        }
        private NotifyTask<BitmapImage> notifyTask;
        
        public ImageSource ExtraLargeIcon
        {
            get { return FileSystemImageHelper.GetImage(this, IconSize.ExtraLarge); }
        }

        public static FileModel FromPath(string path, bool refresh = true)
        {
            if (!refresh && FileModelCache.TryGetValue(path, out FileModel fromCache))
                return fromCache;

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

        public void BeginEdit()
        {

        }

        public void EndEdit()
        {
            string newName = Name.Trim() + Extension;
            if (newName == FullName)
                return;

            bool successful = Utilities.RenameFile(FullPath, newName);            
            if (!successful)
                CancelEdit();
        }

        public void CancelEdit()
        {
            if (IsDirectory)
                Name = FullName;
            else
                Name = Path.GetFileNameWithoutExtension(FullPath);
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

            MimeType = MimeTypeMap.GetMimeType(Extension);
            IsImage = MimeType?.StartsWith("image") == true;
            IsVideo = MimeType?.StartsWith("video") == true;
            IsAudio = MimeType?.StartsWith("audio") == true;

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

            if (info.Extension.OrdinalEndsWith(".lnk"))
            {
                try
                {
                    ShellLinkFile shellLinkFile = ShellLinkFile.Load(info.FullName);
                    if (shellLinkFile != null && shellLinkFile.Header.FileAttributes.HasFlag(System.IO.FileAttributes.Directory))
                        LinkPath = shellLinkFile.LinkInfo.LocalBasePath;
                }
                catch { }
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
            FileModel fileModel = new FileModel();

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
                        fileModel.Parent = parentFileModel;

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
