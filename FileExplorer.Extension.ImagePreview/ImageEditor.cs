using System.Drawing;
using System.Drawing.Imaging;
using DevExpress.XtraEditors;

namespace FileExplorer.Extension.ImagePreview
{
    public class ImageEditor
    {
        public static readonly string[] SupportedFileTypes = new string[] { ".png", ".gif", ".bmp", ".jpg", ".jpeg", ".tif", ".tiff" };

        public static void ShowImageEditor(string path)
        {
            PictureEdit pictureEdit = new PictureEdit();

            using (Bitmap bitmap = new Bitmap(path))
            {
                ImageFormat format = GetImageFormat(bitmap);
                pictureEdit.Image = bitmap;

                if (pictureEdit.ShowImageEditorDialog() == System.Windows.Forms.DialogResult.OK)
                    pictureEdit.Image.Save(path, format);
            }
        }

        private static ImageFormat GetImageFormat(Image image)
        {
            if (image.RawFormat.Equals(ImageFormat.Jpeg))
                return ImageFormat.Jpeg;
            if (image.RawFormat.Equals(ImageFormat.Bmp))
                return ImageFormat.Bmp;
            if (image.RawFormat.Equals(ImageFormat.Png))
                return ImageFormat.Png;
            if (image.RawFormat.Equals(ImageFormat.Gif))
                return ImageFormat.Gif;
            if (image.RawFormat.Equals(ImageFormat.Icon))
                return ImageFormat.Icon;
            if (image.RawFormat.Equals(ImageFormat.MemoryBmp))
                return ImageFormat.MemoryBmp;
            if (image.RawFormat.Equals(ImageFormat.Tiff))
                return ImageFormat.Tiff;
            else
                return ImageFormat.Jpeg;
        }
    }
}
