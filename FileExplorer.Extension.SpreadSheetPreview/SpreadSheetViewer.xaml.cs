using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Spreadsheet;
using DevExpress.Xpf.Spreadsheet;

namespace FileExplorer.Extension.SpreadSheetPreview
{
    [Export(typeof(IPreviewExtension))]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.DisplayName), "Spread Sheet Viewer")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.SupportedFileTypes), "csv,xls,xlt,xlsx,xltx,xlsb,xlsm,xltm")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.Version), "1.0")]
    public partial class SpreadSheetViewer : UserControl, IPreviewExtension
    {
        public SpreadsheetDocumentSource Document
        {
            get { return (SpreadsheetDocumentSource)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(nameof(Document), typeof(SpreadsheetDocumentSource), typeof(SpreadSheetViewer));

        public SpreadSheetViewer()
        {
            InitializeComponent();
        }

        public async Task PreviewFile(string filePath)
        {
            try
            {
                using (FileStream fileStream = File.Open(filePath, FileMode.Open))
                {
                    MemoryStream memoryStream = new MemoryStream();
                    await fileStream.CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        Document = new SpreadsheetDocumentSource(memoryStream, DocumentFormat.Csv);
                    else
                        Document = new SpreadsheetDocumentSource(memoryStream, DocumentFormat.Undefined);
                }
            }
            catch (Exception)
            {
                await UnloadFile();
            }
        }

        public Task UnloadFile()
        {
            Document?.Stream?.Dispose();
            Document = null;

            return Task.CompletedTask;
        }

        private string[] supportedExtensions = new string[] { ".xls", ".xlt", ".xlsx", ".xltx", ".xlsb", ".xlsm", ".xltm", ".csv" };
    }
}
