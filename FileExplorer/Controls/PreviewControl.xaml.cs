using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Mvvm;
using FileExplorer.Core;
using FileExplorer.Messages;
using FileExplorer.Model;
using FileExplorer.Extension;

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

        public static readonly string ExtensionDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PreviewExtensions");

        [ImportMany]
        public List<IPreviewExtension> Extensions { get; private set; }

        public PreviewControl()
        {
            InitializeComponent();            

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string fileName = new AssemblyName(e.Name).Name + ".dll";
                
                string[] files = Directory.GetFiles(ExtensionDirectory, fileName, SearchOption.AllDirectories);
                if (files?.Length > 0) 
                    return Assembly.LoadFile(files[0]);

                return null;
            };

            LoadExtensions();

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

        private void LoadExtensions()
        {
            if (Directory.Exists(ExtensionDirectory))
            {
                AggregateCatalog aggregateCatalog = new AggregateCatalog();
                DummyExtensionLoader dummyExtensionLoader = new DummyExtensionLoader();

                foreach (string assemblyFile in Directory.EnumerateFiles(ExtensionDirectory, "FileExplorer.Extension.*.dll", SearchOption.AllDirectories))
                {
                    try
                    {
                        AssemblyCatalog assemblyCatalog = new AssemblyCatalog(assemblyFile);

                        CompositionContainer compositionContainer = new CompositionContainer(assemblyCatalog);
                        compositionContainer.ComposeParts(dummyExtensionLoader);

                        aggregateCatalog.Catalogs.Add(assemblyCatalog);
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        Journal.WriteLog(ex);

                        if (ex.LoaderExceptions != null)
                        {
                            foreach (Exception exception in ex.LoaderExceptions)
                                Journal.WriteLog(exception);
                        }
                    }
                    catch (Exception ex)
                    {
                        Journal.WriteLog(ex);
                    }
                }

                CompositionContainer aggregateContainer = new CompositionContainer(aggregateCatalog);
                aggregateContainer.ComposeParts(this);

                if (Extensions != null)
                {
                    foreach (IPreviewExtension extension in Extensions)
                    {
                        if (extension is UIElement element)
                            Children.Add(element);
                    }
                }
            }
        }

        private async Task HideAllExtensions()
        {
            foreach (IPreviewExtension extension in Extensions)
            {
                await extension.UnloadFile();

                if (extension is UIElement element)
                    element.Visibility = Visibility.Collapsed;
            }
        }

        private static async void OnFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PreviewControl previewControl && e.NewValue is FileModel fileModel)
            {
                await previewControl.HideAllExtensions();
                
                if (fileModel.IsDirectory)
                    return;

                previewControl.ActiveExtension = previewControl.Extensions?.FirstOrDefault(x => x.CanPreviewFile(fileModel.FullPath));
                if (previewControl.ActiveExtension == null)
                    return;

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

        private class DummyExtensionLoader
        {
            [ImportMany]
            public List<IPreviewExtension> Extensions { get; set; }
        }
    }
}
