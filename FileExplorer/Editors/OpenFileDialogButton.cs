using System;
using System.IO;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors;
using Microsoft.Win32;

namespace FileExplorer.Editors
{
    public class OpenFileDialogButton : FileDialogButton
    {
        public OpenFileDialogButton()
        {
            IsTextEditable = false;
            AllowDefaultButton = false;

            Buttons.Add(new ButtonInfo()
            {
                GlyphKind = GlyphKind.Regular,
                Command = new DelegateCommand(() =>
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = FileFilter;

                    if (!String.IsNullOrEmpty(Text) && File.Exists(Text))
                    {
                        openFileDialog.FileName = Path.GetFileName(Text);
                        openFileDialog.InitialDirectory = Path.GetDirectoryName(Text);
                    }

                    if (openFileDialog.ShowDialog() == true)
                        EditValue = openFileDialog.FileName;
                })
            });
        }
    }
}
