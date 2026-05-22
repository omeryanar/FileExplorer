using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using FileExplorer.Extension.VideoPreview.ViewModel;

namespace FileExplorer.Extension.VideoPreview.View
{
    [Export(typeof(IPreviewExtension))]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.DisplayName), "Video Viewer")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.SupportedFileTypes), "3g2|3gp|3gp2|3gpp|aac|ac3|aif|aifc|aiff|alac|amr|amv|ape|asf|avi|caf|dav|divx|dts|dv|evo|f4v|flac|flv|hdmov|it|m1v|m2p|m2t|m2ts|m2v|m4a|m4v|mka|mkv|mo3|mod|mov|mp2v|mp3|mp4|mp4v|mpc|mpe|mpeg|mpg|mpv2|mpv4|mtm|mts|mxf|ofr|ofs|oga|ogg|opus|pva|ra|rm|rmvb|s3m|shn|spx|tak|tp|tpr|ts|tta|umx|vob|webm|wmv|wv|xm")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.Version), "1.0")]
    public partial class VideoPlayer : UserControl, IPreviewExtension
    {
        public VideoPlayer()
        {
            InitializeComponent();
            InitializeDataContext();

            Messenger.Default.Register<SettingsChangedMessage>(this, async (message) =>
            {
                if (message.OldSettings.UseDirectShow != VideoPreviewSettings.Default.UseDirectShow)
                {
                    InitializeDataContext();

                    if (File.Exists(FilePath))
                        await PreviewFile(FilePath);
                }

                if (VideoPreviewSettings.Default.ShowThumbnails)
                {
                    if (message.OldSettings.ShowThumbnails == false ||
                        message.OldSettings.TimestampPosition != VideoPreviewSettings.Default.TimestampPosition ||
                        message.OldSettings.ThumbnailRows != VideoPreviewSettings.Default.ThumbnailRows ||
                        message.OldSettings.ThumbnailColumns != VideoPreviewSettings.Default.ThumbnailColumns)
                    {
                        if (DataContext is VideoPlayerViewModel videoPlayer && File.Exists(FilePath))
                            await videoPlayer.GenerateThumbnails(FilePath);
                    }
                }
            });
        }

        public async Task PreviewFile(string filePath)
        {
            FilePath = filePath;

            if (DataContext is VideoPlayerViewModel videoPlayer)
                await videoPlayer.PreviewFile(filePath);
        }

        public Task UnloadFile()
        {
            if (DataContext is VideoPlayerViewModel videoPlayer)
                videoPlayer.UnloadFile();

            return Task.CompletedTask;
        }

        private void InitializeDataContext()
        {
            if (VideoPreviewSettings.Default.UseDirectShow)
                DataContext = ViewModelSource.Create<DirectShowVideoPlayerViewModel>();
            else
                DataContext = ViewModelSource.Create<DefaultVideoPlayerViewModel>();
        }

        private void OnMediaFailed(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (DataContext is VideoPlayerViewModel videoPlayer)
                {
                    videoPlayer.Opened = false;
                    videoPlayer.ErrorMessage = Properties.Resources.FileError;
                }
            });
        }

        protected string FilePath;
    }
}
