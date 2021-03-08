using System.Windows;
using DevExpress.Xpf.Editors;

namespace FileExplorer.Editors
{
    public abstract class FileDialogButton : ButtonEdit
    {
        public string FileFilter
        {
            get { return (string)GetValue(FileFilterProperty); }
            set { SetValue(FileFilterProperty, value); }
        }
        public static readonly DependencyProperty FileFilterProperty = DependencyProperty.Register(nameof(FileFilter), typeof(string), typeof(FileDialogButton));
    }
}
