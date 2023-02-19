using FileExplorer.Helpers;

namespace FileExplorer.ViewModel
{
    public class TaskbarViewModel
    {
        public void OpenDefault()
        {
            App.CreateNewWindow();
        }

        public void OpenComputer()
        {
            App.CreateNewWindow(FileSystemHelper.Computer);
        }

        public void OpenQuickAccess()
        {
            App.CreateNewWindow(FileSystemHelper.QuickAccess);
        }

        public void Exit()
        {
            App.Current.Shutdown();
        }
    }
}
