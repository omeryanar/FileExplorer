using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FileExplorer.Extension.RichTextPreview
{
    [Export(typeof(IPreviewExtension))]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.DisplayName), "Rich Text Viewer")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.SupportedFileTypes), "doc|docx|docm|dot|dotm|dotx|epub|htm|html|mht|odt|rtf")]
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

        public async Task PreviewFile(string filePath)
        {
            ZoomFactor = 100f;

            using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                MemoryStream memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                Document = memoryStream;
            }
        }

        public Task UnloadFile()
        {
            if (Document != null)
            {
                Stream stream = Document;
                Document = null;
                stream.Dispose();
            }

            return Task.CompletedTask;
        }
    }
}
