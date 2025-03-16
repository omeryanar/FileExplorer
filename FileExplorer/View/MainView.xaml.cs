using System.Windows;
using System.Windows.Data;
using DevExpress.Xpf.Core;
using FileExplorer.Properties;

namespace FileExplorer.View
{
    public partial class MainView : DXTabbedWindow
    {
        public MainView()
        {
            if (Settings.Default.WindowState != WindowState.Minimized)
                WindowState = Settings.Default.WindowState;

            Top = Settings.Default.WindowTop;
            Left = Settings.Default.WindowLeft;
            Width = Settings.Default.WindowWidth;
            Height = Settings.Default.WindowHeight;

            InitializeComponent();

            Loaded += (s, e) =>
            {
                SetWindowIcon();
                SetWindowTitle();
            };

            Settings.Default.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Settings.Default.StaticTaskbarIcon))
                    SetWindowIcon();
                if (e.PropertyName == nameof(Settings.Default.StaticTaskbarTitle))
                    SetWindowTitle();
            };

            StateChanged += (s, e) =>
            {
                Settings.Default.WindowState = WindowState;
            };

            SizeChanged += (s, e) =>
            {
                Settings.Default.WindowWidth = Width > 0 ? Width : MinWidth;
                Settings.Default.WindowHeight = Height > 0 ? Height : MinHeight;
            };

            LocationChanged += (s, e) =>
            {
                Settings.Default.WindowTop = Top > 0 ? Top : 0;
                Settings.Default.WindowLeft = Left > 0 ? Left : 0;
            };
        }

        private void SetWindowIcon()
        {
            if (Settings.Default.StaticTaskbarIcon)
                SetBinding(IconProperty, new Binding());
            else
                SetBinding(IconProperty, new Binding("SelectedItem.DataContext.CurrentFolder.MediumIcon") { Source = Content });
        }

        private void SetWindowTitle()
        {
            if (Settings.Default.StaticTaskbarTitle)
                SetBinding(TitleProperty, new Binding() { Source = "File Explorer" });
            else
                SetBinding(TitleProperty, new Binding("SelectedItem.DataContext.Title") { Source = Content });
        }

        private void OnTabDragOver(object sender, DragEventArgs e)
        {
            if (sender is DXTabItem tabItem)
                tabItem.IsSelected = true;
        }
    }
}
