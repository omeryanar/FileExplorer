using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FileExplorer.Extension.RichTextPreview
{
    [Export(typeof(IPreviewExtension))]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.AssemblyName), "FileExplorer.Extension.RichTextPreview")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.DisplayName), "Rich Text Viewer")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.Version), "1.0")]
    public partial class RichTextViewer : UserControl, IPreviewExtension
    {
        public Stream Document
        {
            get { return (Stream)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(nameof(Document), typeof(Stream), typeof(RichTextViewer));

        public float ZoomFactor
        {
            get { return (float)GetValue(ZoomFactorProperty); }
            set { SetValue(ZoomFactorProperty, value); }
        }
        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register(nameof(ZoomFactor), typeof(float), typeof(RichTextViewer), new PropertyMetadata(1f));

        public RichTextViewer()
        {
            InitializeComponent();
        }

        public bool CanPreviewFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            return supportedExtensions.Any(x => extension.Equals(x, StringComparison.OrdinalIgnoreCase));
        }

        public async Task PreviewFile(string filePath)
        {
            try
            {
                ZoomFactor = 100f;

                using (FileStream fileStream = File.Open(filePath, FileMode.Open))
                {
                    MemoryStream memoryStream = new MemoryStream();
                    await fileStream.CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    Document = memoryStream;
                }
            }
            catch (Exception)
            {
                await UnloadFile();
            }
        }

        public Task UnloadFile()
        {
            Document?.Dispose();
            Document = null;

            return Task.CompletedTask;
        }

        private string[] supportedExtensions = new string[] { ".rtf", ".doc", ".docx", ".docm", ".dot", ".dotm", ".dotx", ".odt", ".epub", ".htm", ".html", ".mht" };
    }
}
