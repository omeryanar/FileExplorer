using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using FileExplorer.Properties;
using PhotoSauce.MagicScaler;

namespace FileExplorer.Helpers
{
    public class ThumbnailHelper
    {
        public static async Task<BitmapImage> GetThumbnailImage(string path)
        {
            BitmapImage bitmapImage = null;

            await Task.Run(() =>
            {
                try
                {
                    ProcessImageSettings settings = new ProcessImageSettings();
                    settings.Anchor = (CropAnchor)Settings.Default.ThumbnailAnchor;
                    settings.ResizeMode = (CropScaleMode)Settings.Default.ThumbnailMode;
                    settings.Width = Settings.Default.ThumbnailHeight;
                    settings.Height = Settings.Default.ThumbnailHeight;

                    System.IO.MemoryStream stream = new System.IO.MemoryStream();
                    MagicImageProcessor.ProcessImage(path, stream, settings);
                    stream.Position = 0;

                    bitmapImage = new BitmapImage
                    {
                        CreateOptions = BitmapCreateOptions.IgnoreColorProfile,
                        CacheOption = BitmapCacheOption.OnLoad
                    };

                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }
                catch
                {
                    bitmapImage = null;
                }
            });

            return bitmapImage;
        }

        public static bool ThumbnailExists(string path)
        {
            return Regex.Match(path, SupportedImageFormats, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Success;
        }

        const string SupportedImageFormats = @"^.+\.(?:(?:avif)|(?:bmp)|(?:dip)|(?:gif)|(?:heic)|(?:heif)|(?:jfif)|(?:jpe)|(?:jpe?g)|(?:jxr)|(?:png)|(?:rle)|(?:tiff?)|(?:wdp)|(?:webp))$";
    }
}
