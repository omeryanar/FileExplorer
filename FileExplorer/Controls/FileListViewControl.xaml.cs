using System.Collections;
using System.IO;
using System.Linq;
using System.Windows;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Grid;
using FileExplorer.Core;
using FileExplorer.Persistence;

namespace FileExplorer.Controls
{
    public partial class FileListViewControl : GridControl
    {
        public string HighlightedText
        {
            get { return (string)GetValue(HighlightedTextProperty); }
            set { SetValue(HighlightedTextProperty, value); }
        }
        public static readonly DependencyProperty HighlightedTextProperty = DependencyProperty.Register(nameof(HighlightedText), typeof(string), typeof(FileListViewControl));

        public FileListViewControl()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                if (DefaultLayoutStream == null)
                {
                    DefaultLayoutStream = new MemoryStream();
                    SaveLayoutToStream(DefaultLayoutStream);
                }
            };
        }

        public void InvertSelection()
        {
            ArrayList selection = new ArrayList(SelectedItems);

            foreach (var item in VisibleItems)
            {
                if (selection.Contains(item))
                    SelectedItems.Remove(item);
                else
                    SelectedItems.Add(item);
            }
        }

        public void ToggleGrouping(string fieldName)
        {
            GridColumn column = Columns[fieldName];
            GridViewBase view = View as GridViewBase;
            bool isGroupedColumn = view.GroupedColumns.Contains(column);

            ClearGrouping();
            if (!isGroupedColumn)
                GroupBy(column);
        }

        public void CopySelectedRowsToClipboard(GridColumn gridColumn = null)
        {
            TableView tableView = View as TableView;
            int[] rowHandles = GetSelectedRowHandles();
            if (tableView != null && rowHandles?.Length > 0)
            {
                int startRowHandle = rowHandles[0];
                int endRowHandle = rowHandles[rowHandles.Length - 1];
                GridColumn startColumn = gridColumn != null ? gridColumn : tableView.VisibleColumns[1];
                GridColumn endColumn = gridColumn != null ? gridColumn : tableView.VisibleColumns[tableView.VisibleColumns.Count - 1];

                tableView.CopyCellsToClipboard(startRowHandle, startColumn, endRowHandle, endColumn);
            }
        }

        public void SaveFolderLayout(string folderPath, bool applyToSubFolders = false)
        {
            if (!Path.IsPathRooted(folderPath))
                return;

            FolderLayout layout = App.Repository.FolderLayouts.FirstOrDefault(x => x.FolderPath.OrdinalEquals(folderPath));
            if (layout == null)
                layout = new FolderLayout { Name = Path.GetFileName(folderPath), FolderPath = folderPath };

            MemoryStream layoutStream = new MemoryStream();
            SaveLayoutToStream(layoutStream);

            layout.ApplyToSubFolders = applyToSubFolders;
            layout.LayoutData = layoutStream.ToArray();

            App.Repository.FolderLayouts.Add(layout);

            ShowManageLayoutsDialog();
        }        

        public void LoadFolderLayout(string folderPath)
        {
            if (!Path.IsPathRooted(folderPath))
                return;

            FolderLayout layout = App.Repository.FolderLayouts.FirstOrDefault(x => x.FolderPath.OrdinalEquals(folderPath));
            if (layout == null)
                layout = App.Repository.FolderLayouts.Where(x => x.ApplyToSubFolders && folderPath.OrdinalStartsWith(x.FolderPath)).
                    DefaultIfEmpty().Aggregate((x, y) => x.FolderPath.Length > y.FolderPath.Length ? x : y);

            if (layout != null)
            {
                if (layout.LayoutStream != null && layout.FolderPath != LastLoadedLayoutFolder)
                {
                    layout.LayoutStream.Position = 0;
                    RestoreLayoutFromStream(layout.LayoutStream);

                    LastLoadedLayoutFolder = layout.FolderPath;
                }
            }
        }

        public void LoadDefaultLayout()
        {
            DefaultLayoutStream.Position = 0;
            RestoreLayoutFromStream(DefaultLayoutStream);

            LastLoadedLayoutFolder = null;
        }

        public void ShowManageLayoutsDialog()
        {
            IDialogService dialogService = DataContext.GetService<IDialogService>();
            if (dialogService != null)
                dialogService.ShowDialog(MessageButton.OK, Properties.Resources.ManageSavedLayouts, "ManageLayoutView", App.Repository.FolderLayouts);
        }

        protected override void InitiallyFocusedRowAfterFiltering(object row)
        {
            base.InitiallyFocusedRowAfterFiltering(row);
            View.ScrollIntoView(SelectedItem);
        }

        private static MemoryStream DefaultLayoutStream;

        private string LastLoadedLayoutFolder;
    }
}
