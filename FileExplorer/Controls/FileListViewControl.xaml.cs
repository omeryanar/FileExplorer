using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Data;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.TreeList;
using FileExplorer.Core;
using FileExplorer.Helpers;
using FileExplorer.Model;
using FileExplorer.Persistence;
using FileExplorer.Properties;

namespace FileExplorer.Controls
{
    public partial class FileListViewControl : GridControl
    {
        public string CurrentFolderPath
        {
            get { return (string)GetValue(CurrentFolderPathProperty); }
            set { SetValue(CurrentFolderPathProperty, value); }
        }
        public static readonly DependencyProperty CurrentFolderPathProperty = DependencyProperty.Register(nameof(CurrentFolderPath), typeof(string), typeof(FileListViewControl), new PropertyMetadata(null, OnCurrentFolderPathChanged));

        public string HighlightedText
        {
            get { return (string)GetValue(HighlightedTextProperty); }
            set { SetValue(HighlightedTextProperty, value); }
        }
        public static readonly DependencyProperty HighlightedTextProperty = DependencyProperty.Register(nameof(HighlightedText), typeof(string), typeof(FileListViewControl), new PropertyMetadata(null, OnHighlightedTextChanged));

        public ICommand RowDoubleClickCommand
        {
            get => (ICommand)GetValue(RowDoubleClickCommandProperty);
            set => SetValue(RowDoubleClickCommandProperty, value);
        }
        public static readonly DependencyProperty RowDoubleClickCommandProperty = DependencyProperty.Register(nameof(RowDoubleClickCommand), typeof(ICommand), typeof(FileListViewControl));

        public ICommand EditSelectedItemCommand
        {
            get { return (ICommand)GetValue(EditSelectedItemCommandProperty); }
            set { SetValue(EditSelectedItemCommandProperty, value); }
        }
        public static readonly DependencyProperty EditSelectedItemCommandProperty = DependencyProperty.Register(nameof(EditSelectedItemCommand), typeof(ICommand), typeof(FileListViewControl));

        public ICommand EditSelectedItemsCommand
        {
            get { return (ICommand)GetValue(EditSelectedItemsCommandProperty); }
            set { SetValue(EditSelectedItemsCommandProperty, value); }
        }
        public static readonly DependencyProperty EditSelectedItemsCommandProperty = DependencyProperty.Register(nameof(EditSelectedItemsCommand), typeof(ICommand), typeof(FileListViewControl));

        public ICommand EditCommand
        {
            get { return (ICommand)GetValue(EditCommandProperty); }
            private set { SetValue(EditCommandProperty, value); }
        }
        public static readonly DependencyProperty EditCommandProperty = DependencyProperty.Register(nameof(EditCommand), typeof(ICommand), typeof(FileListViewControl));

        public ICommand SaveSelectionCommand
        {
            get { return (ICommand)GetValue(SaveSelectionCommandProperty); }
            set { SetValue(SaveSelectionCommandProperty, value); }
        }
        public static readonly DependencyProperty SaveSelectionCommandProperty = DependencyProperty.Register(nameof(SaveSelectionCommand), typeof(ICommand), typeof(FileListViewControl));

        public ICommand RestoreSelectionCommand
        {
            get { return (ICommand)GetValue(RestoreSelectionCommandProperty); }
            set { SetValue(RestoreSelectionCommandProperty, value); }
        }
        public static readonly DependencyProperty RestoreSelectionCommandProperty = DependencyProperty.Register(nameof(RestoreSelectionCommand), typeof(ICommand), typeof(FileListViewControl));

        public Settings LocalSettings
        {
            get { return (Settings)GetValue(LocalSettingsProperty); }
            set { SetValue(LocalSettingsProperty, value); }
        }
        public static readonly DependencyProperty LocalSettingsProperty = DependencyProperty.Register(nameof(LocalSettings), typeof(Settings), typeof(FileListViewControl), new PropertyMetadata(new Settings()));

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

                LoadDefaultColumnSettins();
            };

            EndSorting += (s, e) =>
            {
                if (SelectedItem != null)
                    View.ScrollIntoView(SelectedItem);
            };

