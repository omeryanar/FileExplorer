using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using FileExplorer.Core;
using FileExplorer.Helpers;
using FileExplorer.Messages;
using FileExplorer.Model;
using FileExplorer.Properties;

namespace FileExplorer.ViewModel
{
    public class MainViewModel
    {
        public virtual IActiveWindowService ActiveWindowService { get { return null; } }

        public virtual IDocumentManagerService DocumentManagerService { get { return null; } }

        public MainViewModel()
        {
            Messenger.Default.Register(this, (CommandMessage message) =>
            {
                if (!ActiveWindowService.IsActive)
                    return;

                switch (message.CommandType)
                {
                    case CommandType.Open:
                        foreach (FileModel file in message.Parameters)
                        {
                            if (DocumentManagerService.ActiveDocument?.Content is BrowserTabViewModel viewModel)
                                viewModel.OpenItem(file);
                        }
                        break;

                    case CommandType.OpenInNewTab:
                        foreach (FileModel file in message.Parameters)
                        {
                            if (file.IsDirectory)
                                CreateNewTab(file);
                        }
                        break;

                    case CommandType.OpenInNewWindow:
                        foreach (FileModel file in message.Parameters)
                        {
                            if (file.IsDirectory)
                                App.CreateNewVindow(file);
                        }
                        break;
                }
            });
        }

        public void CreateNewTab(FileModel selectedFolder = null)
        {
            BrowserTabViewModel viewModel = ViewModelSource.Create<BrowserTabViewModel>();
            viewModel.ParentViewModel = this;

            if (selectedFolder != null)
                viewModel.CurrentFolder = selectedFolder;
            else if (Settings.Default.FirstFolderToOpen == 0)
                viewModel.CurrentFolder = FileSystemHelper.QuickAccess;
            else
                viewModel.CurrentFolder = FileSystemHelper.Computer;

            IDocument document = DocumentManagerService.CreateDocument("BrowserTabView", viewModel);
            document.DestroyOnClose = true;
            document.Show();
        }
    }
}
