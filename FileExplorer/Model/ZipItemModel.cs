using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Compression;
using FileExplorer.Core;
using FileExplorer.Helpers;

namespace FileExplorer.Model
{
    public class ZipItemModel
    {
        public string Name { get; }

        public string Path { get; }

        public string Parent { get; }

        public ImageSource Icon { get; }

        public ZipItem ZipItem { get; }

        public long CompressedSize { get { return ZipItem.CompressedSize; } }

        public long UncompressedSize { get { return ZipItem.UncompressedSize; } }

        public DateTime CreationTime { get { return ZipItem.CreationTime; } }

        public DateTime LastWriteTime { get { return ZipItem.LastWriteTime; } }

        public DateTime LastAccessTime { get { return ZipItem.LastAccessTime; } }

        public ZipItemModel(ZipItem zipItem)
        {
            ZipItem = zipItem;
            Icon = zipItem.Attributes.HasFlag(FileAttributes.Directory) ? Folder : FileSystemImageHelper.GetImage(zipItem.Name, IconSize.Small);

            string[] items = zipItem.Name.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            Name = items.Last();
            Path = String.Join("/", items);
            Parent = String.Join("/", items.Take(items.Length - 1));
        }

        protected static ImageSource Folder = new BitmapImage(new Uri("pack://application:,,,/FileExplorer;component/Assets/Images/NewFolder16.png"));
    }
}
