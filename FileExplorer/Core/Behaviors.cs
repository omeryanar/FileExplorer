using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DevExpress.Data;
using DevExpress.Data.Filtering;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Mvvm.UI;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Docking;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Helpers;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.TreeList;
using DevExpress.XtraGrid;
using FileExplorer.Controls;
using FileExplorer.Helpers;
using FileExplorer.Model;
using FileExplorer.Native;
using FileExplorer.Properties;
using FileExplorer.View;
using FileExplorer.ViewModel;

namespace FileExplorer.Core
{
    public class TextHighlightingBehavior : Behavior<TextEdit>
    {
        public string HighlightedText
        {
            get { return (string)GetValue(HighlightedTextProperty); }
            set { SetValue(HighlightedTextProperty, value); }
        }
        public static readonly DependencyProperty HighlightedTextProperty = DependencyProperty.Register(nameof(HighlightedText), typeof(string), typeof(TextHighlightingBehavior),
            new PropertyMetadata(String.Empty, (obj, e) => { (obj as TextHighlightingBehavior).UpdateText(); }));

        protected void UpdateText()
        {
            if (AssociatedObject.EditMode != EditMode.InplaceInactive)
                return;

            BaseEditHelper.UpdateHighlightingText(AssociatedObject, new TextHighlightingProperties(HighlightedText, FilterCondition.Contains));
        }
    }

    public class DynamicTreeLoadBehavior : Behavior<TreeListView>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.AllowRecreateNodesOnEndDataUpdate = false;

            AssociatedObject.NodeExpanded += AssociatedObject_NodeExpanded;
            AssociatedObject.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;

            AssociatedObject.NodeExpanding += (s, e) => { e.Allow = !IsUpdating; };
            AssociatedObject.NodeCollapsing += (s, e) => { e.Allow = !IsUpdating; };

