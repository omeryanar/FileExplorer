using System;
using System.IO;
using DevExpress.Xpf.Editors;
using FileExplorer.Native;

namespace FileExplorer.Editors
{
    public class BrowseFolderPathEdit : BrowsePathEdit
    {
        public BrowseFolderPathEdit()
        {
            IsTextEditable = false;
            DialogType = DialogType.Folder;            

            DialogOpening += (s, e) =>
            {
                e.Cancel = true;
                e.Handled = true;

                OpenFolderDialog openFolderDialog = new OpenFolderDialog();
                if (!String.IsNullOrEmpty(Text) && Directory.Exists(Text))
                    openFolderDialog.InitialFolder = Text;

                if (openFolderDialog.ShowDialog() == true)
                    EditValue = openFolderDialog.Folder;
            };
        }
    }
}
