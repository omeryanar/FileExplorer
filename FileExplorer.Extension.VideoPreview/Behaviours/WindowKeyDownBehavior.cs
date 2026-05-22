using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Core;
using FileExplorer.Extension.VideoPreview.ViewModel;

namespace FileExplorer.Extension.VideoPreview.Behaviours
{
    public class WindowKeyDownBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;

            MainWindow = Window.GetWindow(AssociatedObject);
            MainWindow.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;

            MainWindow.PreviewKeyDown -= MainWindow_PreviewKeyDown;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
			if (MainWindow is DXTabbedWindow tabbedWindow)
            {
                if (IsVisualChildOf(tabbedWindow.TabControl.SelectedContainer) == false)
                    return;
            }

			if (AssociatedObject.DataContext is VideoPlayerViewModel videoPlayer)
            {
                switch (e.Key)
                {
                    case Key.Space:
                        videoPlayer.TogglePlayPauseCommand.Execute(null);
                        break;

                    case Key.Right:
                        videoPlayer.Next();
                        break;

                    case Key.Left:
                        videoPlayer.Previous();
                        break;

                    case Key.Up:
                        if (videoPlayer.Volume < 1)
                            videoPlayer.Volume += 0.1;
                        break;

                    case Key.Down:
                        if (videoPlayer.Volume > 0)
                            videoPlayer.Volume -= 0.1;
                        break;
                }
            }
        }

		private bool IsVisualChildOf(DependencyObject parent)
		{
            DependencyObject child = AssociatedObject;
			while (child != null)
			{
				if (child == parent)
                    return true;
				
                child = LogicalTreeHelper.GetParent(child);
			}
			return false;
		}

		private Window MainWindow;
    }
}
