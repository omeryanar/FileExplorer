using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;
using static Vanara.PInvoke.User32;

namespace FileExplorer.Native
{
    public class OpenFolderDialog
    {
        public string InitialFolder { get; set; }

        public string DefaultFolder { get; set; }

        public string Folder { get; private set; }

        public bool ShowDialog()
        {
            return ShowDialogCore();
        }

        private bool ShowDialogCore()
        {
            IFileDialog dialog = (IFileDialog)new CFileOpenDialog();
            FILEOPENDIALOGOPTIONS options = dialog.GetOptions();
            options |= FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS | FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM | FILEOPENDIALOGOPTIONS.FOS_NOVALIDATE | FILEOPENDIALOGOPTIONS.FOS_NOTESTFILECREATE | FILEOPENDIALOGOPTIONS.FOS_DONTADDTORECENT;
            dialog.SetOptions(options);

            if (InitialFolder != null && PathFileExists(InitialFolder))
            {
                IShellItem folderItem = ShellUtil.GetShellItemForPath(InitialFolder);
                dialog.SetFolder(folderItem);
            }

            if (DefaultFolder != null && PathFileExists(InitialFolder))
            {
                IShellItem folderItem = ShellUtil.GetShellItemForPath(DefaultFolder);
                dialog.SetDefaultFolder(folderItem);
            }

            if (dialog.Show(GetActiveWindow()) == HRESULT.S_OK)
            {
                Folder = dialog.GetResult().GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
                return true;
            }

            return false;
        }
    }
}
