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
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Grid;
using FileExplorer.Core;
using FileExplorer.Helpers;
using FileExplorer.Model;
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
            int startRowHandle = GetRowHandleByVisibleIndex(0);
            int endRowHandle = GetRowHandleByVisibleIndex(VisibleRowCount - 1);

            if (gridColumn == null)
            {
                if (SelectedItem != null)
                    CopySelectedItemsToClipboard();
                else
                    CopyRangeToClipboard(startRowHandle, endRowHandle);

                return;
            }
            
            if (View is TableView tableView)
                tableView.CopyCellsToClipboard(startRowHandle, gridColumn, endRowHandle, gridColumn);
            else if (View is TreeListView treeView)
                treeView.CopyCellsToClipboard(startRowHandle, gridColumn, endRowHandle, gridColumn);
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
            if (!Path.IsPathRooted(folderPath) || !String.IsNullOrEmpty(HighlightedText))
            {
                if (LastLoadedLayoutFolder != null)
                    LoadDefaultLayout();

                return;
            }

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
            else if (LastLoadedLayoutFolder != null)
                LoadDefaultLayout();
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

        private static void OnHighlightedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridControl gridControl)
                gridControl.View.SearchString = e.NewValue == null ? null : e.NewValue.ToString();
        }

        private static MemoryStream DefaultLayoutStream;

        private string LastLoadedLayoutFolder;

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

            bool refresh = false;
            if (fileModel.Files == null)
            {
                refresh = true;
                fileModel.Files = await FileSystemHelper.GetFiles(fileModel);
            }
            if (fileModel.Folders == null)
            {
                refresh = true;
                fileModel.Folders = await FileSystemHelper.GetFolders(fileModel);
            }

            if (refresh)
                fileModel.Content = new FileModelReadOnlyCollection(fileModel.Folders, fileModel.Files);

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
}
