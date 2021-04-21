using DevExpress.Data.TreeList;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.TreeList;

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
        }
        
        protected override TreeListDataController CreateDataController()
        {
            return new TreeListDataControllerEx(this);
        }
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
