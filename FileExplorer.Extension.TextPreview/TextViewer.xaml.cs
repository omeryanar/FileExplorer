using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace FileExplorer.Extension.TextPreview
{
    [Export(typeof(IPreviewExtension))]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.AssemblyName), "FileExplorer.Extension.TextPreview")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.DisplayName), "Text Viewer")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.Version), "1.0")]
    public partial class TextViewer : UserControl, IPreviewExtension
    {
        public TextDocument Document
        {
            get { return (TextDocument)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(nameof(Document), typeof(TextDocument), typeof(TextViewer));
   
        public IHighlightingDefinition SyntaxHighlighting
        {
            get { return (IHighlightingDefinition)GetValue(SyntaxHighlightingProperty); }
            set { SetValue(SyntaxHighlightingProperty, value); }
        }
        public static readonly DependencyProperty SyntaxHighlightingProperty =
            DependencyProperty.Register(nameof(SyntaxHighlighting), typeof(IHighlightingDefinition), typeof(TextViewer));

        public TextViewer()
        {
            InitializeComponent();
        }

        public bool CanPreviewFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            return extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) || HighlightingManager.Instance.GetDefinitionByExtension(extension) != null;
        }

        public async Task PreviewFile(string filePath)
        {
            try
            {
                string extension = Path.GetExtension(filePath);
                highlightingDefinition = HighlightingManager.Instance.GetDefinitionByExtension(extension);

                using (StreamReader reader = File.OpenText(filePath))
                {
                    string text = await reader.ReadToEndAsync();
                    Document = new TextDocument(text);
                    SyntaxHighlighting = highlightingDefinition;
                }
            }
            catch (Exception)
            {
                await UnloadFile();
            }
        }

        public Task UnloadFile()
        {
            Document = null;

            return Task.CompletedTask;
        }

        private IHighlightingDefinition highlightingDefinition;
    }
}
