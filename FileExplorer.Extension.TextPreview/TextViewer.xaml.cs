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
    [ExportMetadata(nameof(IPreviewExtensionMetadata.DisplayName), "Text Viewer")]
    [ExportMetadata(nameof(IPreviewExtensionMetadata.SupportedFileTypes), "addin,asax,ascx,asmx,asp,aspx,atg,boo,booproj,build,c,cc,config,cpp,cs,csproj,css,diff,disco,h,hpp,htm,html,ilproj,java,js,json,manifest,map,master,md,nuspec,patch,php,proj,ps1,ps1xml,psd1,psm1,py,pyw,sql,targets,tex,txt,vb,vbproj,wsdl,wxi,wxl,wxs,xaml,xfrm,xft,xml,xpt,xsd,xshd,xsl,xslt")]
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

        public async Task PreviewFile(string filePath)
        {
            try
            {
                string extension = Path.GetExtension(filePath);
                SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(extension);

                using (StreamReader reader = File.OpenText(filePath))
                {
                    string text = await reader.ReadToEndAsync();
                    Document = new TextDocument(text);
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
    }
}
