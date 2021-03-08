using System.ComponentModel.DataAnnotations;
using DevExpress.Mvvm.DataAnnotations;

namespace FileExplorer.Core
{
    public enum IconSize
    {
        Small,
        Medium,        
        Large,
        ExtraLarge
    }

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
        [Image("pack://application:,,,/FileExplorer;component/Assets/Layout/Details.png")]
        [Display(Name = "Details", ResourceType = typeof(Properties.Resources))]
        Details,

        [Image("pack://application:,,,/FileExplorer;component/Assets/Layout/List.png")]
        [Display(Name = "List", ResourceType = typeof(Properties.Resources))]
        List,

        [Image("pack://application:,,,/FileExplorer;component/Assets/Layout/SmallIcons.png")]
        [Display(Name = "SmallIcons", ResourceType = typeof(Properties.Resources))]
        SmallIcons,

        [Image("pack://application:,,,/FileExplorer;component/Assets/Layout/MediumIcons.png")]
        [Display(Name = "MediumIcons", ResourceType = typeof(Properties.Resources))]
        MediumIcons,

        [Image("pack://application:,,,/FileExplorer;component/Assets/Layout/LargeIcons.png")]
        [Display(Name = "LargeIcons", ResourceType = typeof(Properties.Resources))]
        LargeIcons,

        [Image("pack://application:,,,/FileExplorer;component/Assets/Layout/ExtraLargeIcons.png")]
        [Display(Name = "ExtraLargeIcons", ResourceType = typeof(Properties.Resources))]
        ExtraLargeIcons
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

    public enum NotificationType
    {
        Add,
        Remove,
        Update,
        Rename
    }
}
