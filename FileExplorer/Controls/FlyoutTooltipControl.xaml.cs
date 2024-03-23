using System.Windows;
using System.Windows.Controls;

namespace FileExplorer.Controls
{
    public partial class FlyoutTooltipControl : UserControl
    {
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(object), typeof(FlyoutTooltipControl));

        public FlyoutTooltipControl()
        {
            InitializeComponent();
        }
    }
}