            DataControlReferences.Add(new WeakReference<DataControlBase>(AssociatedObject.DataControl));
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
        }

        private async void AssociatedObject_NodeExpanded(object sender, TreeListNodeEventArgs e)
        {
            if (!IsUpdating && e.Node != AssociatedObject.FocusedNode && e.Node.Content is FileModel fileModel)
                await UpdateSubFolders(fileModel);
        }

        private async void DataControl_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            if (!IsUpdating && e.NewItem is FileModel fileModel)
                await UpdateSubFolders(fileModel);

            if (AssociatedObject.FocusedNode != null)
                AssociatedObject.FocusedNode.IsExpanded = true;
        }

        private static async Task UpdateSubFolders(FileModel fileModel)
        {
            if (fileModel.Folders == null)
            {
                try
                {
                    BeginDataUpdate();
                    fileModel.Folders = await FileSystemHelper.GetFolders(fileModel);
                }
                finally
                {
                    EndDataUpdate();
                }
            }

            if (fileModel.Folders.Any(x => x.Folders == null))
                UpdateSubFolders(fileModel.Folders);
        }

        private static async void UpdateSubFolders(FileModelCollection collection)
        {
            Dictionary<FileModel, FileModelCollection> folders = new Dictionary<FileModel, FileModelCollection>();

            foreach (FileModel fileModel in collection)
            {
                if (fileModel.Folders == null)
                    folders.Add(fileModel, await FileSystemHelper.GetFolders(fileModel));
            }

            try
            {                
                BeginDataUpdate();

                foreach (FileModel fileModel in folders.Keys)
                    fileModel.Folders = folders[fileModel];
            }
            finally
            {
                EndDataUpdate();                
            }
        }        

        private static void BeginDataUpdate()
        {
            IsUpdating = true;

            List<WeakReference<DataControlBase>> deadReferences = new List<WeakReference<DataControlBase>>();
            foreach (WeakReference<DataControlBase> reference in DataControlReferences)
            {
                if (reference.TryGetTarget(out DataControlBase dataControl))
                    dataControl.BeginDataUpdate();
                else
                    deadReferences.Add(reference);
            }

            if (deadReferences.Count > 0)
                deadReferences.ForEach(x => DataControlReferences.Remove(x));
        }

        private static void EndDataUpdate()
        {
            List<WeakReference<DataControlBase>> deadReferences = new List<WeakReference<DataControlBase>>();
            foreach (WeakReference<DataControlBase> reference in DataControlReferences)
            {
                if (reference.TryGetTarget(out DataControlBase dataControl))
                    dataControl.EndDataUpdate();
                else
                    deadReferences.Add(reference);
            }

            if (deadReferences.Count > 0)
                deadReferences.ForEach(x => DataControlReferences.Remove(x));

            IsUpdating = false;
        }

        private static readonly List<WeakReference<DataControlBase>> DataControlReferences = new List<WeakReference<DataControlBase>>();

        private static bool IsUpdating = false;
    }

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
                e.Result = SafeNativeMethods.NaturalCompare(e.Value1.ToString(), e.Value2.ToString());
                e.Handled = true;
            }
        }
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
            if (e.Column.FieldName == "Size")
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
            else if (e.Column.FieldName == "Name")
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
            if (e.Column.FieldName == "Size")
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
            else if (e.Column.FieldName == "Name")
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
            FileModel value1 = AssociatedObject.GetRowByListIndex(e.ListSourceRowIndex1) as FileModel;
            FileModel value2 = AssociatedObject.GetRowByListIndex(e.ListSourceRowIndex2) as FileModel;

            if (value1 == null || value2 == null)
                return;

            if (e.Column.FieldName == "Name")
            {
                if (value1.IsDrive == true && value2.IsDrive == true)
                {
                    e.Result = value1.FullPath.CompareTo(value2.FullPath);
                    e.Handled = true;
                }
                else if (value1.IsDirectory == value2.IsDirectory)
                {
                    e.Result = SafeNativeMethods.NaturalCompare(value1.FullName, value2.FullName);
                    e.Handled = true;
                }
            }
            else if (e.Column.FieldName == "ParentName")
            {
                if (value1.IsDirectory == value2.IsDirectory)
                {
                    e.Result = SafeNativeMethods.NaturalCompare(value1.ParentName, value2.ParentName);
                    e.Handled = true;
                }
            }

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

    public class RowClickBehavior : Behavior<GridControl>
    {
        public ICommand SecondClickCommand
        {
            get => (ICommand)GetValue(SecondClickCommandProperty);
            set => SetValue(SecondClickCommandProperty, value);
        }
        public static readonly DependencyProperty SecondClickCommandProperty = DependencyProperty.Register(nameof(SecondClickCommand), typeof(ICommand), typeof(RowClickBehavior));

        public ICommand DoubleClickCommand
        {
            get => (ICommand)GetValue(DoubleClickCommandProperty);
            set => SetValue(DoubleClickCommandProperty, value);
        }
        public static readonly DependencyProperty DoubleClickCommandProperty = DependencyProperty.Register(nameof(DoubleClickCommand), typeof(ICommand), typeof(RowClickBehavior));

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(RowClickBehavior));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;

            AssociatedObject.SelectionChanged += (s, e) => 
            {
                if (AssociatedObject.SelectedItems.Count != 1)
                    Reset();
            };

            AssociatedObject.LostFocus += (s, e) =>
            {
                if (e.Source == e.OriginalSource)
                    Reset();
            };

            int doubleClickTime = System.Windows.Forms.SystemInformation.DoubleClickTime;
            ClickTimer.Interval = TimeSpan.FromMilliseconds(doubleClickTime);
            ClickTimer.Tick += (s, e) => { ExecuteCommand(SecondClickCommand); };
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;

            AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObject_PreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseLeftButtonUp += AssociatedObject_PreviewMouseLeftButtonUp;
        }

        private void AssociatedObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject target = e.OriginalSource as DependencyObject;
            GridViewHitInfoBase hitInfo = AssociatedObject.View.CalcHitInfo(target);

            if (e.ClickCount == 2)
            {
                Reset();

                if (hitInfo != null && hitInfo.RowHandle >= 0)
                    ExecuteCommand(DoubleClickCommand);
            }
            else
            {
                if (hitInfo != null && hitInfo.RowHandle >= 0)
                {
                    object row = AssociatedObject.GetRow(hitInfo.RowHandle);
                    SecondClickedItem = FirstClickedItem;
                    FirstClickedItem = row;
                }
                else
                {
                    SecondClickedItem = FirstClickedItem;
                    FirstClickedItem = null;
                }
            }
        }

        private void AssociatedObject_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (FirstClickedItem != null && SecondClickedItem != null && FirstClickedItem == SecondClickedItem)
                ClickTimer.Start();
        }

        private void Reset()
        {
            ClickTimer.Stop();

            FirstClickedItem = null;
            SecondClickedItem = null;
        }

        private void ExecuteCommand(ICommand command)
        {
            ClickTimer.Stop();
            command?.Execute(CommandParameter);
        }

        private object FirstClickedItem;

        private object SecondClickedItem;

        private DispatcherTimer ClickTimer = new DispatcherTimer();
    }

    public class CustomMenuBehavior : Behavior<PopupMenu>
    {
        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(CustomMenuBehavior));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;

            ResetCustomMenuItems();
            App.Repository.MenuItems.ItemUpdated += ResetEventHandler;
            App.Repository.MenuItems.CollectionChanged += ResetEventHandler;
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;

            App.Repository.MenuItems.ItemUpdated -= ResetEventHandler;
            App.Repository.MenuItems.CollectionChanged -= ResetEventHandler;
        }

        private void ResetEventHandler(object sender, object e)
        {
            ResetCustomMenuItems();
        }

        private void ResetCustomMenuItems()
        {
            List<MenuItemControl> menuItemControls = AssociatedObject.Items.OfType<MenuItemControl>().ToList();
            foreach (MenuItemControl menuItem in menuItemControls)
                AssociatedObject.Items.Remove(menuItem);

            List<MenuSubItemControl> subItems = AssociatedObject.Items.OfType<MenuSubItemControl>().ToList();
            foreach (MenuSubItemControl subItem in subItems)
                AssociatedObject.Items.Remove(subItem);

            if (AddRemoveMenuItem == null)
            {
                AddRemoveMenuItem = new BarButtonItem
                {
                    Content = Properties.Resources.AddRemoveMenuItems,
                    Glyph = AddRemoveMenuItemGlyph,
                    Command = new DelegateCommand(() =>
                    {
                        CustomMenuView customMenuView = new CustomMenuView
                        {
                            DataContext = App.Repository.MenuItems,
                            Owner = Window.GetWindow(AssociatedObject)
                        };
                        customMenuView.ShowDialog();
                    })
                };
            }
            else
                AssociatedObject.Items.Remove(AddRemoveMenuItem);

            if (BarItemSeparator == null)
                BarItemSeparator = new BarItemSeparator();
            else
                AssociatedObject.Items.Remove(BarItemSeparator);

            int index = 0;
            foreach (Persistence.MenuItem menuItem in App.Repository.MenuItems)
            {
                MenuItemControl menuItemControl = new MenuItemControl
                {
                    DataContext = menuItem,
                    CommandParameter = CommandParameter
                };

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

            AssociatedObject.Items.Insert(index, AddRemoveMenuItem);
            AssociatedObject.Items.Insert(index + 1, BarItemSeparator);
        }

        private BarButtonItem AddRemoveMenuItem;

        private BarItemSeparator BarItemSeparator;

        private static readonly BitmapImage AddRemoveMenuItemGlyph = new BitmapImage(new Uri("pack://application:,,,/FileExplorer;component/Assets/Images/Menu16.png"));
    }

    public class CustomColumnBehavior : Behavior<TableView>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ShowGridMenu += AssociatedObject_ShowGridMenu;
        }

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
                            AllowUnboundExpressionEditor = true
                        };
                        AssociatedObject.Grid.Columns.Add(gridColumn);

                        CustomColumnView customColumnView = new CustomColumnView
                        {
                            DataContext = gridColumn,
                            Owner = Window.GetWindow(AssociatedObject)
                        };
                        customColumnView.ShowDialog();
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
                            AssociatedObject.Grid.Columns.Remove(e.MenuInfo.Column as GridColumn);
                        })
                    });

                    e.Customizations.Add(new BarButtonItem
                    {
                        Content = Properties.Resources.EditCustomColumn,
                        Glyph = EditCustomColumnItemGlyph,
                        Command = new DelegateCommand(() =>
                        {
                            CustomColumnView customColumnView = new CustomColumnView
                            {
                                DataContext = e.MenuInfo.Column,
                                Owner = Window.GetWindow(AssociatedObject)
                            };
                            customColumnView.ShowDialog();
                        })
                    });
                }
            }
        }

        private static readonly BitmapImage AddCustomColumnItemGlyph = new BitmapImage(new Uri("pack://application:,,,/FileExplorer;component/Assets/Images/AddCustomColumn16.png"));

        private static readonly BitmapImage EditCustomColumnItemGlyph = new BitmapImage(new Uri("pack://application:,,,/FileExplorer;component/Assets/Images/EditCustomColumn16.png"));

        private static readonly BitmapImage RemoveCustomColumnItemGlyph = new BitmapImage(new Uri("pack://application:,,,/FileExplorer;component/Assets/Images/RemoveCustomColumn16.png"));        
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
                if ((hitInfo.Column == null || hitInfo.Column.FieldName == "Name") && (target is TextBlock || target is Image))
                {
                    AllowDragDrop();
                    return;
                }

                object row = AssociatedObject.DataControl.GetRow(hitInfo.RowHandle);

                if (target.ParentExists<CheckEdit>())
                {
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
                AssociatedObject.DataControl.CurrentItem = null;
                ShowSelectionRectangle();
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
                    bool isTargetSubFolder = filePaths.Any(x => targetPath?.OrdinalStartsWith(x + FileSystemHelper.Separator) == true);
                    bool isTargetSameParent = filePaths.Any(x => targetPath?.OrdinalEquals(x) == true || targetPath?.OrdinalEquals(FileSystemHelper.GetParentFolderPath(x)) == true);

                    bool notExists = filePaths.Any(x => !File.Exists(x));
                    bool sameDrive = Path.GetPathRoot(targetPath) == Path.GetPathRoot(filePaths.First());
                    bool copy = e.KeyStates.HasFlag(DragDropKeyStates.ControlKey) || notExists ||
                        (sameDrive && Settings.Default.DragDropSameDrive == 1) ||
                        (!sameDrive && Settings.Default.DragDropDifferentDrive == 0);

                    DragDropEffects allowedEffect = isTargetRoot || isTargetSubFolder || isTargetSameParent ? DragDropEffects.None :
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
}
