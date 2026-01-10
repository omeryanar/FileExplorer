using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Mvvm;
using DevExpress.Mvvm.CodeGenerators;
using DevExpress.Mvvm.Native;
using FileExplorer.Core;
using FileExplorer.Helpers;
using FileExplorer.Messages;
using MimeTypes;
using TagLib;
using Vanara.Windows.Shell;

namespace FileExplorer.Model
{
    [GenerateViewModel(ImplementIDataErrorInfo = true)]
    public partial class FileModel : IEditableObject
    {
        #region Fields

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
        private bool isImage;

        [GenerateProperty]
        private bool isVideo;

        [GenerateProperty]
        private bool isAudio;

        [GenerateProperty]
        private FileModelCollectionView content;

        [GenerateProperty]
        private FileModelCollectionView folders;

        protected FileModelCollection Children
        {
            get => children;
            set
            {
                children = value;
                if (children != null)
                {
                    Content = new FileModelCollectionView(children);
                    Folders = new FileModelCollectionView(children, x => x.IsDirectory);
                }
            }
        }
        private FileModelCollection children;

        #endregion

        #region Properties

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

        #endregion

        public ImageSource SmallIcon
        {
            get
            {
                if (smallIcon == null)
                {
                    smallIcon = NotifyTask.Create(IconHelper.GetIconAsync(FullPath, Extension, 16));
                    smallIcon.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(NotifyTask.IsCompleted) && smallIcon.IsCompleted)
                            RaisePropertyChanged(SmallIconChangedEventArgs);
                    };
                }

                if (smallIcon.IsFaulted)
                    return null;

                if (smallIcon.IsCompleted && smallIcon.Result == null)
                {
                    smallIcon = null;
                    return null;
                }

