using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using DevExpress.Data.TreeList;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.TreeList;
using FileExplorer.Properties;

namespace FileExplorer.Controls
{
    public partial class FileTreeViewControl : TreeListControl
    {
        public object ClickedItem
        {
            get { return GetValue(ClickedItemProperty); }
            set { SetValue(ClickedItemProperty, value); }
        }
        public static readonly DependencyProperty ClickedItemProperty = DependencyProperty.Register(nameof(ClickedItem), typeof(object), typeof(FileTreeViewControl));

        public IList ClickedItems
        {
            get { return (IList)GetValue(ClickedItemsProperty); }
            set { SetValue(ClickedItemsProperty, value); }
        }
        public static readonly DependencyProperty ClickedItemsProperty = DependencyProperty.Register(nameof(ClickedItems), typeof(IList), typeof(FileTreeViewControl));

        public FileTreeViewControl()
        {
            InitializeComponent();
        }

        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (View is not null)
            {
                DependencyObject target = e.OriginalSource as DependencyObject;
                GridViewHitInfoBase hitInfo = View.CalcHitInfo(target);
                if (hitInfo != null && hitInfo.RowHandle >= 0)
                {
                    ClickedItem = GetRow(hitInfo.RowHandle);
                    ClickedItems = new List<object> { ClickedItem };

                    if (e.RightButton == MouseButtonState.Pressed)
                        return;
                }
            }

            base.OnPreviewMouseRightButtonDown(e);
        }
    }

    public class TreeListViewEx : TreeListView
    {
        public TreeListViewEx()
        {
            NodeExpanding += (s, e) =>
            {
                e.Allow = allowExpandNode || Settings.Default.ExpandFocusedNode;
                allowExpandNode = false;
            };

            NodeCollapsing += (s, e) => { nodeCollapsing = true; };
            NodeCollapsed += (s, e) => { nodeCollapsing = false; };
        }

        protected override void OnFocusedNodeChanged()
        {
            base.OnFocusedNodeChanged();

            if (!nodeCollapsing && FocusedNode != null)
                FocusedNode.IsExpanded = Settings.Default.ExpandFocusedNode;
        }

        protected override void RaiseNodeChanged(TreeListNode node, NodeChangeType changeType)
        {
            base.RaiseNodeChanged(node, changeType);

            if (Settings.Default.ExpandFocusedNode && changeType == NodeChangeType.Content && node == FocusedNode)
                node.IsExpanded = true;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            DependencyObject target = e.OriginalSource as DependencyObject;
            TreeListViewHitInfo hitInfo = CalcHitInfo(target);
            if (hitInfo.InNodeExpandButton || hitInfo.InRow)
                allowExpandNode = true;
        }

        private bool allowExpandNode;

        private bool nodeCollapsing;
    }
}