            SelectionChanged += (s, e) =>
            {
                if (SelectedItems.Count == 0)
                    CurrentItem = null;
            };

            ItemsSourceChanged += (s, e) =>
            {
                LoadFolderLayout(CurrentFolderPath);

                if (LocalSettings.AutoRestoreSelection)
                    RestoreSelection(CurrentFolderPath);
            };

            CustomColumnSort += (s, e) =>
            {
                this.NaturalSort(e, LocalSettings.UnifiedSorting);
            };

            ClickTimer = new DispatcherTimer();
            ClickTimer.Interval = TimeSpan.FromMilliseconds(SystemInformation.DoubleClickTime);
            ClickTimer.Tick += (s, e) =>
            {
                StopClickTimer();
                EditSelectedItemCommand?.Execute(SelectedItem);
            };

            EditCommand = new DelegateCommand(() =>
            {
                if (SelectedItems?.Count > 1)
                    EditSelectedItemsCommand?.Execute(SelectedItems);
                else
                    EditSelectedItemCommand?.Execute(SelectedItem);
            }, 
            () => { return EditSelectedItemCommand?.CanExecute(SelectedItem) == true || EditSelectedItemsCommand?.CanExecute(SelectedItems) == true; });

            SaveSelectionCommand = new DelegateCommand(() => { SaveSelection(); }, () => { return SelectedItems.Count > 0; });

