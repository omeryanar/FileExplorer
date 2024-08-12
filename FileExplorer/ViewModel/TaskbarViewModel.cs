using System.Threading.Tasks;
using FileExplorer.Core;
using FileExplorer.Helpers;
using FileExplorer.Properties;

namespace FileExplorer.ViewModel
{
    public class TaskbarViewModel
    {
        public static string LastClosedWindowSession { get; set; }

        public async Task OpenDefault()
        {
            if (Settings.Default.SaveLastSession && LastClosedWindowSession != null)
                await App.CreateFolderTabs(LastClosedWindowSession.Split(";"));
            else
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
            App.SaveSessionAndShutdown();
        }
    }
}
