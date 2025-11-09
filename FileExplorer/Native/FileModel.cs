using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AsyncKeyedLock;
using DevExpress.Mvvm.CodeGenerators;
using FileExplorer.Core;
using FileExplorer.Helpers;
using MimeTypes;
using TagLib;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;

namespace FileExplorer.Model2
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
            get; set;
        }

        public ImageSource MediumIcon
        {
            get; set;
        }

        public ImageSource LargeIcon
        {
            get; set;
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
            get; set;
        }

        public override string ToString()
        {
            return Name;
        }

        public virtual IEnumerable<FileModel> EnumerateChildren()
        {
            if (ShellItem is ShellFolder shellFolder)
                return shellFolder.EnumerateChildren(FolderItemFilter.Folders | FolderItemFilter.NonFolders | FolderItemFilter.IncludeHidden | FolderItemFilter.IncludeSuperHidden).Select(x => GetInstance(x));

            return null;
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

        protected void Initialize(ShellItem shellItem)
        {
            ShellItem = shellItem;

            Extension = Path.GetExtension(shellItem.Name);
            Description = shellItem.FileInfo.TypeName;

            Name = Path.GetFileNameWithoutExtension(shellItem.Name);
            FullName = shellItem.Name;
            FullPath = shellItem.ParsingName;

            MimeType = MimeTypeMap.GetMimeType(Extension);
            IsImage = MimeType?.StartsWith("image") == true;
            IsVideo = MimeType?.StartsWith("video") == true;
            IsAudio = MimeType?.StartsWith("audio") == true;

            SetParent(shellItem.Parent);
            SetFileInfo(shellItem.FileInfo);
        }        

        protected void Initialize(ShellFolder shellFolder)
        {
            ShellItem = shellFolder;

            IsDirectory = true;
            Extension = String.Empty;
            Description = Properties.Resources.Folder;

            Name = shellFolder.Name;
            FullName = shellFolder.Name;
            FullPath = shellFolder.ParsingName;

            SetParent(shellFolder.Parent);
            SetFileInfo(shellFolder.FileInfo);
            
            IsDrive = Regex.IsMatch(FullPath, @"^[A-Za-z]:\\?$");
            if (IsDrive)
            {
                DriveInfo driveInfo = new DriveInfo(FullPath);
                Size = driveInfo.TotalSize;
                FreeSpace = driveInfo.TotalFreeSpace;
            }
        }

        protected void Initialize(ShellLink shellLink)
        {
            Initialize(shellLink as ShellItem);
            LinkPath = shellLink.TargetPath;
        }

        protected void SetParent(ShellFolder shellFolder)
        {
            if (shellFolder != null || shellFolder != ShellFolder.Desktop)
            {
                ParentName = shellFolder.Name;
                ParentPath = shellFolder.ParsingName;

                Parent = GetInstance(shellFolder);
            }
        }

        protected void SetFileInfo(ShellFileInfo shellFileInfo)
        {
            if (shellFileInfo != null)
            {
                DateCreated = shellFileInfo.CreationTime;
                DateModified = shellFileInfo.LastWriteTime;
                DateAccessed = shellFileInfo.LastAccessTime;

                IsHidden = shellFileInfo.Attributes.HasFlag(FileAttributes.Hidden);
                IsSystem = shellFileInfo.Attributes.HasFlag(FileAttributes.System);

                Size = shellFileInfo.Length;
            }
        }

        protected ShellItem ShellItem { get; set; }

        protected FileModel GetInstance(string path)
        {
            ShellItem shellItem = ShellItem.Open(path);
            return GetInstance(shellItem);
        }

        protected FileModel GetInstance(ShellItem shellItem)
        {
            FileModel fileModel = FileModelCache.GetOrAdd(shellItem, new FileModel());

            Initialize(shellItem);

            return fileModel;
        }

        protected ImageSource GetIcon(SHIL size)
        {
            IntPtr result = IntPtr.Zero;
            SHFILEINFO shFileInfo = new SHFILEINFO();

            string path = Windows;
            if (IsDirectory)
            {
                if (IsDrive || System.IO.File.Exists(Path.Combine(FullPath, "desktop.ini")))
                    path = FullPath;
            }
            else
            {
                if (Extension.OrdinalEquals(".url"))
                {
                    path = Extension;

                    foreach (string line in System.IO.File.ReadLines(FullPath))
                    {
                        if (line.OrdinalStartsWith("IconFile="))
                        {
                            string[] split = line.Split('=');
                            if (split.Length == 2)
                            {
                                path = split[1];
                                break;
                            }
                        }
                    }
                }
                else if (Extension.OrdinalEquals(".exe") ||
                    Extension.OrdinalEquals(".ico") ||
                    Extension.OrdinalEquals(".lnk") ||
                    Extension.OrdinalEquals(".cur"))
                    path = FullPath;
                else
                    path = Extension;
            }

            if (path.StartsWith(".") || path.StartsWith(@"\\"))
                result = SHGetFileInfo(path, FileAttributes.Normal, ref shFileInfo, FileInfoSize, SHGFI.SHGFI_SYSICONINDEX | SHGFI.SHGFI_USEFILEATTRIBUTES);
            else
                result = SHGetFileInfo(ShellItem.PIDL, FileAttributes.Normal, ref shFileInfo, FileInfoSize, SHGFI.SHGFI_SYSICONINDEX | SHGFI.SHGFI_PIDL);

            if (result == IntPtr.Zero)
                return null;

            if (SHGetImageList(size, ImageListId, out object image) != HRESULT.S_OK)
                return null;

            IImageList iImageList = image as IImageList;
            if (iImageList == null)
                return null;

            SafeHICON hIcon = iImageList.GetIcon(shFileInfo.iIcon, IMAGELISTDRAWFLAGS.ILD_IMAGE);

            ImageSource imageSource = hIcon.ToBitmapSource();
            imageSource.Freeze();
            DestroyIcon(hIcon);

            return imageSource;
        }

        protected static ConcurrentDictionary<ShellItem, FileModel> FileModelCache = new ConcurrentDictionary<ShellItem, FileModel>();        

        protected static ConcurrentDictionary<String, ImageSource> ImageSourceCache = new ConcurrentDictionary<String, ImageSource>();

        protected static AsyncKeyedLocker<string> ImageKeyLockProvider = new AsyncKeyedLocker<string>();

        protected static string Windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        private static readonly int FileInfoSize = Marshal.SizeOf(typeof(SHFILEINFO));

        private static readonly Guid ImageListId = typeof(IImageList).GUID;
    }
}
