using System;
using System.Linq;
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

        private void OnValidate(object sender, ValidationEventArgs e)
        {
            if (e.Value is String statement && !String.IsNullOrWhiteSpace(statement))
            {
                if (App.Repository.Expressions.Any(x => x.Statement == statement) == false)
                {
                    App.Repository.Expressions.Add(new Persistence.Expression { Statement = statement });
                    RefreshData();
                }
            }
        }
    }
}
