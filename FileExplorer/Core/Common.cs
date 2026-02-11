using System.ComponentModel.DataAnnotations;
using DevExpress.Mvvm.DataAnnotations;

namespace FileExplorer.Core
{
    public enum IconType
    {
        Question,
        Information,
        Exclamation
    }

    public enum ItemTypeFilter
    {
        [Display(Name = "None", ResourceType = typeof(Properties.Resources))]
        None,

        [Display(Name = "File", ResourceType = typeof(Properties.Resources))]
        File,

        [Display(Name = "Folder", ResourceType = typeof(Properties.Resources))]
        Folder,
    }

    public enum SelectionFilter
    {
        [Display(Name = "None", ResourceType = typeof(Properties.Resources))]
        None,

        [Display(Name = "Single", ResourceType = typeof(Properties.Resources))]
        Single,

        [Display(Name = "Multiple", ResourceType = typeof(Properties.Resources))]
        Multiple
    }

    public enum Layout
    {
        [Image("pack://application:,,,/FileExplorer;component/Assets/ICO/Details.ico")]
        [Display(Name = "Details", ResourceType = typeof(Properties.Resources))]
        Details,

        [Image("pack://application:,,,/FileExplorer;component/Assets/ICO/List.ico")]
        [Display(Name = "List", ResourceType = typeof(Properties.Resources))]
        List,

        [Image("pack://application:,,,/FileExplorer;component/Assets/ICO/SmallIcons.ico")]
        [Display(Name = "SmallIcons", ResourceType = typeof(Properties.Resources))]
        SmallIcons,

        [Image("pack://application:,,,/FileExplorer;component/Assets/ICO/MediumIcons.ico")]
        [Display(Name = "MediumIcons", ResourceType = typeof(Properties.Resources))]
        MediumIcons,

        [Image("pack://application:,,,/FileExplorer;component/Assets/ICO/LargeIcons.ico")]
        [Display(Name = "LargeIcons", ResourceType = typeof(Properties.Resources))]
        LargeIcons,

        [Image("pack://application:,,,/FileExplorer;component/Assets/ICO/ExtraLargeIcons.ico")]
        [Display(Name = "ExtraLargeIcons", ResourceType = typeof(Properties.Resources))]
        ExtraLargeIcons,

        [Image("pack://application:,,,/FileExplorer;component/Assets/ICO/Content.ico")]
        [Display(Name = "Content", ResourceType = typeof(Properties.Resources))]
        Content,

        [Image("pack://application:,,,/FileExplorer;component/Assets/ICO/Thumbnails.ico")]
        [Display(Name = "Thumbnails", ResourceType = typeof(Properties.Resources))]
        Thumbnails
    }

    public enum CommandType
    {
        [Display(Name = "Open", ResourceType = typeof(Properties.Resources))]
        Open,

        [Display(Name = "OpenInNewTab", ResourceType = typeof(Properties.Resources))]
        OpenInNewTab,

        [Display(Name = "OpenInNewWindow", ResourceType = typeof(Properties.Resources))]
        OpenInNewWindow,

        [Display(Name = "OpenWithApplication", ResourceType = typeof(Properties.Resources))]
        OpenWithApplication
    }

    public enum ParameterType
    {
        [Display(Name = "Name", ResourceType = typeof(Properties.Resources))]
        Name,

        [Display(Name = "Path", ResourceType = typeof(Properties.Resources))]
        Path,

        [Display(Name = "Expression", ResourceType = typeof(Properties.Resources))]
        Expression
    }

    public enum ColumnType
    {
        [Display(Name = "General", ResourceType = typeof(Properties.Resources))]
        General,

        [Display(Name = "Text", ResourceType = typeof(Properties.Resources))]
        Text,

        [Display(Name = "Integer", ResourceType = typeof(Properties.Resources))]
        Integer,

        [Display(Name = "Decimal", ResourceType = typeof(Properties.Resources))]
        Decimal,

        [Display(Name = "DateTime", ResourceType = typeof(Properties.Resources))]
        DateTime,

        [Display(Name = "Boolean", ResourceType = typeof(Properties.Resources))]
        Boolean,
    }

    public enum ThumbnailMode
    {
        [Image("pack://application:,,,/Assets/Thumbnail/Crop.svg")]
        [Display(Name = "Crop", Description = "CropDescription", ResourceType = typeof(Properties.Resources))]
        Crop,

        [Image("pack://application:,,,/Assets/Thumbnail/Contain.svg")]
        [Display(Name = "Contain", Description = "ContainDescription", ResourceType = typeof(Properties.Resources))]
        Contain,

        [Image("pack://application:,,,/Assets/Thumbnail/Stretch.svg")]
        [Display(Name = "Stretch", Description = "StretchDescription", ResourceType = typeof(Properties.Resources))]
        Stretch
    }

    public enum ThumbnailAnchor
    {
        [Image("pack://application:,,,/Assets/Thumbnail/Center.svg")]
        [Display(Name = "Center", ResourceType = typeof(Properties.Resources))]
        Center = 0,

        [Image("pack://application:,,,/Assets/Thumbnail/Top.svg")]
        [Display(Name = "Top", ResourceType = typeof(Properties.Resources))]
        Top = 1,

        [Image("pack://application:,,,/Assets/Thumbnail/Bottom.svg")]
        [Display(Name = "Bottom", ResourceType = typeof(Properties.Resources))]
        Bottom = 2,

        [Image("pack://application:,,,/Assets/Thumbnail/Left.svg")]
        [Display(Name = "Left", ResourceType = typeof(Properties.Resources))]
        Left = 4,

        [Image("pack://application:,,,/Assets/Thumbnail/Right.svg")]
        [Display(Name = "Right", ResourceType = typeof(Properties.Resources))]
        Right = 8,

        [Image("pack://application:,,,/Assets/Thumbnail/TopLeft.svg")]
        [Display(Name = "TopLeft", ResourceType = typeof(Properties.Resources))]
        TopLeft = 5,

        [Image("pack://application:,,,/Assets/Thumbnail/TopRight.svg")]
        [Display(Name = "TopRight", ResourceType = typeof(Properties.Resources))]
        TopRight = 9,

        [Image("pack://application:,,,/Assets/Thumbnail/BottomLeft.svg")]
        [Display(Name = "BottomLeft", ResourceType = typeof(Properties.Resources))]
        BottomLeft = 6,

        [Image("pack://application:,,,/Assets/Thumbnail/BottomRight.svg")]
        [Display(Name = "BottomRight", ResourceType = typeof(Properties.Resources))]
        BottomRight = 10,
    }

    public enum NotificationType
    {
        Add,
        Remove,
        Update,
        Rename,
        Recycle,
        Restore
    }

    public enum UpdateStatus
    {
        UpToDate,
        ReadyToDownload,
        DownloadInProgress,
        ReadyToInstall,
        Cancelled,
        Failed
    }
}
