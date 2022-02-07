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

            StateChanged += (s, e) =>
            {
                Settings.Default.WindowState = WindowState;
            };

            SizeChanged += (s, e) =>
            {
                Settings.Default.WindowWidth = Width;
                Settings.Default.WindowHeight = Height;
            };

            LocationChanged += (s, e) =>
            {
                Settings.Default.WindowTop = Top;
                Settings.Default.WindowLeft = Left;
            };
        }

        private void OnTabDragOver(object sender, DragEventArgs e)
        {
            if (sender is DXTabItem tabItem)
                tabItem.IsSelected = true;
        }

        private void OnNewTabbedWindow(object sender, TabControlNewTabbedWindowEventArgs e)
        {
            e.NewTabControl.NewTabbedWindow += OnNewTabbedWindow;

            e.NewWindow.SetBinding(TitleProperty, new Binding("SelectedItem.DataContext.Title")
            {
                Source = e.NewTabControl
            });

            e.NewWindow.SetBinding(IconProperty, new Binding("SelectedItem.DataContext.CurrentFolder.MediumIcon")
            {
                Source = e.NewTabControl
            });
        }
    }
}
