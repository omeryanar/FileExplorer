using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.DocumentViewer;

namespace FileExplorer.Extension.PdfPreview
{
    [Export(typeof(IPreviewExtension))]
    public partial class PdfViewer : UserControl, IPreviewExtension
    {
        public Stream Document
        {
            get { return (Stream)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(nameof(Document), typeof(Stream), typeof(PdfViewer));

        public double ZoomFactor
        {
            get { return (double)GetValue(ZoomFactorProperty); }
            set { SetValue(ZoomFactorProperty, value); }
        }
        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register(nameof(ZoomFactor), typeof(double), typeof(PdfViewer), new PropertyMetadata(1.0));

        public PdfViewer()
        {
            InitializeComponent();
        }

        public bool CanPreviewFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        public async Task PreviewFile(string filePath)
        {
            await UnloadFile();
            ZoomFactor = 1;

            using (FileStream fileStream = File.Open(filePath, FileMode.Open))
            {
                MemoryStream memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                Document = memoryStream;
            }
        }

        public Task UnloadFile()
        {
            Document?.Dispose();
            Document = null;

            return Task.CompletedTask;
        }

        private void OnPdfViewerLoaded(object sender, RoutedEventArgs e)
        {
            DXScrollViewer viewer = LayoutTreeHelper.GetVisualChildren(this).OfType<DXScrollViewer>().FirstOrDefault();
            if (viewer != null)
            {
                viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
        }
    }
}
