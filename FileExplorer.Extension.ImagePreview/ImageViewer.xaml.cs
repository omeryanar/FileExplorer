using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Mvvm;
using DevExpress.Utils.Svg;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;

namespace FileExplorer.Extension.ImagePreview
{
    [Export(typeof(IPreviewExtension))]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.DisplayName), "Image Viewer")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.SupportedFileTypes), "bmp,jpg,jpeg,ico,gif,png,svg,tif,tiff")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.Version), "1.0")]
    public partial class ImageViewer : UserControl, IPreviewExtension
    {
        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            protected set { SetValue(ImageSourcePropertyKey, value); }
        }
        private static readonly DependencyPropertyKey ImageSourcePropertyKey = DependencyProperty.RegisterReadOnly
            (nameof(ImageSource), typeof(ImageSource), typeof(ImageViewer), new PropertyMetadata(null));
        public static readonly DependencyProperty ImageSourceProperty = ImageSourcePropertyKey.DependencyProperty;

        public ICommand EditCommand
        {
            get { return (ICommand)GetValue(EditCommandProperty); }
            protected set { SetValue(EditCommandPropertyKey, value); }
        }
        private static readonly DependencyPropertyKey EditCommandPropertyKey = DependencyProperty.RegisterReadOnly
            (nameof(EditCommand), typeof(ICommand), typeof(ImageViewer), new PropertyMetadata(null));
        public static readonly DependencyProperty EditCommandProperty = EditCommandPropertyKey.DependencyProperty;

        public int RotationAngle
        {
            get { return (int)GetValue(RotationAngleProperty); }
            set { SetValue(RotationAngleProperty, value); }
        }
        public static readonly DependencyProperty RotationAngleProperty =
            DependencyProperty.Register(nameof(RotationAngle), typeof(int), typeof(ImageViewer));

        public double ScaleFactor
        {
            get { return (double)GetValue(ScaleFactorProperty); }
            set { SetValue(ScaleFactorProperty, value); }
        }
        public static readonly DependencyProperty ScaleFactorProperty =
            DependencyProperty.Register(nameof(ScaleFactor), typeof(double), typeof(ImageViewer), new PropertyMetadata(100.0));

        public ImageViewer()
        {
            InitializeComponent();
            EditCommand = new DelegateCommand(EditImage, CanEditImage);
        }

        public async Task PreviewFile(string filePath)
        {
            ScaleFactor = 100;
            RotationAngle = 0;

            currentFilePath = filePath;
            currentFileExtension = Path.GetExtension(currentFilePath);

            using (FileStream fileStream = File.Open(filePath, FileMode.Open))
            {
                imageStream?.Close();
                imageStream = new MemoryStream();
                await fileStream.CopyToAsync(imageStream);
                imageStream.Seek(0, SeekOrigin.Begin);

                if (filePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    double scale = 1.0;
                    SvgImage image = SvgImageHelper.CreateImage(imageStream);
                    if (image.Width > 0)
                        scale = Math.Floor(400 / image.Width);

                    ImageSource = WpfSvgRenderer.CreateImageSource(image, scale, null, null, true);
                }
                else
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.DecodePixelWidth = 960;
                    bitmapImage.BeginInit();
                    bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = imageStream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    ImageSource = bitmapImage;
                }
            }
        }

        public Task UnloadFile()
        {
            ImageSource = null;
            imageStream?.Close();

            currentFilePath = null;
            currentFileExtension = null;

            return Task.CompletedTask;
        }

        public bool CanEditImage()
        {
            if (String.IsNullOrEmpty(currentFileExtension))
                return false;

            return ImageEditor.SupportedFileTypes.Any(x => x.Equals(currentFileExtension, StringComparison.OrdinalIgnoreCase));
        }

        public void EditImage()
        {
            ImageEditor.ShowImageEditor(currentFilePath);
        }

        public void RotateLeft()
        {
            if (RotationAngle > -270)
                RotationAngle -= 90;
            else
                RotationAngle = 0;
        }

        public void RotateRight()
        {
            if (RotationAngle < 270)
                RotationAngle += 90;
            else
                RotationAngle = 0;
        }

        private Stream imageStream;

        private string currentFilePath;

        private string currentFileExtension;
    }
}
