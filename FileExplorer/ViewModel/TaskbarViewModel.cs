using FileExplorer.Helpers;

namespace FileExplorer.ViewModel
{
    public class TaskbarViewModel
    {
        public void OpenDefault()
        {
            App.ParseArgumentsAndRun(new string[0]);
        }

        public void OpenComputer()
        {
            App.CreateNewSingleTabWindow(FileSystemHelper.Computer);
        }

        public void OpenQuickAccess()
        {
            App.CreateNewSingleTabWindow(FileSystemHelper.QuickAccess);
        }

        public void Exit()
        {
            App.SaveSessionAndShutdown();
        }
    }
}
