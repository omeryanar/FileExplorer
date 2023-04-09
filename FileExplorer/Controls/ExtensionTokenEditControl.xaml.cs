using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Editors;
using FileExplorer.Core;

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

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);

            if (CanShowPopup && Convert.ToBoolean(e.NewValue) == true)
                ShowPopup();
        }

        private void OnTokenTextChanging(object sender, TokenTextChangingEventArgs e)
        {
            if (e.NewText != null)
            {
                e.Text = e.NewText.Trim();
                e.Handled = true;

                if (ItemsSource is IEnumerable<object> items && items.Any(x => x.ToString().OrdinalStartsWith(e.NewText)))
                    ShowPopup();
                else
                    ClosePopup();
            }
        }
    }
}
