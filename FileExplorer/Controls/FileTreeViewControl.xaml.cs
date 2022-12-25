using System.Windows;
using DevExpress.Data.TreeList;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.TreeList;
using FileExplorer.Properties;

namespace FileExplorer.Controls
{
    public partial class FileTreeViewControl : TreeListControl
    {
        public FileTreeViewControl()
        {
            InitializeComponent();
        }
    }

    public class TreeListViewEx : TreeListView
    {
        protected override TreeListDataProvider CreateDataProvider()
        {
            return new TreeListDataProviderEx(this);
        }

        protected override void OnFocusedNodeChanged()
        {
            base.OnFocusedNodeChanged();

            if (FocusedNode!= null)
                FocusedNode.IsExpanded = Settings.Default.ExpandFocusedNode;
        }

        protected override void RaiseNodeChanged(TreeListNode node, NodeChangeType changeType)
        {
            base.RaiseNodeChanged(node, changeType);

            if (Settings.Default.ExpandFocusedNode && changeType == NodeChangeType.Content && node == FocusedNode)
                node.IsExpanded = true;
        }

        public new void RaiseNodeExpanded(TreeListNode node)
        {
            base.RaiseNodeExpanded(node);
        }
        public new void RaiseNodeCollapsed(TreeListNode node)
        {
            base.RaiseNodeCollapsed(node);
        }
    }

    public class TreeListDataProviderEx : TreeListDataProvider
    {
        public TreeListDataProviderEx(TreeListViewEx view) : base(view)
        {
            view.PreviewMouseDown += (s, e) =>
            {
                DependencyObject target = e.OriginalSource as DependencyObject;
                TreeListViewHitInfo hitInfo = view.CalcHitInfo(target);
                if (hitInfo.InNodeExpandButton || hitInfo.InRow)
                    allowExpandNode = true;
            };

            view.NodeExpanding += (s, e) => 
            {
                e.Allow = allowExpandNode || Settings.Default.ExpandFocusedNode;
                allowExpandNode = false;
            };
        }
        
        protected override TreeListDataController CreateDataController()
        {
            return new TreeListDataControllerEx(this);
        }

        private bool allowExpandNode;
    }

    public class TreeListDataControllerEx : TreeListDataController
    {
        public TreeListDataControllerEx(TreeListDataProviderEx dataProvider) : base(dataProvider)
        {
        }

        protected override void OnNodeExpandedOrCollapsed(TreeListNodeBase node)
        {
            TreeListNode treeListNode = node as TreeListNode;
            TreeListViewEx treeListView = DataProvider.View as TreeListViewEx;

            if (treeListNode.IsExpanded)
                treeListView.RaiseNodeExpanded(treeListNode);
            else
                treeListView.RaiseNodeCollapsed(treeListNode);
        }
    }
}
