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
    public partial class PreviewControl : Grid
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

        private Dictionary<string, IPreviewExtension> ExtensionCache = new Dictionary<string, IPreviewExtension>();

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

        private void UpdateExtensionCache()
        {
            foreach (var extension in ExtensionManager.Instance.Extensions)
            {
                ExtensionMetadata extensionMetadata = App.Repository.Extensions.FirstOrDefault(x => x.AssemblyName == extension.Metadata.AssemblyName);

                if (extensionMetadata?.Disabled == true && ExtensionCache.ContainsKey(extensionMetadata.AssemblyName))
                {
                    IPreviewExtension previewExtension = ExtensionCache[extensionMetadata.AssemblyName];
                    if (previewExtension is UIElement element)
                        Children.Remove(element);

                    ExtensionCache.Remove(extensionMetadata.AssemblyName);
                }

                if (extensionMetadata?.Disabled == false && !ExtensionCache.ContainsKey(extensionMetadata.AssemblyName))
                {
                    IPreviewExtension previewExtension = extension.CreateExport().Value;
                    if (previewExtension is UIElement element)
                    {
                        element.Visibility = Visibility.Collapsed;
                        Children.Add(element);
                    }

                    ExtensionCache.Add(extension.Metadata.AssemblyName, previewExtension);
                }
            }
        }

        private static async void OnFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PreviewControl previewControl && previewControl.IsVisible && e.NewValue is FileModel fileModel)
            {
                if (previewControl.ActiveExtension is UIElement element)
                {
                    element.Visibility = Visibility.Collapsed;
                    await previewControl.ActiveExtension.UnloadFile();
                }

                if (fileModel.IsDirectory)
                    return;

                previewControl.UpdateExtensionCache();

                string fileType = fileModel.Extension.StartsWith(".") ? fileModel.Extension.Substring(1) : fileModel.Extension;
                ExtensionMetadata preferredExtension = App.Repository.Extensions.FirstOrDefault(x => !x.Disabled && x.Preferred?.OrdinalContains(fileType) == true);

                if (preferredExtension != null && previewControl.ExtensionCache.ContainsKey(preferredExtension.AssemblyName))
                    previewControl.ActiveExtension = previewControl.ExtensionCache[preferredExtension.AssemblyName];
                else
                    previewControl.ActiveExtension = previewControl.ExtensionCache.Values.FirstOrDefault(x => x.CanPreviewFile(fileModel.FullPath));

                if (previewControl.ActiveExtension is UIElement extension)
                {
                    try
                    {
                        previewControl.Loading = true;
                        await previewControl.ActiveExtension.PreviewFile(fileModel.FullPath);
                    }
                    finally
                    {
                        extension.Visibility = Visibility.Visible;
                        previewControl.Loading = false;
                    }
                }
            }
        }
    }
}
