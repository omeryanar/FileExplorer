using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using DevExpress.Mvvm;
using FileExplorer.Core;
using FileExplorer.Model;

namespace FileExplorer.ViewModel
{
    public class RenameBatchViewModel
    {
        public virtual List<FileModel> FileModelList { get; set; }

        public List<UICommand> UICommandList { get; private set; }

        public virtual IDialogService DialogService { get { return null; } }

        public RenameBatchViewModel()
        {
            UICommandList = new List<UICommand>
            {
                new UICommand
                {
                    Caption = Properties.Resources.OK,
                    Id = MessageBoxResult.OK,
                    IsDefault = true,
                    IsCancel = false,
                    Command = new DelegateCommand<CancelEventArgs>(Accept)
                },
                new UICommand
                {
                    Caption = Properties.Resources.Cancel,
                    Id = MessageBoxResult.Cancel,
                    IsDefault = false,
                    IsCancel = true
                }
            };
        }

        private void Accept(CancelEventArgs e)
        {
            List<FileModel> invalidFileNames = FileModelList.Where(x => x.Tag == null || x.Tag.ToString().ContainsInvalidFileNameCharacters()).ToList();
            if (invalidFileNames.Count > 0)
            {
                MessageViewModel viewModel = new MessageViewModel
                {
                    Icon = IconType.Exclamation,
                    Title = Properties.Resources.InvalidFileName,
                    Content = Properties.Resources.InvalidFileNameMessage,
                    Details = invalidFileNames.Select(x => $"'{x}' => '{x.Tag}'").Join(Environment.NewLine)
                };
                DialogService.ShowDialog(MessageButton.OK, viewModel.Title, "MessageView", viewModel);

                e.Cancel = true;
                return;
            }

            var duplicates = FileModelList.GroupBy(x => (x.Tag, x.Extension), x => x.FullName).Where(x => x.Count() > 1);            
            if (duplicates.Count() > 0)
            {
                IEnumerable<string> duplicateFileNames = duplicates.Select(x => $"{x.Join(Environment.NewLine)} \r\n\r\n └{new string('─', x.Max(y => y.Length))}> {x.Key.Tag}{x.Key.Extension}\r\n");

                MessageViewModel viewModel = new MessageViewModel
                {
                    Icon = IconType.Exclamation,
                    Title = Properties.Resources.DuplicateFileName,
                    Content = Properties.Resources.DuplicateFileNameMessage,
                    Details = duplicateFileNames.Join(Environment.NewLine)
                };
                DialogService.ShowDialog(MessageButton.OK, viewModel.Title, "MessageView", viewModel);

                e.Cancel = true;
                return;
            }
        }
    }
}
