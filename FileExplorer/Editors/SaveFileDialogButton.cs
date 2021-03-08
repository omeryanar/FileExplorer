using System;
using System.IO;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors;
using Microsoft.Win32;

namespace FileExplorer.Editors
{
    public class SaveFileDialogButton : FileDialogButton
    {
        public SaveFileDialogButton()
        {
            IsTextEditable = false;
            AllowDefaultButton = false;

            Buttons.Add(new ButtonInfo()
            {
                GlyphKind = GlyphKind.Regular,
                Command = new DelegateCommand(() =>
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = FileFilter;

                    if (!String.IsNullOrEmpty(Text))
                    {
                        saveFileDialog.FileName = Text;
                        saveFileDialog.InitialDirectory = Path.GetDirectoryName(Text);
                    }

                    if (saveFileDialog.ShowDialog() == true)
                        EditValue = saveFileDialog.FileName;
                })
            });
        }
    }
}
