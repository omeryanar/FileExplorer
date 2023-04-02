using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Mvvm;
using FileExplorer.Core;
using FileExplorer.Extension;
using FileExplorer.Messages;
using FileExplorer.Model;
using FileExplorer.Persistence;

namespace FileExplorer.Controls
{
    public partial class PreviewControl : ContentControl
    {
        public FileModel File
        {
            get { return (FileModel)GetValue(FileProperty); }
            set { SetValue(FileProperty, value); }
        }
        public static readonly DependencyProperty FileProperty =
            DependencyProperty.Register(nameof(File), typeof(FileModel), typeof(PreviewControl), new PropertyMetadata(null, OnFileChanged));

        public IPreviewExtension ActiveExtension
        {
            get { return (IPreviewExtension)GetValue(ActiveExtensionProperty); }
            protected set { SetValue(ActiveExtensionPropertyKey, value); }
        }
        private static readonly DependencyPropertyKey ActiveExtensionPropertyKey = DependencyProperty.RegisterReadOnly
            (nameof(ActiveExtension), typeof(IPreviewExtension), typeof(PreviewControl), new PropertyMetadata(null));
        public static readonly DependencyProperty ActiveExtensionProperty = ActiveExtensionPropertyKey.DependencyProperty;

        public bool Loading
        {
            get { return (bool)GetValue(LoadingProperty); }
            protected set { SetValue(LoadingPropertyKey, value); }
        }
        private static readonly DependencyPropertyKey LoadingPropertyKey = DependencyProperty.RegisterReadOnly
            (nameof(Loading), typeof(bool), typeof(PreviewControl), new PropertyMetadata(null));
        public static readonly DependencyProperty LoadingProperty = LoadingPropertyKey.DependencyProperty;

        public PreviewControl()
        {
            InitializeComponent();

            Messenger.Default.Register(this, async (NotificationMessage message) =>
            {
                if (File == null || ActiveExtension == null)
                    return;

                if (message.Path.OrdinalEquals(File.FullPath))
                {
                    switch (message.NotificationType)
                    {
                        case NotificationType.Remove:
                            await ActiveExtension.UnloadFile();
                            break;

                        case NotificationType.Update:
                            await ActiveExtension.PreviewFile(message.Path);
                            break;

                        case NotificationType.Rename:
                            await ActiveExtension.UnloadFile();
                            await ActiveExtension.PreviewFile(message.NewPath);
                            break;
                    }
                }
            });
        }

        private async Task ActivateExtension(FileModel fileModel)
        {
            string fileType = fileModel.Extension.StartsWith(".") ? fileModel.Extension.Substring(1) : fileModel.Extension;
            
            ExtensionMetadata extensionMetadata = App.Repository.Extensions.FirstOrDefault(x => !x.Disabled && x.Preferred?.Split("|").Any(x => x.OrdinalEquals(fileType)) == true);
            if (extensionMetadata == null)
                extensionMetadata = App.Repository.Extensions.FirstOrDefault(x => !x.Disabled && x.SupportedFileTypes?.Split(",").Any(x => x.OrdinalEquals(fileType)) == true);

            if (extensionMetadata == null)
                return;

            if (!ExtensionCache.ContainsKey(extensionMetadata.DisplayName))
            {
                var exportFactory = ExtensionManager.Instance.Extensions.FirstOrDefault(x => x.Metadata.DisplayName == extensionMetadata.DisplayName);
                IPreviewExtension extension = exportFactory.CreateExport().Value;

                ExtensionCache.Add(extensionMetadata.DisplayName, extension);
            }

            ActiveExtension = ExtensionCache[extensionMetadata.DisplayName];
            if (ActiveExtension != null)
            {
                try
                {
                    Loading = true;
                    await ActiveExtension.PreviewFile(fileModel.FullPath);
                }
                finally
                {
                    Loading = false;
                }
            }
        }

        private static async void OnFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PreviewControl previewControl && previewControl.IsVisible && e.NewValue is FileModel fileModel)
            {
                if (previewControl.ActiveExtension != null)
                    await previewControl.ActiveExtension.UnloadFile();

                previewControl.ActiveExtension = null;

                if (!fileModel.IsDirectory)
                    await previewControl.ActivateExtension(fileModel);
            }
        }

        private Dictionary<string, IPreviewExtension> ExtensionCache = new Dictionary<string, IPreviewExtension>();
    }
}
