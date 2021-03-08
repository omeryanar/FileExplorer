using System.Windows;
using DevExpress.Xpf.Editors;
using FileExplorer.View;

namespace FileExplorer.Controls
{
    public partial class ExpressionSelectorControl : ComboBoxEdit
    {
        public ExpressionSelectorControl()
        {
            InitializeComponent();
        }

        public void ShowManageExpressionView()
        {
            ManageExpressionView view = new ManageExpressionView
            {
                DataContext = App.Repository.Expressions,
                Owner = Window.GetWindow(this)
            };
            view.ShowDialog();

            RefreshData();
        }
    }
}
