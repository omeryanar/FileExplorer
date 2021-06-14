using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using DevExpress.Xpf.LayoutControl;

namespace FileExplorer.View
{
    public partial class BrowserTabView : UserControl
    {
        public BrowserTabView()
        {
            InitializeComponent();
        }

        private void ElementSizer_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void ElementSizer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            BindingExpression bindingExpression = (sender as ElementSizer).Element.GetBindingExpression(WidthProperty);
            bindingExpression.UpdateSource();
        }
    }
}
