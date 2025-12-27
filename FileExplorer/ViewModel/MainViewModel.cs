using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using FileExplorer.Core;
using FileExplorer.Messages;
using FileExplorer.Model;
using FileExplorer.Properties;
using static DevExpress.Xpf.Core.TabbedWindowDocumentUIService;

namespace FileExplorer.ViewModel
{
    public class MainViewModel
    {
        public static string LastClosedWindowSession { get; set; }

        public virtual IActiveWindowService ActiveWindowService { get { return null; } }

        public virtual IDocumentManagerService DocumentManagerService { get { return null; } }

        public MainViewModel()
        {
            Messenger.Default.Register(this, (CommandMessage message) =>
            {
                if (!ActiveWindowService.IsActive)
                    return;

                switch (message.MenuItem.Command)
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
                                App.CreateNewSingleTabWindow(file);
                        }
                        break;

                    case CommandType.OpenWithApplication:
                        OpenWithApplication(message.MenuItem.Application, message.Directory, message.Arguments, message.MenuItem.ConfirmBeforeRun);
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
                viewModel.CurrentFolder = FileModel.QuickAccess;
            else
                viewModel.CurrentFolder = FileModel.Computer;

            IDocument document = DocumentManagerService.CreateDocument("BrowserTabView", viewModel);
            document.DestroyOnClose = true;
            document.Show();
        }

        public async Task CreateFolderTabs(IEnumerable<string> folders = null)
        {
            List<string> validFolders = folders?.Where(x => FileModel.FolderExists(x)).ToList();
            if (validFolders?.Count > 0)
            {
                foreach (string folder in validFolders)
                {
                    FileModel selectedFolder = FileModel.Create(folder);
                    if (selectedFolder != null)
                    {
                        await selectedFolder.EnumerateParents();
                        CreateNewTab(selectedFolder);
                    }
                }
            }
            else
                CreateNewTab();
        }

        public void SaveSession()
        {
            if (Settings.Default.SaveLastSession)
            {
                var tabs = DocumentManagerService.Documents.OfType<TabbedWindowDocument>().Select(x => x.Content);
                LastClosedWindowSession = tabs.OfType<BrowserTabViewModel>().Select(x => x.CurrentFolder.FullPath).Join(";");
            }
        }

        private void OpenWithApplication(string application, string workingDirectory, string arguments, bool confirmBeforeRun)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(application, arguments);
            processStartInfo.WorkingDirectory = workingDirectory;
            processStartInfo.UseShellExecute = false;            

            if (DocumentManagerService.ActiveDocument?.Content is BrowserTabViewModel viewModel)
            {
                try
                {
                    if (!confirmBeforeRun)
                    {
                        Process.Start(processStartInfo);
                        return;
                    }

                    MessageViewModel messageViewModel = ViewModelSource.Create(() => new MessageViewModel());
                    messageViewModel.Title = Properties.Resources.Command;
                    messageViewModel.Icon = IconType.Question;
                    messageViewModel.Content = arguments;
                    messageViewModel.ContentReadOnly = false;
                    messageViewModel.Details = $"{Properties.Resources.Application}: {application}{Environment.NewLine}{Properties.Resources.WorkingDirectory}: {workingDirectory}";

                    MessageResult result = viewModel.DialogService.ShowDialog(MessageButton.OKCancel, Properties.Resources.RunCommand, "MessageView", messageViewModel);
                    if (result == MessageResult.OK)
                    {
                        processStartInfo.Arguments = messageViewModel.Content;
                        Process.Start(processStartInfo);
                    }
                }
                catch (Exception exception)
                {
                    Journal.WriteLog(exception);

                    MessageViewModel messageViewModel = ViewModelSource.Create(() => new MessageViewModel());
                    messageViewModel.Title = Properties.Resources.ErrorDetails;
                    messageViewModel.Icon = IconType.Exclamation;
                    messageViewModel.Content = exception.Message;
                    messageViewModel.Details = exception.StackTrace;

                    viewModel.DialogService.ShowDialog(MessageButton.OK, Properties.Resources.Error, "MessageView", messageViewModel);
                }
            }
        }
    }
}
