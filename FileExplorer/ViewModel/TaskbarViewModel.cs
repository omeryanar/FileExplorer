using FileExplorer.Model;

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
            App.CreateNewSingleTabWindow(FileModel.Computer);
        }

        public void OpenQuickAccess()
        {
            App.CreateNewSingleTabWindow(FileModel.QuickAccess);
        }

        public void Exit()
        {
            App.SaveSessionAndShutdown();
        }
    }
}
