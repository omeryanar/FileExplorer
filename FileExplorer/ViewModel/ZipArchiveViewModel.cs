using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Compression;
using DevExpress.Mvvm;
using FileExplorer.Model;

namespace FileExplorer.ViewModel
{
    public class ZipArchiveViewModel : ZipViewModel
    {
        public virtual List<FileModel> FileModelList { get; set; }

        public virtual IDispatcherService DispatcherService { get { return null; } }

        protected override async Task RunCore(CancellationTokenSource cancellationToken)
        {
            await Task.Run(() =>
            {
                using (ZipArchive archive = new ZipArchive())
                {
                    archive.Progress += (s, e) =>
                    {
                        DispatcherService.BeginInvoke(() => Progress = Math.Round(e.Progress * 100));

                        if (cancellationToken.IsCancellationRequested)
                            e.CanContinue = false;
                    };

                    if (!String.IsNullOrEmpty(Password))
                    {
                        archive.Password = Password;
                        archive.EncryptionType = EncryptionType.Aes256;
                    }

                    foreach (FileModel fileModel in FileModelList)
                    {
                        if (fileModel.IsDirectory)
                            archive.AddDirectory(fileModel.FullPath, fileModel.Name);
                        else
                            archive.AddFile(fileModel.FullPath, "/");
                    }

                    archive.Save(FilePath);
                }
            });
        }
    }
}
