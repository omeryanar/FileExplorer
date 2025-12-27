using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DevExpress.Data;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Mvvm.UI;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Docking;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.TreeList;
using DevExpress.XtraGrid;
using FileExplorer.Controls;
using FileExplorer.Helpers;
using FileExplorer.Model;
using FileExplorer.Properties;
using FileExplorer.ViewModel;

namespace FileExplorer.Core
{
    public class SortOnlyFocusedNodeBehavior : Behavior<TreeListView>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.CustomColumnSort += AssociatedObject_CustomColumnSort;
        }

        private void AssociatedObject_CustomColumnSort(object sender, TreeListCustomColumnSortEventArgs e)
        {
            if (e.Node1.ParentNode != AssociatedObject.FocusedNode)
            {
                int result1 = e.Node1.RowHandle.CompareTo(e.Node2.RowHandle);
                int result2 = e.Node2.RowHandle.CompareTo(e.Node1.RowHandle);

                e.Result = e.SortOrder == ColumnSortOrder.Ascending ? result1 : result2;
                e.Handled = true;
            }
            else if (e.Column.FieldName.Contains("Name"))
            {
                e.Result = Utilities.NaturalCompare(e.Value1.ToString(), e.Value2.ToString());
                e.Handled = true;
            }
        }
    }

    public class FileModelTreeViewBehavior : Behavior<TreeListView>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;

            AssociatedObject.NodeExpanding += AssociatedObject_NodeExpanding;
            AssociatedObject.NodeCollapsing += AssociatedObject_NodeCollapsing;
            AssociatedObject.CustomColumnSort += AssociatedObject_CustomColumnSort;
            AssociatedObject.EndSorting += AssociatedObject_EndSorting;
        }        

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;

            MainWindow = Window.GetWindow(AssociatedObject);
            MainWindow.PreviewMouseLeftButtonDown += MainWindow_PreviewMouseLeftButtonDown;
        }        

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;

            MainWindow.PreviewMouseLeftButtonDown -= MainWindow_PreviewMouseLeftButtonDown;
        }

        private void MainWindow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Settings.Default.ShowCheckBoxes)
                return;

            DependencyObject target = e.OriginalSource as DependencyObject;
            GridViewHitInfoBase hitInfo = AssociatedObject.CalcHitInfo(target);

            if (hitInfo != null && hitInfo.RowHandle >= 0)
            {
                if (target.ParentExists<CheckEdit>())
                {
                    AssociatedObject.DataControl.Focus();

                    object row = AssociatedObject.DataControl.GetRow(hitInfo.RowHandle);

                    if (AssociatedObject.DataControl.SelectedItems.Contains(row))
                        AssociatedObject.DataControl.SelectedItems.Remove(row);
                    else
                        AssociatedObject.DataControl.SelectedItems.Add(row);

                    e.Handled = true;
                }
            }
        }

        private async void AssociatedObject_NodeExpanding(object sender, TreeListNodeAllowEventArgs e)
        {
            SynchronizeFocusedNode(e.Node);

            if (e.Row is FileModel fileModel && fileModel.Content == null)
                await fileModel.EnumerateChildren();
        }

        private void AssociatedObject_NodeCollapsing(object sender, TreeListNodeAllowEventArgs e)
        {
            SynchronizeFocusedNode(e.Node);
        }      

        private void AssociatedObject_CustomColumnSort(object sender, TreeListCustomColumnSortEventArgs e)
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
                else if (Settings.Default.UnifiedSorting || value1.IsDirectory == value2.IsDirectory)
                {
                    e.Result = Utilities.NaturalCompare(value1.FullName, value2.FullName);
                    e.Handled = true;
                }
            }
            else if (e.Column.FieldName == nameof(FileModel.ParentName))
            {
                if (Settings.Default.UnifiedSorting || value1.IsDirectory == value2.IsDirectory)
                {
                    e.Result = Utilities.NaturalCompare(value1.ParentName, value2.ParentName);
                    e.Handled = true;
                }
            }
            else if (e.Column.UnboundType != UnboundColumnType.Bound)
            {
                object nodeValue1 = AssociatedObject.GetNodeValue(e.Node1, e.Column);
                object nodeValue2 = AssociatedObject.GetNodeValue(e.Node2, e.Column);

                if (nodeValue1 is UnboundErrorObject)
                    e.Result = e.SortOrder == ColumnSortOrder.Ascending ? 1 : -1;
                else if (nodeValue2 is UnboundErrorObject)
                    e.Result = e.SortOrder == ColumnSortOrder.Ascending ? 1 : -1;
                else
                    e.Result = Comparer.Default.Compare(nodeValue1, nodeValue2);

                e.Handled = true;                
            }

            if (Settings.Default.UnifiedSorting)
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

        private void AssociatedObject_EndSorting(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject.DataControl.SelectedItem != null)
                AssociatedObject.ScrollIntoView(AssociatedObject.DataControl.SelectedItem);
        }

        private void SynchronizeFocusedNode(TreeListNode node)
        {
            if (AssociatedObject.FocusedNode != node)
            {
                AssociatedObject.FocusedNode = node;
                AssociatedObject.DataControl.SelectedItem = node.Content;
            }
        }

        private Window MainWindow;
    }

    public class FileModelGroupBehavior : Behavior<GridControl>
    {
        const string IntervalFormat = "1 - 1024 {0}";

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.CustomColumnGroup += AssociatedObject_CustomColumnGroup;
            AssociatedObject.CustomGroupDisplayText += AssociatedObject_CustomGroupDisplayText;
        }

        private void AssociatedObject_CustomColumnGroup(object sender, CustomColumnSortEventArgs e)
        {
            if (e.Column.FieldName == nameof(FileModel.Size))
            {
                long value1 = Convert.ToInt64(e.Value1);
                long value2 = Convert.ToInt64(e.Value2);

                int result = Comparer.Default.Compare(value1, value2);
                while (value1 > 0 && value2 > 0)
                {
                    value1 = value1 / 1024;
                    value2 = value2 / 1024;

                    if (value1 == value2)
                        result = 0;
                }

                e.Result = result;
                e.Handled = true;
            }
            else if (e.Column.FieldName == nameof(FileModel.Name))
            {
                string value1 = e.Value1.ToString();
                string value2 = e.Value2.ToString();

                e.Result = String.Compare(value1.Substring(0, 1), value2.Substring(0, 1), StringComparison.OrdinalIgnoreCase);
                if (Char.IsDigit(value1[0]) && Char.IsDigit(value2[0]))
                    e.Result = 0;

                e.Handled = true;
            }
            else if (e.Column.FieldType == typeof(DateTime))
            {
                DateTime value1 = Convert.ToDateTime(e.Value1);
                DateTime value2 = Convert.ToDateTime(e.Value2);

                switch (e.Column.GroupInterval)
                {
                    case ColumnGroupInterval.Date:
                    case ColumnGroupInterval.DateRange:
                        e.Result = Comparer.Default.Compare(value1.Date, value2.Date);
                        e.Handled = true;
                        break;
                    case ColumnGroupInterval.DateMonth:
                        e.Result = value1.Year != value2.Year ? 
                            Comparer.Default.Compare(value1.Year, value2.Year) :
                            Comparer.Default.Compare(value1.Month, value2.Month);
                        e.Handled = true;
                        break;
                    case ColumnGroupInterval.DateYear:
                        e.Result = Comparer.Default.Compare(value1.Year, value2.Year);
                        e.Handled = true;
                        break;
                }
            }
        }

        private void AssociatedObject_CustomGroupDisplayText(object sender, CustomGroupDisplayTextEventArgs e)
        {
            if (e.Column.FieldName == nameof(FileModel.Size))
            {
                long size = Convert.ToInt64(e.Value);
                if (size == 0)
                    e.DisplayText = Properties.Resources.EmptyFiles;
                else if (size < 1024)
                    e.DisplayText = String.Format(IntervalFormat, "Bytes");
                else if (size < 1048576)
                    e.DisplayText = String.Format(IntervalFormat, "KB");
                else if (size < 1073741824)
                    e.DisplayText = String.Format(IntervalFormat, "MB");
                else
                    e.DisplayText = String.Format(IntervalFormat, "GB");
            }
            else if (e.Column.FieldName == nameof(FileModel.Name))
            {  
                if (Char.IsDigit(e.DisplayText[0]))
                    e.DisplayText = "0-9";
                else
                    e.DisplayText = e.DisplayText[0].ToString().ToUpper();
            }
            else if (e.Column.FieldType == typeof(DateTime))
            {
                DateTime date = Convert.ToDateTime(e.Value);

                switch (e.Column.GroupInterval)
                {
                    case ColumnGroupInterval.Date:
                    case ColumnGroupInterval.DateRange:
                        e.DisplayText = date.ToString("dd.MM.yyyy");
                        break;
                    case ColumnGroupInterval.DateMonth:
                        e.DisplayText = date.ToString("MMMM yyyy");
                        break;
                    case ColumnGroupInterval.DateYear:
                        e.DisplayText = date.ToString("yyyy");
                        break;
                }
            }
        } 
    }

    public class FileModelSortBehavior : Behavior<GridControl>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.CustomColumnSort += AssociatedObject_CustomColumnSort;
        }

        private void AssociatedObject_CustomColumnSort(object sender, CustomColumnSortEventArgs e)
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
                else if (Settings.Default.UnifiedSorting || value1.IsDirectory == value2.IsDirectory)
                {
                    e.Result = Utilities.NaturalCompare(value1.FullName, value2.FullName);
                    e.Handled = true;
                }
            }
            else if (e.Column.FieldName == nameof(FileModel.ParentName))
            {
                if (Settings.Default.UnifiedSorting || value1.IsDirectory == value2.IsDirectory)
                {
                    e.Result = Utilities.NaturalCompare(value1.ParentName, value2.ParentName);
                    e.Handled = true;
                }
            }

            if (Settings.Default.UnifiedSorting)
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

    public class SortOrderOnHeaderClickBehavior : Behavior<GridDataViewBase>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ColumnHeaderClick += AssociatedObject_ColumnHeaderClick;
        }

        private void AssociatedObject_ColumnHeaderClick(object sender, ColumnHeaderClickEventArgs e)
        {
            if (e.Column.SortOrder == ColumnSortOrder.None && e.Column.FieldType != typeof(string))
                e.Column.SortOrder = ColumnSortOrder.Ascending;
        }
    }

    public class CustomMenuBehavior : Behavior<PopupMenu>
    {
        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(CustomMenuBehavior));

        public object DefaultCommandParameter
        {
            get => GetValue(DefaultCommandParameterProperty);
            set => SetValue(DefaultCommandParameterProperty, value);
        }
        public static readonly DependencyProperty DefaultCommandParameterProperty = DependencyProperty.Register(nameof(DefaultCommandParameter), typeof(object), typeof(CustomMenuBehavior));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Opening += AssociatedObject_Opening;
        }

        private void AssociatedObject_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                e.Cancel = true;
            else
                ResetCustomMenuItems();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;

            ResetCustomMenuItems();
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
        }

        private void ResetCustomMenuItems()
        {
            List<MenuItemControl> menuItemControls = AssociatedObject.Items.OfType<MenuItemControl>().ToList();
            foreach (MenuItemControl menuItem in menuItemControls)
                AssociatedObject.Items.Remove(menuItem);

            List<MenuSubItemControl> subItems = AssociatedObject.Items.OfType<MenuSubItemControl>().ToList();
            foreach (MenuSubItemControl subItem in subItems)
                AssociatedObject.Items.Remove(subItem);

            int index = 0;
            foreach (Persistence.MenuItem menuItem in App.Repository.MenuItems)
            {
                MenuItemControl menuItemControl = new MenuItemControl
                {
                    DataContext = menuItem,
                    CommandParameter = CommandParameter
                };

                if (CommandParameter is null || (CommandParameter is ICollection collection && collection.Count == 0))
                    menuItemControl.CommandParameter = new List<object> { DefaultCommandParameter };

                if (menuItemControl.CommandParameter is IEnumerable enumerable && enumerable.OfType<FileModel>().Any(x => x.IsRoot))
                    return;

                if (String.IsNullOrEmpty(menuItem.GroupName))
                {
                    AssociatedObject.Items.Insert(index, menuItemControl);
                    index++;
                }
                else
                {
                    MenuSubItemControl subItem = AssociatedObject.Items.OfType<MenuSubItemControl>().FirstOrDefault(x => x.Content.ToString() == menuItem.GroupName);
                    if (subItem == null)
                    {
                        subItem = new MenuSubItemControl
                        {
                            Content = menuItem.GroupName,
                            Glyph = menuItem.Icon
                        };

                        AssociatedObject.Items.Insert(index, subItem);
                        index++;
                    }

                    subItem.Items.Add(menuItemControl);
                }
            }
        }
    }

    public class NativeContextMenuBehavior : Behavior<DataControlBase>
    {
        public FileModel CurrentFolder
        {
            get { return (FileModel)GetValue(CurrentFolderProperty); }
            set { SetValue(CurrentFolderProperty, value); }
        }
        public static readonly DependencyProperty CurrentFolderProperty = DependencyProperty.Register(nameof(CurrentFolder), typeof(FileModel), typeof(NativeContextMenuBehavior));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseRightButtonDown += AssociatedObject_MouseRightButtonDown;
        }

        private void AssociatedObject_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                IList<string> filePaths = new List<string>();
                Point point = AssociatedObject.PointToScreen(e.GetPosition(AssociatedObject));

                if (AssociatedObject is FileTreeViewControl fileTree)
                {
                    FileModel fileModel = fileTree.ClickedItem as FileModel;
                    if (fileModel == null || fileModel.IsRoot)
                        return;

                    filePaths.Add(fileModel.FullPath);
                }
                else if (AssociatedObject is FileListViewControl fileList)
                {
                    int rowHandle = fileList.View.GetRowHandleByMouseEventArgs(e);
                    if (rowHandle >= 0)
                    {
                        FileModel clickedItem = AssociatedObject.GetRow(rowHandle) as FileModel;
                        if (clickedItem != null && !fileList.SelectedItems.Contains(clickedItem))
                            fileList.SelectedItems.Add(clickedItem);

                        foreach (FileModel fileModel in fileList.SelectedItems.OfType<FileModel>())
                            filePaths.Add(fileModel.FullPath);
                    }
                    else
                    {
                        if (fileList.SelectedItems.Count > 1)
                        {
                            foreach (FileModel fileModel in fileList.SelectedItems.OfType<FileModel>())
                                filePaths.Add(fileModel.FullPath);
                        }
                        else if (CurrentFolder?.IsRoot == false)
                            filePaths.Add(CurrentFolder.FullPath);
                    }
                }

                if (filePaths.Count() > 0)
                    ContextMenuHelper.ShowNativeContextMenu(filePaths, point);
            }
        }
    }

    public class CustomColumnBehavior : Behavior<GridDataViewBase>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ShowGridMenu += AssociatedObject_ShowGridMenu;
        }

        private GridControl Grid => AssociatedObject.DataControl as GridControl;

        private void AssociatedObject_ShowGridMenu(object sender, GridMenuEventArgs e)
        {
            if (e.MenuType == GridMenuType.Column)
            {
                e.Customizations.Add(new BarItemLinkSeparator());
                e.Customizations.Add(new BarButtonItem
                {
                    Content = Properties.Resources.AddCustomColumn,
                    Glyph = AddCustomColumnItemGlyph,
                    Command = new DelegateCommand(() =>
                    {
                        GridColumn gridColumn = new GridColumn
                        {
                            Header = Properties.Resources.CustomColumn,
                            FieldName = Guid.NewGuid().ToString(),
                            UnboundType = UnboundColumnType.String,
                            AllowUnboundExpressionEditor = true,
                            SortMode = ColumnSortMode.Custom
                        };
                        Grid.Columns.Add(gridColumn);

                        IDialogService dialogService = AssociatedObject.DataContext.GetService<IDialogService>();
                        if (dialogService != null)
                            dialogService.ShowDialog(MessageButton.OK, Properties.Resources.CustomColumn, "CustomColumnView", gridColumn);
                    })
                });

                if (e.MenuInfo.Column != null && e.MenuInfo.Column.AllowUnboundExpressionEditor == true)
                {
                    e.Customizations.Add(new BarButtonItem
                    {
                        Content = Properties.Resources.RemoveCustomColumn,
                        Glyph = RemoveCustomColumnItemGlyph,
                        Command = new DelegateCommand(() =>
                        {
                            Grid.Columns.Remove(e.MenuInfo.Column as GridColumn);
                        })
                    });

                    e.Customizations.Add(new BarButtonItem
                    {
                        Content = Properties.Resources.EditCustomColumn,
                        Glyph = EditCustomColumnItemGlyph,
                        Command = new DelegateCommand(() =>
                        {
                            IDialogService dialogService = AssociatedObject.DataContext.GetService<IDialogService>();
                            if (dialogService != null)
                                dialogService.ShowDialog(MessageButton.OK, Properties.Resources.CustomColumn, "CustomColumnView", e.MenuInfo.Column);
                        })
                    });
                }
            }
        }

        private static readonly ImageSource AddCustomColumnItemGlyph = WpfSvgRenderer.CreateImageSource(new Uri("pack://application:,,,/FileExplorer;component/Assets/SVG/AddCustomColumn.svg"));

        private static readonly ImageSource EditCustomColumnItemGlyph = WpfSvgRenderer.CreateImageSource(new Uri("pack://application:,,,/FileExplorer;component/Assets/SVG/EditCustomColumn.svg"));

        private static readonly ImageSource RemoveCustomColumnItemGlyph = WpfSvgRenderer.CreateImageSource(new Uri("pack://application:,,,/FileExplorer;component/Assets/SVG/RemoveCustomColumn.svg"));        
    }

    public class AllowDragDropBehavior : Behavior<GridDataViewBase>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;

            MainWindow = Window.GetWindow(AssociatedObject);

            MainWindow.PreviewMouseLeftButtonDown += MainWindow_PreviewMouseLeftButtonDown;
            MainWindow.PreviewMouseLeftButtonUp += MainWindow_PreviewMouseLeftButtonUp;
            MainWindow.PreviewDragEnter += MainWindow_PreviewDragOver;
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;

            MainWindow.PreviewMouseLeftButtonDown -= MainWindow_PreviewMouseLeftButtonDown;
            MainWindow.PreviewMouseLeftButtonUp -= MainWindow_PreviewMouseLeftButtonUp;
            MainWindow.PreviewDragEnter -= MainWindow_PreviewDragOver;
        }

        private void MainWindow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject target = e.OriginalSource as DependencyObject;
            GridViewHitInfoBase hitInfo = AssociatedObject.CalcHitInfo(target);

            if (hitInfo != null && hitInfo.RowHandle >= 0)
            {
                if ((hitInfo.Column == null || hitInfo.Column.FieldName == nameof(FileModel.Name)) && (target is TextBlock || target is Image))
                {
                    AllowDragDrop();
                    return;
                }

                object row = AssociatedObject.DataControl.GetRow(hitInfo.RowHandle);

                if (target.ParentExists<CheckEdit>())
                {
                    AssociatedObject.DataControl.Focus();

                    if (AssociatedObject.DataControl.SelectedItems.Contains(row))
                        AssociatedObject.DataControl.SelectedItems.Remove(row);
                    else
                        AssociatedObject.DataControl.SelectedItems.Add(row);

                    e.Handled = true;
                }

                if (AssociatedObject.DataControl.SelectedItems.Contains(row))
                    AllowDragDrop();
                else
                    ShowSelectionRectangle();
            }
            else
            {
                if (hitInfo != null && hitInfo.IsDataArea)
                    ShowSelectionRectangle();
                else
                    AllowDragDrop();
            }
        }

        private void MainWindow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AllowDragDrop();
        }

        private void MainWindow_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                AllowDragDrop();
        }

        private void AllowDragDrop()
        {
            AssociatedObject.AllowDragDrop = true;
            AssociatedObject.ShowSelectionRectangle = false;
        }

        private void ShowSelectionRectangle()
        {
            AssociatedObject.AllowDragDrop = false;
            AssociatedObject.ShowSelectionRectangle = true;
        }

        private Window MainWindow;
    }

    public class DragDropFilesBehavior : Behavior<GridDataViewBase>
    {
        public string CurrentFolderPath
        {
            get { return (string)GetValue(CurrentFolderPathProperty); }
            set { SetValue(CurrentFolderPathProperty, value); }
        }
        public static readonly DependencyProperty CurrentFolderPathProperty = DependencyProperty.Register(nameof(CurrentFolderPath), typeof(string), typeof(DragDropFilesBehavior));

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.StartRecordDrag += AssociatedObject_StartRecordDrag;
            AssociatedObject.ContinueRecordDrag += AssociatedObject_ContinueRecordDrag;
            AssociatedObject.DragRecordOver += AssociatedObject_DragRecordOver;
            AssociatedObject.DropRecord += AssociatedObject_DropRecord;
            AssociatedObject.CompleteRecordDragDrop += AssociatedObject_CompleteRecordDragDrop;
        }

        private void AssociatedObject_StartRecordDrag(object sender, StartRecordDragEventArgs e)
        {
            try
            {
                IEnumerable<FileModel> files = e.Records.OfType<FileModel>();
                if (files == null || files.Count() == 0)
                    return;

                e.Data = Utilities.CreateDataObject(files.Select(x => x.FullPath).ToArray());
                e.AllowDrag = files.All(x => !x.IsRoot && !x.IsDrive);
            }
            finally
            {
                e.Handled = true;
            }
        }

        private void AssociatedObject_ContinueRecordDrag(object sender, ContinueRecordDragEventArgs e)
        {
            e.Action = e.EscapePressed ? DragAction.Cancel : DragAction.Continue;
        }

        private void AssociatedObject_DragRecordOver(object sender, DragRecordOverEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    FileModel target = e.TargetRecord as FileModel;
                    string targetPath = target?.IsDirectory == true ? target.FullPath : CurrentFolderPath;

                    if (targetPath == null)
                        return;

                    IEnumerable<String> filePaths = e.Data.GetData(DataFormats.FileDrop) as IEnumerable<String>;
                    if (filePaths == null || filePaths.Count() == 0)
                        return;

                    bool isTargetRoot = target?.IsRoot == true;
                    bool isTargetFile = target?.IsDirectory == false;
                    bool isTargetSubFolder = filePaths.Any(x => targetPath?.OrdinalStartsWith(x + FileSystemHelper.Separator) == true);
                    bool isTargetSameParent = filePaths.Any(x => targetPath?.OrdinalEquals(x) == true || targetPath?.OrdinalEquals(FileSystemHelper.GetParentFolderPath(x)) == true);

                    bool notExists = filePaths.Any(x => !File.Exists(x));
                    bool sameDrive = Path.GetPathRoot(targetPath) == Path.GetPathRoot(filePaths.First());
                    bool copy = e.KeyStates.HasFlag(DragDropKeyStates.ControlKey) || notExists ||
                        (sameDrive && Settings.Default.DragDropSameDrive == 1) ||
                        (!sameDrive && Settings.Default.DragDropDifferentDrive == 0);

                    DragDropEffects allowedEffect = isTargetRoot || isTargetFile || isTargetSubFolder || isTargetSameParent ? DragDropEffects.None :
                        copy ? DragDropEffects.Copy : DragDropEffects.Move;

                    if (targetPath == CurrentFolderPath)
                    {
                        e.Effects = allowedEffect;
                        e.DropPosition = DropPosition.Append;
                    }
                    else
                    {
                        e.Effects = allowedEffect;
                        e.DropPosition = DropPosition.Inside;
                    }
                }
            }
            finally
            {
                e.Handled = true;
            }
        }

        private void AssociatedObject_DropRecord(object sender, DropRecordEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    FileModel target = e.TargetRecord as FileModel;
                    string targetPath = target?.IsDirectory == true ? target.FullPath : CurrentFolderPath;
                    IEnumerable<String> filePaths = e.Data.GetData(DataFormats.FileDrop) as IEnumerable<String>;

                    bool sameDrive = Path.GetPathRoot(targetPath) == Path.GetPathRoot(filePaths.First());
                    bool copy = e.KeyStates.HasFlag(DragDropKeyStates.ControlKey) ||
                        (sameDrive && Settings.Default.DragDropSameDrive == 1) ||
                        (!sameDrive && Settings.Default.DragDropDifferentDrive == 0);

                    if ((copy && Settings.Default.DragDropConfirmCopy) || (!copy && Settings.Default.DragDropConfirmMove))
                    {
                        MessageViewModel viewModel = ViewModelSource.Create<MessageViewModel>();
                        viewModel.Icon = IconType.Question;

                        if (filePaths.Count() == 1)
                        {
                            viewModel.Title = copy ? Properties.Resources.ConfirmCopy : Properties.Resources.ConfirmMove;
                            viewModel.Content = String.Format(copy ? Properties.Resources.ConfirmCopyMessage : Properties.Resources.ConfirmMoveMessage, filePaths.First(), targetPath);
                        }
                        else
                        {
                            viewModel.Title = copy ? Properties.Resources.ConfirmCopy : Properties.Resources.ConfirmMove;
                            viewModel.Content = String.Format(copy ? Properties.Resources.ConfirmMultipleCopy : Properties.Resources.ConfirmMultipleMove, targetPath);
                            viewModel.Details = String.Join(Environment.NewLine, filePaths);
                        }

                        IDialogService dialogService = AssociatedObject.DataContext.GetService<IDialogService>();
                        MessageResult result = dialogService.ShowDialog(MessageButton.YesNo, viewModel.Title, "MessageView", viewModel);
                        if (result == MessageResult.No)
                            return;
                    }

                    if (copy)
                        Utilities.CopyFiles(filePaths, targetPath);
                    else
                        Utilities.MoveFiles(filePaths, targetPath);
                }
            }
            finally
            {
                e.Handled = true;
            }
        }

        private void AssociatedObject_CompleteRecordDragDrop(object sender, CompleteRecordDragDropEventArgs e)
        {
            e.Handled = true;
        }
    }

    public class MouseEventToCommand : EventToCommand
    {
        public MouseButton MouseButton
        {
            get { return (MouseButton)GetValue(MouseButtonProperty); }
            set { SetValue(MouseButtonProperty, value); }
        }
        public static readonly DependencyProperty MouseButtonProperty =
            DependencyProperty.Register(nameof(MouseButton), typeof(MouseButton), typeof(MouseEventToCommand));

        public bool ExecuteCommandWhenParameterNull
        {
            get { return (bool)GetValue(ExecuteCommandWhenParameterNullProperty); }
            set { SetValue(ExecuteCommandWhenParameterNullProperty, value); }
        }
        public static readonly DependencyProperty ExecuteCommandWhenParameterNullProperty =
            DependencyProperty.Register(nameof(ExecuteCommandWhenParameterNull), typeof(bool), typeof(MouseEventToCommand));

        protected override void OnEvent(object sender, object eventArgs)
        {
            if (eventArgs is MouseButtonEventArgs e)
            {
                if (e.ChangedButton == MouseButton && (ModifierKeys == null || Keyboard.Modifiers.HasFlag(ModifierKeys)))
                {
                    e.Handled = MarkRoutedEventsAsHandled;

                    if (PassEventArgsToCommand == true && EventArgsConverter != null)
                        CommandParameter = EventArgsConverter.Convert(sender, eventArgs);
                    
                    if (ExecuteCommandWhenParameterNull || CommandParameter != null)
                        Command?.Execute(CommandParameter);
                }
            }
        }
    }

    public class WindowKeyToCommand : Behavior<FrameworkElement>
    {
        public KeyGesture KeyGesture
        {
            get { return (KeyGesture)GetValue(KeyGestureProperty); }
            set { SetValue(KeyGestureProperty, value); }
        }
        public static readonly DependencyProperty KeyGestureProperty =
            DependencyProperty.Register(nameof(KeyGesture), typeof(KeyGesture), typeof(WindowKeyToCommand));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(WindowKeyToCommand));

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(WindowKeyToCommand));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;

            MainWindow = Window.GetWindow(AssociatedObject);
            MainWindow.KeyDown += MainWindow_KeyDown;
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;

            MainWindow.KeyDown -= MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (KeyGesture == null)
                return;

            if (e.RealKey() == KeyGesture.Key && Keyboard.Modifiers.HasFlag(KeyGesture.Modifiers))
                Command?.Execute(CommandParameter);
        }

        private Window MainWindow;
    }
}
