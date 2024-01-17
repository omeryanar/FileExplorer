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
    [ExportMetadata(nameof(IPreviewExtensionMetadata.SupportedFileTypes), "bmp|jpg|jpeg|ico|gif|png|svg|tif|tiff")]
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

        public double OffSetX
        {
            get { return (double)GetValue(OffSetXProperty); }
            set { SetValue(OffSetXProperty, value); }
        }
        public static readonly DependencyProperty OffSetXProperty =
            DependencyProperty.Register(nameof(OffSetX), typeof(double), typeof(ImageViewer));

        public double OffSetY
        {
            get { return (double)GetValue(OffSetYProperty); }
            set { SetValue(OffSetYProperty, value); }
        }
        public static readonly DependencyProperty OffSetYProperty =
            DependencyProperty.Register(nameof(OffSetY), typeof(double), typeof(ImageViewer));

        public ImageViewer()
        {
            InitializeComponent();
            EditCommand = new DelegateCommand(EditImage, CanEditImage);
        }

        public async Task PreviewFile(string filePath)
        {
            Reset();

            currentFilePath = filePath;
            currentFileExtension = Path.GetExtension(currentFilePath);

            DelegateCommand delegateCommand = EditCommand as DelegateCommand;
            delegateCommand.RaiseCanExecuteChanged();

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
            ImageEditor imageEditor = new ImageEditor();
            imageEditor.ImageSource = ImageSource;
            imageEditor.Title = Path.GetFileName(currentFilePath);

            imageEditor.ShowDialog();
            if (imageEditor.DialogButtonResult == MessageBoxResult.OK)
            {
                BitmapEncoder encoder = null;

                if (currentFileExtension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                    encoder = new PngBitmapEncoder();
                else if (currentFileExtension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
                    encoder = new GifBitmapEncoder();
                else if (currentFileExtension.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
                    encoder = new BmpBitmapEncoder();
                else if (currentFileExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || currentFileExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                    encoder = new JpegBitmapEncoder();
                else if (currentFileExtension.Equals(".tif", StringComparison.OrdinalIgnoreCase) || currentFileExtension.Equals(".tiff", StringComparison.OrdinalIgnoreCase))
                    encoder = new TiffBitmapEncoder();

                if (encoder != null)
                {
                    using (var fileStream = new FileStream(currentFilePath, FileMode.Open))
                    {
                        using(MemoryStream memoryStream = new MemoryStream(imageEditor.Image))
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = memoryStream;
                            bitmap.EndInit();

                            encoder.Frames.Add(BitmapFrame.Create(bitmap));
                            encoder.Save(fileStream);
                        }
                    }
                }
            }
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

        public void Reset()
        {
            ScaleFactor = 100;
            RotationAngle = 0;
            OffSetX = 0;
            OffSetY = 0;

            mouseUpPosition = new Point(0, 0);
        }

        private Stream imageStream;

        private string currentFilePath;

        private string currentFileExtension;

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.ReleaseMouseCapture();
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UIElement container = VisualTreeHelper.GetParent(image) as UIElement;
            mouseDownPosition = e.GetPosition(container);

            image.CaptureMouse();
            image.Cursor = Cursors.Hand;
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            image.ReleaseMouseCapture();
            image.Cursor = null;

            mouseUpPosition = new Point(OffSetX, OffSetY);
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (image.IsMouseCaptured)
            {
                UIElement container = VisualTreeHelper.GetParent(image) as UIElement;
                Point currentMousePosition = e.GetPosition(container);

                OffSetX = (currentMousePosition.X - mouseDownPosition.X + mouseUpPosition.X);
                OffSetY = (currentMousePosition.Y - mouseDownPosition.Y + mouseUpPosition.Y);
            }
        }

        private Point mouseDownPosition;
        private Point mouseUpPosition;
    }
}
