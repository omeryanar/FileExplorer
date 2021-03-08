using FileExplorer.Helpers;

namespace FileExplorer.ViewModel
{
    public class TaskbarViewModel
    {
        public void OpenDefault()
        {
            App.CreateNewVindow();
        }

        public void OpenComputer()
        {
            App.CreateNewVindow(FileSystemHelper.Computer);
        }

        public void OpenQuickAccess()
        {
            App.CreateNewVindow(FileSystemHelper.QuickAccess);
        }

        public void Exit()
        {
            App.Current.Shutdown();
        }
    }
}