            RestoreSelectionCommand = new DelegateCommand(() => { RestoreSelection(); }, () => { return !String.IsNullOrEmpty(CurrentFolderPath) && ManuelRestoreItemsDictionary.ContainsKey(CurrentFolderPath); });
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
            if (View is GridViewBase gridView)
            {
                GridColumn column = Columns[fieldName];
                bool isGroupedColumn = gridView.GroupedColumns.Contains(column);

                if (isGroupedColumn)
                    UngroupBy(column);
                else
                    GroupBy(column, true);
            }
        }

        public void CopySelectedRowsToClipboard(GridColumn gridColumn = null)
        {
            try
            {
                int startRowHandle = GetRowHandleByVisibleIndex(0);
                int endRowHandle = GetRowHandleByVisibleIndex(VisibleRowCount - 1);

                if (gridColumn == null)
                {
                    ClipboardCopyMode = ClipboardCopyMode.Default;

                    if (SelectedItem != null)
                        CopySelectedItemsToClipboard();
                    else
                        CopyRangeToClipboard(startRowHandle, endRowHandle);
                }
                else
                {
                    ClipboardCopyMode = ClipboardCopyMode.ExcludeHeader;

                    if (SelectedItem != null)
                    {
                        List<string> values = new List<string>();

                        foreach (int rowHandle in GetSelectedRowHandles())
                            values.Add(GetCellDisplayText(rowHandle, gridColumn));

                        System.Windows.Clipboard.SetText(values.Join(Environment.NewLine));
                    }
                    else
                    {
                        if (View is TableView tableView)
                            tableView.CopyCellsToClipboard(startRowHandle, gridColumn, endRowHandle, gridColumn);
                        else if (View is TreeListView treeView)
                            treeView.CopyCellsToClipboard(startRowHandle, gridColumn, endRowHandle, gridColumn);
                    }                    
                }
            }
            finally { ClipboardCopyMode = ClipboardCopyMode.None; }
        }

        public void SaveFolderLayout(string folderPath, bool applyToSubFolders = false)
        {
            if (!Path.IsPathRooted(folderPath))
                return;

            FolderLayout layout = App.Repository.FolderLayouts.FirstOrDefault(x => x.FolderPath.OrdinalEquals(folderPath));
            if (layout == null)
                layout = new FolderLayout { Name = Path.GetFileName(folderPath), FolderPath = folderPath };

            using(MemoryStream layoutStream = new MemoryStream())
            {
                SaveLayoutToStream(layoutStream);

				layout.LayoutType = LocalSettings.LayoutType;
				layout.ApplyToSubFolders = applyToSubFolders;
                layout.LayoutData = layoutStream.ToArray();                
			}

            LayoutState = LayoutStatus.Folder;

            App.Repository.FolderLayouts.Add(layout);
            ShowManageLayoutsDialog();
        }        

        public void LoadFolderLayout(string folderPath)
        {
            if (!String.IsNullOrEmpty(HighlightedText) && LayoutState == LayoutStatus.Folder)
            {
                LoadCurrentFolderLayout();
                return;
            }

            if (FileSystemHelper.RecycleBinPath.OrdinalEquals(folderPath))
            {
                LoadDefaultLayout();
                Columns["DateDeleted"].Visible = true;
                Columns["OriginalLocation"].Visible = true;

                return;
            }
            else
            {
                Columns["DateDeleted"].Visible = false;
                Columns["OriginalLocation"].Visible = false;
            }

            FolderLayout layout = App.Repository.FolderLayouts.FirstOrDefault(x => x.FolderPath.OrdinalEquals(folderPath));
            if (layout == null)
                layout = App.Repository.FolderLayouts.Where(x => x.ApplyToSubFolders && folderPath.OrdinalStartsWith(x.FolderPath)).
                    DefaultIfEmpty().Aggregate((x, y) => x.FolderPath.Length > y.FolderPath.Length ? x : y);

            if (layout != null)
            {
                if (LayoutState != LayoutStatus.Folder)
                    SaveCurrentFolderLayout();

                LoadFolderLayout(layout);
            }
            else
            {
				if (LayoutState == LayoutStatus.Folder)
					LoadCurrentFolderLayout();
			}
        }

        public void SaveSelection()
        {
            SaveSelection(CurrentFolderPath, false);
        }

        public void SaveSelection(string folderPath, bool autoRestore = true)
        {
            Dictionary<string, IList> selectedItemsDictionary = autoRestore ? AutoRestoreItemsDictionary : ManuelRestoreItemsDictionary;

            if (!String.IsNullOrEmpty(folderPath) && SelectedItems.Count > 0)
                selectedItemsDictionary[folderPath] = new ArrayList(SelectedItems);
        }

        public void RestoreSelection()
        {
            RestoreSelection(CurrentFolderPath, false);   
        }

        public void RestoreSelection(string folderPath, bool autoRestore = true)
        {
            Dictionary<string, IList> selectedItemsDictionary = autoRestore ? AutoRestoreItemsDictionary : ManuelRestoreItemsDictionary;

            if (!String.IsNullOrEmpty(folderPath) && selectedItemsDictionary.TryGetValue(folderPath, out IList selectedItems))
            {
                if (View is TreeListView treeView)
                {
                    TreeListNodeIterator nodeIterator = new TreeListNodeIterator(treeView.Nodes, false);
                    while (nodeIterator.MoveNext())
                    {
                        FileModel fileModel = nodeIterator.Current.Content as FileModel;
                        if (fileModel?.Folders != null)
                            nodeIterator.Current.IsExpanded = true;
                    }
                }

                SelectedItems.Clear();
                foreach (object item in selectedItems)
                    SelectedItems.Add(item);
            }
        }

		public void LoadDefaultLayout()
        {
			LocalSettings.LayoutType = Settings.Default.LayoutType;
			GridSerializationOptions.SetAddNewColumns(this, false);

			DefaultLayoutStream.Position = 0;
            RestoreLayoutFromStream(DefaultLayoutStream);

            LoadDefaultColumnSettins();

			LayoutState = LayoutStatus.Default;
        }

        public void ShowManageLayoutsDialog()
        {
            IDialogService dialogService = DataContext.GetService<IDialogService>();
            if (dialogService != null)
                dialogService.ShowDialog(MessageButton.OK, Properties.Resources.ManageSavedLayouts, "ManageLayoutView", App.Repository.FolderLayouts);
        }

        public void ShowCustomMenuDialog()
        {
            IDialogService dialogService = DataContext.GetService<IDialogService>();
            if (dialogService != null)
                dialogService.ShowDialog(MessageButton.OK, Properties.Resources.CustomMenuItems, "CustomMenuView", App.Repository.MenuItems);
        }

		protected void SaveCurrentFolderLayout()
		{
			CurrentFolderLayout = new FolderLayout { LayoutType = LocalSettings.LayoutType };

			using (MemoryStream layoutStream = new MemoryStream())
			{
				SaveLayoutToStream(layoutStream);

				CurrentFolderLayout.LayoutType = LocalSettings.LayoutType;
				CurrentFolderLayout.LayoutData = layoutStream.ToArray();
			}
		}

		protected void LoadCurrentFolderLayout()
		{
			if (CurrentFolderLayout != null && CurrentFolderLayout.LayoutStream != null)
            {
				LocalSettings.LayoutType = CurrentFolderLayout.LayoutType;
				GridSerializationOptions.SetAddNewColumns(this, false);

				CurrentFolderLayout.LayoutStream.Position = 0;
				RestoreLayoutFromStream(CurrentFolderLayout.LayoutStream);

				LayoutState = LayoutStatus.Current;
			}
            else
                LoadDefaultLayout();
		}

		protected void LoadFolderLayout(FolderLayout folderLayout)
		{
			if (folderLayout != null && folderLayout.LayoutStream != null)
			{
				LocalSettings.LayoutType = folderLayout.LayoutType;
				GridSerializationOptions.SetAddNewColumns(this, true);

				folderLayout.LayoutStream.Position = 0;
				RestoreLayoutFromStream(folderLayout.LayoutStream);

				LayoutState = LayoutStatus.Folder;
			}
		}

		protected override void InitiallyFocusedRowAfterFiltering(object row)
        {
            base.InitiallyFocusedRowAfterFiltering(row);

            if (SelectedItem != null)
                View.ScrollIntoView(SelectedItem);
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            OldClickedItem = CurrentItem;
            base.OnPreviewMouseLeftButtonDown(e);
            NewClickedItem = CurrentItem;            

            if (View.IsEditing || Keyboard.Modifiers != ModifierKeys.None)
            {
                StopClickTimer();
                return;
            }

            DependencyObject target = e.OriginalSource as DependencyObject;
            GridViewHitInfoBase hitInfo = View.CalcHitInfo(target);
            if (hitInfo?.IsDataArea == true)
            {
                UnselectAll();
                return;
            }
            else if (e.ClickCount == 2)
            {
                StopClickTimer();
                RowDoubleClickCommand?.Execute(CurrentItem);
            }
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (currentTime - FocusTime < SystemInformation.DoubleClickTime)
                return;

            if (View.IsEditing || Keyboard.Modifiers != ModifierKeys.None)
            {
                StopClickTimer();
                return;
            }

            if (NewClickedItem != null && NewClickedItem == OldClickedItem)
            {
                DependencyObject target = e.OriginalSource as DependencyObject;
                GridViewHitInfoBase hitInfo = View.CalcHitInfo(target);

                if (hitInfo is CardViewHitInfo cardViewHitInfo && cardViewHitInfo.InRow)
                    ClickTimer.Start();

                if (hitInfo != null && hitInfo.InRow && hitInfo.Column == Columns[0])
                    ClickTimer.Start();
            }
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);

            bool hasFocus = Convert.ToBoolean(e.NewValue);
            FocusTime = hasFocus ? DateTimeOffset.Now.ToUnixTimeMilliseconds() : Int64.MaxValue;
        }

        private void StopClickTimer()
        {
            ClickTimer.Stop();

            OldClickedItem = null;
            NewClickedItem = null;
        }

        private static void OnCurrentFolderPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FileListViewControl fileListViewControl)
            {
                if (fileListViewControl.LocalSettings.AutoRestoreSelection && e.OldValue != null)
                    fileListViewControl.SaveSelection(e.OldValue.ToString());
            }
        }

        private static void OnHighlightedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridControl gridControl)
                gridControl.View.SearchString = e.NewValue == null ? null : e.NewValue.ToString();
        }

        private void LoadDefaultColumnSettins()
        {
            if (String.IsNullOrEmpty(Settings.Default.ColumnSettings))
                return;

			if (SurrogateFileGridControl == null)
            {
				SurrogateFileGridControl = new GridControl();
                SurrogateFileGridControl.View = new TableView();

				GridSerializationOptions.SetAddNewColumns(SurrogateFileGridControl, false);
				GridSerializationOptions.SetRemoveOldColumns(SurrogateFileGridControl, false);
			}

			using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(Settings.Default.ColumnSettings)))
			{
				SurrogateFileGridControl.RestoreLayoutFromStream(stream);
			}

            foreach (GridColumn surrogateColumn in SurrogateFileGridControl.Columns)
            {
                if (Columns[surrogateColumn.FieldName] == null)
                    continue;

                Columns[surrogateColumn.FieldName].Visible = surrogateColumn.Visible;
				Columns[surrogateColumn.FieldName].VisibleIndex = surrogateColumn.VisibleIndex;

				Columns[surrogateColumn.FieldName].SortIndex = surrogateColumn.SortIndex;
				Columns[surrogateColumn.FieldName].SortOrder = surrogateColumn.SortOrder;

				Columns[surrogateColumn.FieldName].GroupIndex = surrogateColumn.GroupIndex;
			}
		}

        private Dictionary<string, IList> AutoRestoreItemsDictionary = new Dictionary<string, IList>();

        private Dictionary<string, IList> ManuelRestoreItemsDictionary = new Dictionary<string, IList>();

        private static MemoryStream DefaultLayoutStream;

        private GridControl SurrogateFileGridControl;

		private FolderLayout CurrentFolderLayout;

        private DispatcherTimer ClickTimer;

        private LayoutStatus LayoutState;

		private object NewClickedItem;

        private object OldClickedItem;

        private long FocusTime;

		private enum LayoutStatus { Default, Current, Folder }
	}

    public class TableViewEx : TableView
    {
        public TableViewEx()
        {
            RowEditStarting += (s, e) => { ScrollIntoView(e.RowHandle); };

            RowEditFinished += (s, e) => { DataControl.Focus(); };
        }

        protected override void UpdateAfterIncrementalSearch()
        {
            base.UpdateAfterIncrementalSearch();
            
            if (TextSearchEngineRoot.MatchedItemIndex != null && TextSearchEngineRoot.MatchedItemIndex.RowIndex == FocusedRowHandle)
                DataControl.SelectedItem = DataControl.CurrentItem;
        }
    }

    public class CardViewEx : CardView
    {
        protected override void UpdateAfterIncrementalSearch()
        {
            base.UpdateAfterIncrementalSearch();

            if (TextSearchEngineRoot.MatchedItemIndex != null && TextSearchEngineRoot.MatchedItemIndex.RowIndex == FocusedRowHandle)
                DataControl.SelectedItem = DataControl.CurrentItem;
        }
    }

    public class TreeViewEx : TreeListView
    {
        public TreeViewEx()
        {
            NodeEditStarting += (s, e) => { ScrollIntoView(e.Node.RowHandle); };

            NodeEditFinished += (s, e) => { DataControl.Focus(); };

            CustomColumnSort += (s, e) =>
            {
                if (DataControl is FileListViewControl fileListViewControl)
                    this.NaturalSort(e, fileListViewControl.LocalSettings.UnifiedSorting);
            };
        }

        protected override void UpdateAfterIncrementalSearch()
        {
            base.UpdateAfterIncrementalSearch();

            if (TextSearchEngineRoot.MatchedItemIndex != null && TextSearchEngineRoot.MatchedItemIndex.RowIndex == FocusedRowHandle)
                DataControl.SelectedItem = DataControl.CurrentItem;
        }

        public async Task ExpandToLevelAsync(int level)
        {
            TreeListNode[] nodes = Nodes.ToArray();

            IList<TreeListRowInfo> selectedNodes = GetSelectedRows();
            if (selectedNodes.Count > 0)
                nodes = selectedNodes.Select(x => x.Node).ToArray();

            try
            {
                BeginDataUpdate(false);

                foreach (TreeListNode node in nodes)
                    await LoadChildren(node.Content as FileModel, level);
            }
            finally
            {
                EndDataUpdate();
            }

            ExpandToLevel(nodes, level);
        }

        private async Task LoadChildren(FileModel fileModel, int level)
        {
            if (fileModel == null)
                return;

            if (fileModel.Content == null)
                await fileModel.EnumerateChildren();

            if (level > 0)
            {
                foreach (FileModel childModel in fileModel.Folders)
                    await LoadChildren(childModel, level - 1);
            }
        }

        private void ExpandToLevel(IEnumerable nodes, int level)
        {
            List<TreeListNode> treeListNodes = nodes.OfType<TreeListNode>().ToList();
            foreach (TreeListNode node in treeListNodes)
            {
                node.IsExpanded = true;

                ExpandToLevel(node.Nodes, level);
                node.IsExpanded = level > node.Level;
            }
        }
    }

    public static class FileModelSorter
    {
        public static void NaturalSort(this TreeListView treeListView, TreeListCustomColumnSortEventArgs e, bool unifiedSorting)
        {
            FileModel value1 = e.Node1.Content as FileModel;
            FileModel value2 = e.Node2.Content as FileModel;

            if (value1 == null || value2 == null)
                return;

            if (e.Column.UnboundType != UnboundColumnType.Bound)
            {
                object nodeValue1 = treeListView.GetNodeValue(e.Node1, e.Column);
                object nodeValue2 = treeListView.GetNodeValue(e.Node2, e.Column);

                if (nodeValue1 is UnboundErrorObject)
                    e.Result = e.SortOrder == ColumnSortOrder.Ascending ? 1 : -1;
                else if (nodeValue2 is UnboundErrorObject)
                    e.Result = e.SortOrder == ColumnSortOrder.Ascending ? -1 : 1;
                else
                    e.Result = Comparer.Default.Compare(nodeValue1, nodeValue2);

                e.Handled = true;
            }

			var unifiedSort = NaturalSort(value1, value2, e.SortOrder, e.Column.FieldName, e.Result, unifiedSorting);
			e.Result = unifiedSort.Result;
			e.Handled = unifiedSort.Handled;
		}

        public static void NaturalSort(this GridControl gridControl, CustomColumnSortEventArgs e, bool unifiedSorting)
        {
            FileModel value1 = e.Row1 as FileModel;
            FileModel value2 = e.Row2 as FileModel;

            if (value1 == null || value2 == null)
                return;

            if (e.Column.UnboundType != UnboundColumnType.Bound)
            {
                int rowHandle1 = gridControl.GetRowHandleByListIndex(e.ListSourceRowIndex1);
                int rowHandle2 = gridControl.GetRowHandleByListIndex(e.ListSourceRowIndex2);

                object cellValue1 = gridControl.GetCellValue(rowHandle1, e.Column);
                object cellValue2 = gridControl.GetCellValue(rowHandle2, e.Column);

                if (cellValue1 is UnboundErrorObject)
                    e.Result = e.SortOrder == ColumnSortOrder.Ascending ? 1 : -1;
                else if (cellValue2 is UnboundErrorObject)
                    e.Result = e.SortOrder == ColumnSortOrder.Ascending ? -1 : 1;
                else
                    e.Result = Comparer.Default.Compare(cellValue1, cellValue2);

                e.Handled = true;
            }

            var unifiedSort = NaturalSort(value1, value2, e.SortOrder, e.Column.FieldName, e.Result, unifiedSorting);
            e.Result = unifiedSort.Result;
            e.Handled = unifiedSort.Handled;
		}

		private static (int Result, bool Handled) NaturalSort(FileModel value1, FileModel value2, ColumnSortOrder sortOrder, string fieldName, int result, bool unifiedSorting)
		{
            if (unifiedSorting || value1.IsDirectory == value2.IsDirectory)
            {
				if (fieldName == nameof(FileModel.Name))
                {
					if (value1.IsDrive == true && value2.IsDrive == true)
						return (value1.FullPath.CompareTo(value2.FullPath), true);

					return (Utilities.NaturalCompare(value1.FullName, value2.FullName), true);
				}

				if (fieldName == nameof(FileModel.ParentName))
					return (Utilities.NaturalCompare(value1.ParentName, value2.ParentName), true);

				return (result, false);
			}

			if (value1.IsDirectory == true && value2.IsDirectory == false)
				return (sortOrder == ColumnSortOrder.Ascending ? -1 : 1, true);
			else if (value2.IsDirectory == true && value1.IsDirectory == false)
				return (sortOrder == ColumnSortOrder.Ascending ? 1 : -1, true);
			else
				return (result, false);
		}
	}
}
