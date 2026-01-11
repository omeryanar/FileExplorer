using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                ClipboardCopyMode = ClipboardCopyMode.Default;

                int startRowHandle = GetRowHandleByVisibleIndex(0);
                int endRowHandle = GetRowHandleByVisibleIndex(VisibleRowCount - 1);

                if (gridColumn == null)
                {
                    if (SelectedItem != null)
                        CopySelectedItemsToClipboard();
                    else
                        CopyRangeToClipboard(startRowHandle, endRowHandle);
                }
                else
                {
                    if (SelectedItem != null)
                    {
                        List<string> values = new List<string>();
                        values.Add(gridColumn.HeaderCaption.ToString());

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

                layout.ApplyToSubFolders = applyToSubFolders;
                layout.LayoutData = layoutStream.ToArray();
            }

            App.Repository.FolderLayouts.Add(layout);
            ShowManageLayoutsDialog();
        }        

        public void LoadFolderLayout(string folderPath)
        {
            if (!String.IsNullOrEmpty(HighlightedText) && LastLoadedFolderLayout != null)
            {
                LoadDefaultLayout();
                return;
            }

            FolderLayout layout = App.Repository.FolderLayouts.FirstOrDefault(x => x.FolderPath.OrdinalEquals(folderPath));
            if (layout == null)
                layout = App.Repository.FolderLayouts.Where(x => x.ApplyToSubFolders && folderPath.OrdinalStartsWith(x.FolderPath)).
                    DefaultIfEmpty().Aggregate((x, y) => x.FolderPath.Length > y.FolderPath.Length ? x : y);

            if (layout != null)
            {
                if (layout.LayoutStream != null && layout != LastLoadedFolderLayout)
                {
                    layout.LayoutStream.Position = 0;
                    RestoreLayoutFromStream(layout.LayoutStream);

                    LastLoadedFolderLayout = layout;
                }
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
            DefaultLayoutStream.Position = 0;
            RestoreLayoutFromStream(DefaultLayoutStream);

            LastLoadedFolderLayout = null;
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

                if (e.NewValue != null)
                {
                    FolderLayout lastLoadedFolderLayout = fileListViewControl.LastLoadedFolderLayout;
                    if (lastLoadedFolderLayout == null)
                        return;

                    string newFolderPath = e.NewValue.ToString();

                    if (newFolderPath.OrdinalEquals(lastLoadedFolderLayout.FolderPath))
                        return;

                    if (newFolderPath.OrdinalStartsWith(lastLoadedFolderLayout.FolderPath) && lastLoadedFolderLayout.ApplyToSubFolders)
                        return;

                    fileListViewControl.LoadDefaultLayout();
                }
            }
        }

        private static void OnHighlightedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridControl gridControl)
                gridControl.View.SearchString = e.NewValue == null ? null : e.NewValue.ToString();
        }

        private Dictionary<string, IList> AutoRestoreItemsDictionary = new Dictionary<string, IList>();

        private Dictionary<string, IList> ManuelRestoreItemsDictionary = new Dictionary<string, IList>();

        private static MemoryStream DefaultLayoutStream;

        private FolderLayout LastLoadedFolderLayout;

        private DispatcherTimer ClickTimer;

        private object NewClickedItem;

        private object OldClickedItem;

        private long FocusTime;
    }

    public class TableViewEx : TableView
    {
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

            if (e.Column.FieldName == nameof(FileModel.Name))
            {
                if (value1.IsDrive == true && value2.IsDrive == true)
                {
                    e.Result = value1.FullPath.CompareTo(value2.FullPath);
                    e.Handled = true;
                }
                else if (unifiedSorting || value1.IsDirectory == value2.IsDirectory)
                {
                    e.Result = Utilities.NaturalCompare(value1.FullName, value2.FullName);
                    e.Handled = true;
                }
            }
            else if (e.Column.FieldName == nameof(FileModel.ParentName))
            {
                if (unifiedSorting || value1.IsDirectory == value2.IsDirectory)
                {
                    e.Result = Utilities.NaturalCompare(value1.ParentName, value2.ParentName);
                    e.Handled = true;
                }
            }
            else if (e.Column.UnboundType != UnboundColumnType.Bound)
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

            if (unifiedSorting)
                return;

            if (value1.IsDirectory == true && value2.IsDirectory == false)
            {
                e.Result = e.SortOrder == ColumnSortOrder.Ascending ? -1 : 1;
                e.Handled = true;
            }
            else if (value2.IsDirectory == true && value1.IsDirectory == false)
            {
                e.Result = e.SortOrder == ColumnSortOrder.Ascending ? 1 : -1;
                e.Handled = true;
            }
        }

        public static void NaturalSort(this GridControl gridControl, CustomColumnSortEventArgs e, bool unifiedSorting)
        {
            FileModel value1 = e.Row1 as FileModel;
            FileModel value2 = e.Row2 as FileModel;

            if (value1 == null || value2 == null)
                return;

            if (e.Column.FieldName == nameof(FileModel.Name))
            {
                if (value1.IsDrive == true && value2.IsDrive == true)
                {
                    e.Result = value1.FullPath.CompareTo(value2.FullPath);
                    e.Handled = true;
                }
                else if (unifiedSorting || value1.IsDirectory == value2.IsDirectory)
                {
                    e.Result = Utilities.NaturalCompare(value1.FullName, value2.FullName);
                    e.Handled = true;
                }
            }
            else if (e.Column.FieldName == nameof(FileModel.ParentName))
            {
                if (unifiedSorting || value1.IsDirectory == value2.IsDirectory)
                {
                    e.Result = Utilities.NaturalCompare(value1.ParentName, value2.ParentName);
                    e.Handled = true;
                }
            }
            else if (e.Column.UnboundType != UnboundColumnType.Bound)
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

            if (unifiedSorting)
                return;

            if (value1.IsDirectory == true && value2.IsDirectory == false)
            {
                e.Result = e.SortOrder == ColumnSortOrder.Ascending ? -1 : 1;
                e.Handled = true;
            }
            else if (value2.IsDirectory == true && value1.IsDirectory == false)
            {
                e.Result = e.SortOrder == ColumnSortOrder.Ascending ? 1 : -1;
                e.Handled = true;
            }
        }
    }
}
