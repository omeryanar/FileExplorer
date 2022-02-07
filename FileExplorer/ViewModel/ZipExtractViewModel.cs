using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Compression;
using DevExpress.Mvvm;
using FileExplorer.Model;

namespace FileExplorer.ViewModel
{
    public class ZipExtractViewModel : ZipViewModel
    {
        public virtual FileModel FileModel { get; set; }

        public virtual double MaxProgress { get; protected set; }

        public virtual List<ZipItemModel> ZipItemModelList { get; protected set; }

        public virtual IDialogService DialogService { get { return null; } }

        public virtual IMessageBoxService MessageBoxService { get { return null; } }

        protected void OnFileModelChanged()
        {
            if (FileModel != null)
            {
                ZipItemModelList = new List<ZipItemModel>();

                using (ZipArchive archive = ZipArchive.Read(FileModel.FullPath))
                {
                    foreach (ZipItem zipItem in archive)
                    {
                        ZipItemModelList.Add(new ZipItemModel(zipItem));
                        MaxProgress += zipItem.UncompressedSize;

                        if (zipItem.EncryptionType != EncryptionType.None)
                            PasswordProtected = true;
                    }
                }
            }
        }

        protected override async Task RunCore(CancellationTokenSource cancellationToken)
        {
            using (ZipArchive archive = ZipArchive.Read(FileModel.FullPath))
            {
                foreach (ZipItem zipItem in archive)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    MessageResult skip = await Extract(zipItem);
                    if (skip == MessageResult.Cancel)
                        break;
                }
            }
        }

        private async Task<MessageResult> Extract(ZipItem zipItem, MessageResult skip = MessageResult.No)
        {
            if (skip != MessageResult.No)
                return skip;

            if (zipItem.EncryptionType != EncryptionType.None)
                zipItem.Password = Password;

            try
            {
                await Task.Run(() => zipItem.Extract(FilePath));
                Progress += zipItem.UncompressedSize;
            }
            catch (WrongPasswordException)
            {
                skip = MessageBoxService.ShowMessage(String.Format("{0}\r\n\r\n{1}", Properties.Resources.WrongPasswordMessage, zipItem.Name),
                    Properties.Resources.WrongPassword, MessageButton.YesNoCancel, MessageIcon.Error);

                if (skip == MessageResult.No)
                {
                    MessageResult result = DialogService.ShowDialog(MessageButton.OKCancel, Properties.Resources.WrongPassword, this);
                    skip = result == MessageResult.OK ? MessageResult.No : MessageResult.Cancel;
                }

                await Extract(zipItem, skip);
            }

            return skip;
        }
    }
}
