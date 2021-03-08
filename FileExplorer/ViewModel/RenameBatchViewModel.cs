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
                    Details = invalidFileNames.Select(x => x.Tag.ToString()).Join(Environment.NewLine)
                };
                DialogService.ShowDialog(MessageButton.OK, viewModel.Title, "MessageView", viewModel);

                e.Cancel = true;
                return;
            }

            List<FileModel> duplicateFileNames = FileModelList.GroupBy(x => x.Tag).SelectMany(y => y.Skip(1)).ToList();
            if (duplicateFileNames.Count > 0)
            {
                MessageViewModel viewModel = new MessageViewModel
                {
                    Icon = IconType.Exclamation,
                    Title = Properties.Resources.DuplicateFileName,
                    Content = Properties.Resources.DuplicateFileNameMessage,
                    Details = duplicateFileNames.Select(x => x.Tag.ToString()).Join(Environment.NewLine)
                };
                DialogService.ShowDialog(MessageButton.OK, viewModel.Title, "MessageView", viewModel);

                e.Cancel = true;
                return;
            }
        }
    }
}
