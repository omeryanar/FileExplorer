using System;
using System.IO;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors;
using FileExplorer.Native;

namespace FileExplorer.Editors
{
    public class OpenFolderDialogButton : FileDialogButton
    {
        public OpenFolderDialogButton()
        {
            IsTextEditable = false;
            AllowDefaultButton = false;

            Buttons.Add(new ButtonInfo()
            {
                GlyphKind = GlyphKind.Regular,
                Command = new DelegateCommand(() =>
                {
                    OpenFolderDialog openFolderDialog = new OpenFolderDialog();

                    if (!String.IsNullOrEmpty(Text) && Directory.Exists(Text))
                        openFolderDialog.InitialFolder = Text;

                    if (openFolderDialog.ShowDialog() == true)
                        EditValue = openFolderDialog.Folder;
                })
            });
        }
    }
}
