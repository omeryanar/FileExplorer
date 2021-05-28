using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using FileExplorer.Messages;
using FileExplorer.Model;

namespace FileExplorer.Core
{
    public static class ImagePreview
    {
        #region Source

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached("Source", typeof(object), typeof(ImagePreview), new PropertyMetadata(null, SourcePropertyChanged));

        public static object GetSource(DependencyObject obj)
        {
            return obj.GetValue(SourceProperty);
        }

        public static void SetSource(DependencyObject obj, object value)
        {
            obj.SetValue(SourceProperty, value);
        }

        private static async void SourcePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is FileModel oldFileModel && PreviewedFilePaths.Contains(oldFileModel.FullPath))
                PreviewedFilePaths.Remove(oldFileModel.FullPath);

            if (obj is Image image)
            {
                if (e.NewValue is FileModel fileModel && !fileModel.IsDirectory && SupportedExtensions.Any(x => x.OrdinalEquals(fileModel.Extension)))
                {
                    try
                    {
                        image.Tag = fileModel.FullPath;
                        PreviewedFilePaths.Add(fileModel.FullPath);

                        int hashCode = image.GetHashCode();
                        if (!ImageReferences.ContainsKey(hashCode))
                        {
                            ImageReferences.Add(hashCode, new WeakReference<Image>(image));
                            image.Unloaded += (x, y) => { PreviewedFilePaths.Remove(image.Tag?.ToString()); };
                        }

                        if (fileModel.Size > 1048576)
                            image.Visibility = Visibility.Collapsed;

                        image.Tag = fileModel.FullPath;
                        image.Source = await ReadImageAsync(fileModel.FullPath);
                    }
                    finally
                    {
                        image.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    image.Tag = null;
                    image.Source = null;
                }
            }
        }

        #endregion

        static ImagePreview()
        {
            Messenger.Default.Register(Application.Current, async (NotificationMessage message) =>
            {
                if (!PreviewedFilePaths.Contains(message.Path))
                    return;

                List<int> deadReferences = new List<int>();
                foreach (KeyValuePair<int, WeakReference<Image>> keyValuePair in ImageReferences)
                {
                    if (keyValuePair.Value.TryGetTarget(out Image image))
                    {
                        if (message.Path.OrdinalEquals(image.Tag?.ToString()))
                        {
                            switch (message.NotificationType)
                            {
                                case NotificationType.Remove:
                                    image.Source = null;
                                    break;

                                case NotificationType.Update:
                                    image.Source = await ReadImageAsync(message.Path);
                                    break;

                                case NotificationType.Rename:
                                    image.Tag = message.NewPath;
                                    break;
                            }
                        }
                    }
                    else
                        deadReferences.Add(keyValuePair.Key);
                }

                if (deadReferences.Count > 0)
                    deadReferences.ForEach(x => ImageReferences.Remove(x));
            });
        }

        private static async Task<ImageSource> ReadImageAsync(string path)
        {
            ImageSource imageSource = null;

            await Task.Run(() =>
            {
                try
                {
                    if (path.OrdinalEndsWith(".svg"))
                    {
                        var image = SvgImageHelper.CreateImage(new Uri(path));
                        var scale = Math.Floor(400 / image.Width);

                        imageSource = WpfSvgRenderer.CreateImageSource(image, scale, null, null, true);
                    }
                    else
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.DecodePixelWidth = 960;
                        bitmapImage.BeginInit();
                        bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.IgnoreImageCache;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.UriSource = new Uri(path, UriKind.Absolute);
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();

                        imageSource = bitmapImage;
                    }
                }
                catch
                {
                    imageSource = null;
                }
            });

            return imageSource;
        }

        private static readonly List<string> PreviewedFilePaths = new List<string>();

        private static readonly Dictionary<int, WeakReference<Image>> ImageReferences = new Dictionary<int, WeakReference<Image>>(); 

        private static readonly string[] SupportedExtensions = new string[] { ".png", ".gif", ".ico", ".bmp", ".jpg", ".jpeg", ".tif", ".tiff", ".svg" };
    }
}