                return smallIcon.IsCompleted ? smallIcon.Result : null;
            }
        }
        private NotifyTask<ImageSource> smallIcon;

        public ImageSource MediumIcon
        {
            get
            {
                if (mediumIcon == null)
                {
                    mediumIcon = NotifyTask.Create(IconHelper.GetIconAsync(FullPath, Extension, 32));
                    mediumIcon.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(NotifyTask.IsCompleted) && mediumIcon.IsCompleted)
                            RaisePropertyChanged(MediumIconChangedEventArgs);
                    };
                }

                if (mediumIcon.IsFaulted)
                    return null;

                if (mediumIcon.IsCompleted && mediumIcon.Result == null)
                {
                    mediumIcon = null;
                    return null;
                }

                return mediumIcon.IsCompleted ? mediumIcon.Result : null;
            }
        }
        private NotifyTask<ImageSource> mediumIcon;

        public ImageSource LargeIcon
        {
            get
            {
                if (largeIcon == null)
                {
                    largeIcon = NotifyTask.Create(IconHelper.GetIconAsync(FullPath, Extension, 48));
                    largeIcon.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(NotifyTask.IsCompleted) && largeIcon.IsCompleted)
                            RaisePropertyChanged(LargeIconChangedEventArgs);
                    };
                }

                if (largeIcon.IsFaulted)
                    return null;

                if (largeIcon.IsCompleted && largeIcon.Result == null)
                {
                    largeIcon = null;
                    return null;
                }

                return largeIcon.IsCompleted ? largeIcon.Result : null;
            }
        }
        private NotifyTask<ImageSource> largeIcon;

        public ImageSource ThumbnailImage
        {
            get
            {
                if (ThumbnailHelper.ThumbnailExists(FullPath) == false)
                    return ExtraLargeIcon;

                if (thumbnailNotifyTask == null)
                {
                    thumbnailNotifyTask = NotifyTask.Create(ThumbnailHelper.GetThumbnailImage(FullPath));
                    thumbnailNotifyTask.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(NotifyTask.IsCompleted) && thumbnailNotifyTask.IsCompleted)
                            RaisePropertyChanged(ThumbnailImageChangedEventArgs);
                    };
                }

                return thumbnailNotifyTask.IsCompleted ? thumbnailNotifyTask.Result : ExtraLargeIcon;
            }
        }
        private NotifyTask<BitmapImage> thumbnailNotifyTask;

        public ImageSource ExtraLargeIcon
        {
            get
            {
                if (extraLargeIcon == null)
                {
                    extraLargeIcon = NotifyTask.Create(IconHelper.GetIconAsync(FullPath, Extension, 256));
                    extraLargeIcon.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(NotifyTask.IsCompleted) && extraLargeIcon.IsCompleted)
                        {
                            RaisePropertyChanged(ExtraLargeIconChangedEventArgs);
                            RaisePropertyChanged(ThumbnailImageChangedEventArgs);
                        }
                    };
                }

                if (extraLargeIcon.IsFaulted)
                    return null;

                if (extraLargeIcon.IsCompleted && extraLargeIcon.Result == null)
                {
                    extraLargeIcon = null;
                    return null;
                }

                return extraLargeIcon.IsCompleted ? extraLargeIcon.Result : null;
            }
        }
        private NotifyTask<ImageSource> extraLargeIcon;

        #endregion

        #region Methods

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

        private void Rename(string oldPath, string newPath)
        {
            FileModelCache.TryRemove(FullPath, out _);

            FullPath = FullPath.Replace(oldPath, newPath);
            ParentPath = ParentPath.Replace(oldPath, newPath);

            FileModelCache.TryAdd(FullPath, this);

            if (Children != null)
            {
                foreach (FileModel child in Children)
                    child.Rename(oldPath, newPath);
            }
        }

        public async Task<List<FileModel>> EnumerateParents()
        {
            List<FileModel> list = new List<FileModel> ();
            FileModel fileModel = this;

            await Task.Run(() =>
            {                
                while (fileModel != null)
                {
                    list.Add(fileModel);

                    if (!fileModel.IsRoot && fileModel.Parent == null)
                        fileModel.Parent = Create(fileModel.ParentPath);

                    fileModel = fileModel.Parent;
                }
            });            

            return list;
        }

        public virtual async Task EnumerateChildren()
        {
            if (IsDirectory)
            {
                List<FileModel> items = new List<FileModel>();
                List<Tuple<FileModel, FileSystemInfo>> updatedItems = new List<Tuple<FileModel, FileSystemInfo>>();

                await Task.Run(() =>
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(FullPath);
                    foreach (FileSystemInfo fileSystemInfo in directoryInfo.EnumerateFileSystemInfos())
                    {
                        if (FileModelCache.TryGetValue(fileSystemInfo.FullName, out FileModel fileModel))
                        {
                            var tuple = Tuple.Create(fileModel, fileSystemInfo);
                            updatedItems.Add(tuple);
                        }
                        else
                        {
                            fileModel = new FileModel();
                            fileModel.Initialize(fileSystemInfo, this);

                            FileModelCache.TryAdd(fileSystemInfo.FullName, fileModel);
                        }

                        items.Add(fileModel);
                    }
                });

                updatedItems.ForEach(x => x.Item1.Update(x.Item2));
                if (items.Count > 0)
                    LargestFileSize = items.Max(x => x.Size);

                Children = new FileModelCollection(items);
            }
        }

        #endregion

        #region Constructors

        public static FileModel Create(string path)
        {
            if (FileModelCache.TryGetValue(path, out FileModel fileModel))
                return fileModel;

            if (Directory.Exists(path))
                return Create(new DirectoryInfo(path));
            else if (System.IO.File.Exists(path))
                return Create(new FileInfo(path));
            
            if (FileSystemHelper.IsNetworkHost(path) && NetworkHostAccessible(path))
                return CreateNetworkHost(path);

            return null;
        }

        public static FileModel Create(FileSystemInfo fileSystemInfo, FileModel parentModel = null)
        {
            if (FileModelCache.TryGetValue(fileSystemInfo.FullName, out FileModel fileModel))
                return fileModel;

            fileModel = new FileModel();
            fileModel.Initialize(fileSystemInfo, parentModel);

            FileModelCache.TryAdd(fileSystemInfo.FullName, fileModel);

            return fileModel;
        }

        protected FileModel()
        {
        }

        protected void Initialize(FileSystemInfo fileSystemInfo, FileModel parentModel = null)
        {
            FullName = fileSystemInfo.Name;
            FullPath = fileSystemInfo.FullName;

            IsDrive = FileSystemHelper.IsDrive(FullPath);
            IsDirectory = fileSystemInfo.Attributes.HasFlag(FileAttributes.Directory);
            IsHidden = fileSystemInfo.Attributes.HasFlag(FileAttributes.Hidden);
            IsSystem = fileSystemInfo.Attributes.HasFlag(FileAttributes.System);

            DateCreated = fileSystemInfo.CreationTime;
            DateModified = fileSystemInfo.LastWriteTime;
            DateAccessed = fileSystemInfo.LastAccessTime;

            if (IsDrive)
            {
                DriveInfo driveInfo = new DriveInfo(FullPath);
                Size = driveInfo.TotalSize;
                FreeSpace = driveInfo.TotalFreeSpace;

                using(ShellItem shellItem = new ShellItem(FullPath))
                {
                    Name = shellItem.FileInfo.DisplayName;
                    FullName = shellItem.FileInfo.DisplayName;
                    Description = shellItem.FileInfo.TypeName;
                }         

                IsHidden = false;
                IsSystem = false;
            }
            else if (IsDirectory)
            {
                Name = fileSystemInfo.Name;
                Extension = String.Empty;
                Description = Properties.Resources.Folder;
            }
            else
            {
                Name = Path.GetFileNameWithoutExtension(fileSystemInfo.Name);
                Extension = fileSystemInfo.Extension;
                Description = FileSystemHelper.GetFileFriendlyDocName(Extension);

                if (fileSystemInfo is FileInfo fileInfo)
                    Size = fileInfo.Length;

                MimeType = MimeTypeMap.GetMimeType(Extension);
                IsImage = MimeType?.StartsWith("image") == true;
                IsVideo = MimeType?.StartsWith("video") == true;
                IsAudio = MimeType?.StartsWith("audio") == true;

                if (Extension.OrdinalEndsWith(".lnk"))
                {
                    using (ShellLink shellLink = new ShellLink(FullPath, LinkResolution.NoUI | LinkResolution.NoSearch))
                    {
                        if (shellLink.Target.IsFolder)
                            LinkPath = shellLink.TargetPath;
                    }
                }
            }

            Parent = parentModel;
            if (Parent == null)
            {
                FileSystemInfo parentDirectory = null;
                if (fileSystemInfo is FileInfo fileInfo)
                    parentDirectory = fileInfo.Directory;
                else if (fileSystemInfo is DirectoryInfo directoryInfo)
                    parentDirectory = directoryInfo.Parent;

                if (FileModelCache.TryGetValue(parentDirectory.FullName, out FileModel parent))
                    Parent = parent;
                else
                    Parent = Create(parentDirectory.FullName);
            }

            ParentName = Parent?.Name;
            ParentPath = Parent?.FullPath;
        }

        protected void Update(FileSystemInfo fileSystemInfo)
        {
            IsHidden = fileSystemInfo.Attributes.HasFlag(FileAttributes.Hidden);
            IsSystem = fileSystemInfo.Attributes.HasFlag(FileAttributes.System);

            DateCreated = fileSystemInfo.CreationTime;
            DateModified = fileSystemInfo.LastWriteTime;
            DateAccessed = fileSystemInfo.LastAccessTime;

            if (fileSystemInfo is FileInfo fileInfo)
                Size = fileInfo.Length;
        }

        protected void ChangeExtension(string newExtension)
        {
            Extension = newExtension;
            Description = FileSystemHelper.GetFileFriendlyDocName(Extension);

            smallIcon = null;
            mediumIcon = null;
            largeIcon = null;
            extraLargeIcon = null;

            RaisePropertyChanged(SmallIconChangedEventArgs);
            RaisePropertyChanged(MediumIconChangedEventArgs);
            RaisePropertyChanged(LargeIconChangedEventArgs);
            RaisePropertyChanged(ExtraLargeIconChangedEventArgs);
        }

        #endregion

        #region Static

        private static readonly ConcurrentDictionary<String, FileModel> FileModelCache = new ConcurrentDictionary<String, FileModel>();        

        private static PropertyChangedEventArgs SmallIconChangedEventArgs = new PropertyChangedEventArgs(nameof(SmallIcon));

        private static PropertyChangedEventArgs MediumIconChangedEventArgs = new PropertyChangedEventArgs(nameof(MediumIcon));

        private static PropertyChangedEventArgs LargeIconChangedEventArgs = new PropertyChangedEventArgs(nameof(LargeIcon));

        private static PropertyChangedEventArgs ExtraLargeIconChangedEventArgs = new PropertyChangedEventArgs(nameof(ExtraLargeIcon));

        private static PropertyChangedEventArgs ThumbnailImageChangedEventArgs = new PropertyChangedEventArgs(nameof(ThumbnailImage));

        static FileModel()
        {
            QuickAccess = new QuickAccessModel();
            Computer = new ComputerModel();
            Network = new NetworkModel();

            Messenger.Default.Register(App.Current, (NotificationMessage message) =>
            {
                if (message.NotificationType == NotificationType.Add)
                {
                    string parentPath = FileSystemHelper.GetParentFolderPath(message.Path);
                    if (FileModelCache.TryGetValue(parentPath, out FileModel parent))
                    {
                        if (parent.Children == null)
                            return;

                        string parsingName = FileSystemHelper.GetFileParsingName(message.Path);
                        if (String.IsNullOrEmpty(parsingName))
                            return;

                        FileSystemInfo fileSystemInfo;
                        if (Directory.Exists(parsingName))
                            fileSystemInfo = new DirectoryInfo(parsingName);
                        else
                            fileSystemInfo = new FileInfo(parsingName);

                        if (FileModelCache.TryGetValue(parsingName, out FileModel fileModel))
                            fileModel.Initialize(fileSystemInfo, parent);
                        else
                        {
                            fileModel = FileModel.Create(fileSystemInfo, parent);
                            parent.Children.Add(fileModel);
                        }

                        if (fileModel.Size > parent.LargestFileSize)
                            parent.LargestFileSize = fileModel.Size;
                    }
                }
                else if (message.NotificationType == NotificationType.Remove)
                {
                    string parentPath = FileSystemHelper.GetParentFolderPath(message.Path);
                    if (FileModelCache.TryGetValue(parentPath, out FileModel parent) && FileModelCache.ContainsKey(message.Path))
                    {
                        FileModelCache.TryRemove(message.Path, out FileModel fileModel);

                        if (parent.Children != null)
                        {
                            parent.Children.Remove(fileModel);

                            if (parent.LargestFileSize == fileModel.Size && parent.Children.Count > 0)
                                parent.LargestFileSize = parent.Children.Max(x => x.Size);
                        }
                    }
                }
                else if (message.NotificationType == NotificationType.Rename)
                {
                    if (FileModelCache.ContainsKey(message.Path))
                    {
                        if (FileModelCache.TryGetValue(message.NewPath, out FileModel fileModel) && message.NewPath.OrdinalEquals(fileModel.FullPath))
                            return;

                        FileModelCache.TryRemove(message.Path, out fileModel);
                        FileModelCache.TryAdd(message.NewPath, fileModel);

                        string parsingName = FileSystemHelper.GetFileParsingName(message.NewPath);
                        if (String.IsNullOrEmpty(parsingName))
                            return;
                        
                        fileModel.FullName = Path.GetFileName(parsingName);
                        fileModel.FullPath = parsingName;

                        if (fileModel.IsDirectory)
                        {
                            fileModel.Name = fileModel.FullName;
                        }
                        else
                        {
                            fileModel.Name = Path.GetFileNameWithoutExtension(parsingName);

                            string extension = Path.GetExtension(parsingName);
                            if (fileModel.Extension.OrdinalEquals(extension) == false)
                                fileModel.ChangeExtension(extension);
                        }                        

                        if (fileModel.Children != null)
                        {
                            foreach (FileModel item in fileModel.Children)
                            {
                                item.ParentName = fileModel.Name;
                                item.Rename(message.Path, message.NewPath);
                            }
                        }
                    }
                }
                else if (message.NotificationType == NotificationType.Update)
                {
                    if (FileModelCache.TryGetValue(message.Path, out FileModel fileModel))
                    {
                        if (fileModel.IsRoot || fileModel.IsDrive)
                            return;

                        long oldSize = fileModel.Size;

                        if (Directory.Exists(message.Path))
                            fileModel.Update(new DirectoryInfo(message.Path));
                        else
                            fileModel.Update(new FileInfo(message.Path));

                        if (fileModel.Parent?.LargestFileSize == oldSize || fileModel.Parent?.LargestFileSize < fileModel.Size)
                            fileModel.Parent.LargestFileSize = fileModel.Size;
                    }
                }
            });
        }

        #endregion

        #region QuickAccess

        public static FileModel QuickAccess { get; private set; }

        public static void AddToQuickAccess(FileModel fileModel)
        {
            if (fileModel?.IsDirectory == true)
                QuickAccess.Children.Add(fileModel);
        }

        public static void RemoveFromQuickAccess(FileModel fileModel)
        {
            if (fileModel?.IsDirectory == true && QuickAccess.Children.Contains(fileModel))
                QuickAccess.Children.Remove(fileModel);
        }

        private class QuickAccessModel : FileModel
        {
            public QuickAccessModel()
            {
                using (ShellFolder shellFolder = new ShellFolder(FileSystemHelper.QuickAccessPath))
                {
                    Name = shellFolder.Name;
                    FullName = shellFolder.Name;
                    FullPath = FileSystemHelper.QuickAccessPath;

                    IsRoot = true;
                    IsDirectory = true;
                    ParentPath = String.Empty;

                    FileModelCache.TryAdd(shellFolder.Name, this);
                    FileModelCache.TryAdd(shellFolder.ParsingName, this);
                }

                EnumerateChildren();
            }

            public override Task EnumerateChildren()
            {
                IList<string> folders = FileSystemHelper.GetQuickAccess();
                Children = new FileModelCollection();

                foreach (string folder in folders)
                {
                    if (Directory.Exists(folder))
                        Children.Add(Create(new DirectoryInfo(folder), this));
                }

                return Task.CompletedTask;
            }
        }

        #endregion

        #region Computer

        public static FileModel Computer { get; private set; }

        private class ComputerModel : FileModel
        {
            public ComputerModel()
            {
                using (ShellFolder shellFolder = new ShellFolder(FileSystemHelper.ComputerPath))
                {
                    Name = shellFolder.Name;
                    FullName = shellFolder.Name;
                    FullPath = FileSystemHelper.ComputerPath;

                    IsRoot = true;
                    IsDirectory = true;
                    ParentPath = String.Empty;

                    FileModelCache.TryAdd(shellFolder.Name, this);
                    FileModelCache.TryAdd(shellFolder.ParsingName, this);
                }

                EnumerateChildren();              
            }

            public override Task EnumerateChildren()
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                Children = new FileModelCollection(drives.Select(x => Create(x.RootDirectory, this)));

                return Task.CompletedTask;
            }
        }

        #endregion

        #region Network

        public static FileModel Network { get; private set; }

        public static bool FolderExists(string path)
        {
            if (FileModelCache.ContainsKey(path))
                return true;

            return Directory.Exists(path);
        }

        private class NetworkModel : FileModel
        {
            public NetworkModel()
            {
                using(ShellFolder shellFolder = new ShellFolder(FileSystemHelper.NetworkPath))
                {
                    Name = shellFolder.Name;
                    FullName = shellFolder.Name;
                    FullPath = FileSystemHelper.NetworkPath;

                    IsRoot = true;
                    IsDirectory = true;
                    ParentPath = String.Empty;

                    FileModelCache.TryAdd(shellFolder.Name, this);
                    FileModelCache.TryAdd(shellFolder.ParsingName, this);
                }

                EnumerateChildren();
            }

            public override Task EnumerateChildren()
            {
                if (Children == null)
                    Children = new FileModelCollection();

                foreach (FileModel host in Children)
                {
                    if (!NetworkHostAccessible(host.FullPath))
                    {
                        FileModelCache.TryRemove(host.FullPath, out _);
                        Children.Remove(host);
                    }
                }

                return Task.CompletedTask;
            }
        }

        private class NetworkHostModel : FileModel
        {
            public NetworkHostModel(string path)
            {
                Name = FileSystemHelper.GetHostName(path); ;
                FullName = Name;
                FullPath = path;
                Extension = String.Empty;

                IsRoot = true;
                IsDirectory = true;

                Parent = Network;
                ParentPath = FileSystemHelper.NetworkPath;

                FileModelCache.TryAdd(path, this);
            }

            public async override Task EnumerateChildren()
            {
                List<FileModel> items = new List<FileModel>();
                List<Tuple<FileModel, ShellFileInfo>> updatedItems = new List<Tuple<FileModel, ShellFileInfo>>();

                await Task.Run(() =>
                {
                    using (ShellFolder shellFolder = new ShellFolder(FullPath))
                    {
                        foreach (ShellItem shellItem in shellFolder.EnumerateChildren(FolderItemFilter.Folders))
                        {
                            try
                            {
                                if (FileModelCache.TryGetValue(shellItem.ParsingName, out FileModel fileModel))
                                {
                                    var tuple = Tuple.Create(fileModel, shellItem.FileInfo);
                                    updatedItems.Add(tuple);
                                }
                                else
                                {
                                    fileModel = new FileModel();
                                    fileModel.Initialize(shellItem.FileInfo, this);

                                    FileModelCache.TryAdd(shellItem.ParsingName, fileModel);                                    
                                }

                                items.Add(fileModel);
                                FileSystemWatcherHelper.RegisterDirectoryWatcher(shellItem.ParsingName);
                            }
                            catch { continue; }
                        }
                    }
                });

                updatedItems.ForEach(x => x.Item1.Update(x.Item2));
                Children = new FileModelCollection(items);
            }
        }

        public static bool NetworkHostAccessible(string path)
        {
            if (!FileSystemHelper.IsNetworkHost(path))
                return false;

            try
            {
                ShellFolder shellFolder = new ShellFolder(path);
                foreach (var item in shellFolder.EnumerateChildIds(FolderItemFilter.Folders))
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static FileModel CreateNetworkHost(string path)
        {
            if (FileSystemHelper.IsNetworkHost(path))
            {
                using (ShellFolder shellFolder = new ShellFolder(path))
                {
                    if (FileModelCache.TryGetValue(shellFolder.ParsingName, out FileModel host))
                        return host;

                    host = new NetworkHostModel(shellFolder.ParsingName);
                    Network.Children.Add(host);

                    return host;
                }
            }

            return null;
        }

        #endregion
    }
}
