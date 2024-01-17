using System.Windows;
using System.Windows.Media;

namespace FileExplorer.Extension.ImagePreview
{
    public partial class ImageEditor 
    {
        public static readonly string[] SupportedFileTypes = new string[] { ".png", ".gif", ".bmp", ".jpg", ".jpeg", ".tif", ".tiff" };

        public byte[] Image => imageEdit.EditValue as byte[];

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(ImageEditor), new PropertyMetadata(null, OnImageSourceChanged));

        public ImageEditor()
        {
            InitializeComponent();
        }

        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageEditor window && e.NewValue is ImageSource image)
            {
                double screenWidth = SystemParameters.PrimaryScreenWidth - 200;
                double screenHeight = SystemParameters.PrimaryScreenHeight - 200;

                double imageWidth = image.Width;
                double imageHeight = image.Height;

                double aspectRatio = imageWidth / imageHeight;
                double widthRate = imageWidth / screenWidth;
                double heightRate = imageHeight / screenHeight;

                if (widthRate <= 1 && heightRate <= 1)
                {
                    window.Width = imageWidth;
                    window.Height = imageHeight;
                }
                else if (widthRate >= 1 && heightRate <= 1)
                {
                    window.Width = screenWidth;
                    window.Height = window.Width / aspectRatio;
                }
                else if (widthRate <= 1 && heightRate >= 1)
                {
                    window.Height = screenHeight;
                    window.Width = window.Height * aspectRatio;
                }
                else if (widthRate >= 1 && heightRate >= 1)
                {
                    if (widthRate > heightRate)
                    {
                        window.Width = screenWidth;
                        window.Height = window.Width / aspectRatio;
                    }
                    else
                    {
                        window.Height = screenHeight;
                        window.Width = window.Height * aspectRatio;
                    }
                }

                window.Top = (screenHeight - window.Height + 200) / 2;
                window.Left = (screenWidth - window.Width + 200) / 2;
            }
        }
    }
}
