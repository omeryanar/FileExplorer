using System;
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

        private ExtensionManager ExtensionManager;

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

            ExtensionManager = ExtensionManager.Instance;
        }

        private async Task HideAllExtensions()
        {
            foreach (UIElement element in Children)
            {
                if (element is IPreviewExtension extension)
                {
                    await extension.UnloadFile();
                    element.Visibility = Visibility.Collapsed;
                }
            }
        }

        private static async void OnFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PreviewControl previewControl && e.NewValue is FileModel fileModel)
            {
                UpdateExtensionCache(previewControl);
                await previewControl.HideAllExtensions();
                
                if (fileModel.IsDirectory)
                    return;

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

        private static void UpdateExtensionCache(PreviewControl previewControl)
        {
            foreach (var extension in previewControl.ExtensionManager.Extensions)
            {
                ExtensionMetadata extensionMetadata = App.Repository.Extensions.FirstOrDefault(x => x.AssemblyName == extension.Metadata.AssemblyName);

                if (extensionMetadata?.Disabled == true && previewControl.ExtensionCache.ContainsKey(extensionMetadata.AssemblyName))
                {
                    IPreviewExtension previewExtension = previewControl.ExtensionCache[extensionMetadata.AssemblyName];
                    if (previewExtension is UIElement element)
                        previewControl.Children.Remove(element);

                    previewControl.ExtensionCache.Remove(extensionMetadata.AssemblyName);
                }

                if (extensionMetadata?.Disabled == false && !previewControl.ExtensionCache.ContainsKey(extensionMetadata.AssemblyName))
                {
                    IPreviewExtension previewExtension = extension.CreateExport().Value;
                    if (previewExtension is UIElement element)
                        previewControl.Children.Add(element);

                    previewControl.ExtensionCache.Add(extension.Metadata.AssemblyName, previewExtension);
                }
            }
        }
    }
}
