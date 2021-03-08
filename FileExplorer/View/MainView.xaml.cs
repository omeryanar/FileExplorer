using System.Windows;
using System.Windows.Data;
using DevExpress.Xpf.Core;

namespace FileExplorer.View
{
    public partial class MainView : DXTabbedWindow
    {
        public MainView()
        {
            InitializeComponent();
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
