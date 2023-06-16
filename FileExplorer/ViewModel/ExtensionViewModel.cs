using DevExpress.Mvvm;
using FileExplorer.Core;
using FileExplorer.Persistence;

namespace FileExplorer.ViewModel
{
    public class ExtensionViewModel
    {
        public virtual IDialogService DialogService { get { return null; } }

        public virtual PersistentCollection<ExtensionMetadata> Extensions { get; set; }

        public void ShowErrorDetails(string error)
        {
            MessageViewModel viewModel = new MessageViewModel
            {
                Icon = IconType.Exclamation,
                Title = Properties.Resources.Error,
                Content = Properties.Resources.ExtensionError,
                Details = error
            };
            DialogService.ShowDialog(MessageButton.OK, viewModel.Title, "MessageView", viewModel);
        }
    }
}
