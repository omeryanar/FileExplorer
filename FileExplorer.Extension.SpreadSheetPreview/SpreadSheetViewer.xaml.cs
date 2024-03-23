using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using DevExpress.Spreadsheet;

namespace FileExplorer.Extension.SpreadSheetPreview
{
    [Export(typeof(IPreviewExtension))]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.DisplayName), "Spread Sheet Viewer")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.SupportedFileTypes), "csv|xls|xlt|xlsx|xltx|xlsb|xlsm|xltm")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.Version), "1.0")]
    public partial class SpreadSheetViewer : UserControl, IPreviewExtension
    {
        public Stream DocumentSource { get; private set; }

        public SpreadSheetViewer()
        {
            InitializeComponent();
        }

        public async Task PreviewFile(string filePath)
        {
            using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                DocumentSource = new MemoryStream();
                await fileStream.CopyToAsync(DocumentSource);
                DocumentSource.Seek(0, SeekOrigin.Begin);

                if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    SpreadSheetEditor.LoadDocument(DocumentSource, DocumentFormat.Csv);
                else
                    SpreadSheetEditor.LoadDocument(DocumentSource, DocumentFormat.Undefined);
            }
        }

        public Task UnloadFile()
        {
            SpreadSheetEditor.CreateNewDocument();
            DocumentSource?.Dispose();

            return Task.CompletedTask;
        }
    }
}
