using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Editors;

namespace FileExplorer.Controls
{
    public partial class ExtensionTokenEditControl : ComboBoxEdit
    {
        public ExtensionTokenEditControl()
        {
            InitializeComponent();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                Clipboard.SetText(DisplayText);
        }

        private void OnTokenTextChanging(object sender, TokenTextChangingEventArgs e)
        {
            if (e.NewText != null)
            {
                e.Text = e.NewText.Trim();
                e.Handled = true;
            }
        }
    }
}
