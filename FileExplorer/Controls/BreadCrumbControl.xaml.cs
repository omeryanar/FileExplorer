using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Validation.Native;
using DevExpress.XtraEditors.DXErrorProvider;
using FileExplorer.Helpers;
using FileExplorer.Model;

namespace FileExplorer.Controls
{
    public partial class BreadCrumbControl : ComboBoxEdit
    {
        public object CurrentItem
        {
            get { return GetValue(CurrentItemProperty); }
            set { SetValue(CurrentItemProperty, value); }
        }
        public static readonly DependencyProperty CurrentItemProperty =
            DependencyProperty.Register(nameof(CurrentItem), typeof(object), typeof(BreadCrumbControl), new PropertyMetadata(null, OnCurrentItemChanged));


        public bool ShowHiddenItems
        {
            get { return (bool)GetValue(ShowHiddenItemsProperty); }
            set { SetValue(ShowHiddenItemsProperty, value); }
        }
        public static readonly DependencyProperty ShowHiddenItemsProperty = DependencyProperty.Register(nameof(ShowHiddenItems), typeof(bool), typeof(BreadCrumbControl));

        public bool ShowSystemItems
        {
            get { return (bool)GetValue(ShowSystemItemsProperty); }
            set { SetValue(ShowSystemItemsProperty, value); }
        }
        public static readonly DependencyProperty ShowSystemItemsProperty = DependencyProperty.Register(nameof(ShowSystemItems), typeof(bool), typeof(BreadCrumbControl));

        public ICommand ItemClickCommand
        {
            get => (ICommand)GetValue(ItemClickCommandProperty);
            set => SetValue(ItemClickCommandProperty, value);
        }
        public static readonly DependencyProperty ItemClickCommandProperty = DependencyProperty.Register(nameof(ItemClickCommand), typeof(ICommand), typeof(BreadCrumbControl));

        public ICommand ItemMiddleClickCommand
        {
            get => (ICommand)GetValue(ItemMiddleClickCommandProperty);
            set => SetValue(ItemMiddleClickCommandProperty, value);
        }
        public static readonly DependencyProperty ItemMiddleClickCommandProperty = DependencyProperty.Register(nameof(ItemMiddleClickCommand), typeof(ICommand), typeof(BreadCrumbControl));

        public ICommand NavigatePathCommand
        {
            get => (ICommand)GetValue(NavigatePathCommandProperty);
            set => SetValue(NavigatePathCommandProperty, value);
        }
        public static readonly DependencyProperty NavigatePathCommandProperty = DependencyProperty.Register(nameof(NavigatePathCommand), typeof(ICommand), typeof(BreadCrumbControl));

        #region ReadonlyProperties

        public IEnumerable BreadCrumbs
        {
            get { return (IEnumerable)GetValue(BreadCrumbsProperty); }
            protected set { SetValue(BreadCrumbsPropertyKey, value); }
        }
        private static readonly DependencyPropertyKey BreadCrumbsPropertyKey = DependencyProperty.RegisterReadOnly
            (nameof(BreadCrumbs), typeof(IEnumerable), typeof(BreadCrumbControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BreadCrumbsProperty = BreadCrumbsPropertyKey.DependencyProperty;

        public Style BreadCrumbButtonStyle
        { 
            get
            {
                if (breadCrumbButtonStyle == null)
                    breadCrumbButtonStyle = FindResource("BreadCrumbButtonStyle") as Style;

                return breadCrumbButtonStyle;
            }
        }
        private static Style breadCrumbButtonStyle;

        #endregion

        public BreadCrumbControl()
        {
            InitializeComponent();

            Validate += (s, e) =>
            {
                if (IsTextEditable == true && e.Value != null)
                {
                    string path = e.Value.ToString();

                    if (e.UpdateSource == UpdateEditorSource.ValueChanging || e.UpdateSource == UpdateEditorSource.EnterKeyPressed)
                    {
                        if (FileSystemHelper.DirectoryExists(path) || FileSystemHelper.NetworkHostAccessible(path))
                        {
                            NavigatePathCommand?.Execute(path);
                            IsTextEditable = false;
                        }
                        else
                        {
                            e.IsValid = false;
                            e.ErrorType = ErrorType.Critical;
                        }
                    }

                    if (path.LastIndexOf(FileSystemHelper.Separator) > 0)
                    {
                        path = path.Substring(0, path.LastIndexOf(FileSystemHelper.Separator) + 1);
                        if (FileSystemHelper.DirectoryExists(path))
                            ItemsSource = FileSystemHelper.GetFolderPaths(path, ShowHiddenItems, ShowSystemItems);
                    }
                }
            };
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            IsTextEditable = true;
        }

        protected override void OnTextChanged(string oldText, string text)
        {
            base.OnTextChanged(oldText, text);

            if (!CanShowPopup)
                ClosePopup();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
                IsTextEditable = false;
        }

        protected override void OnPopupOpened()
        {
            base.OnPopupOpened();

            IsTextEditable = true;
        }

        protected override void OnPopupClosed()
        {
            base.OnPopupClosed();

            Select(Text.Length, 0);
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);

            if (IsKeyboardFocusWithin)
                SelectAll();            
            else
                IsTextEditable = false;

            if (Text.LastIndexOf(FileSystemHelper.Separator) > 0)
            {
                string path = Text.Substring(0, Text.LastIndexOf(FileSystemHelper.Separator) + 1);
                if (FileSystemHelper.DirectoryExists(path))
                    ItemsSource = FileSystemHelper.GetFolderPaths(path, ShowHiddenItems, ShowSystemItems);

                if (IsTextEditable == true && CanShowPopup)
                    ShowPopup();
            }
        }

        protected override void OnIsTextEditableChanged()
        {
            base.OnIsTextEditableChanged();

            if (IsTextEditable == true)
            {
                FileModel fileModel = CurrentItem as FileModel;
                Text = fileModel?.FullPath;
            }
        }

        private static async void OnCurrentItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BreadCrumbControl breadCrumbControl = d as BreadCrumbControl;

            FileModel fileModel = e.NewValue as FileModel;
            breadCrumbControl.Text = fileModel?.FullPath;

            List<FileModel> breadCrumbs = new List<FileModel>();
            while (fileModel != null)
            {
                breadCrumbs.Add(fileModel);

                if (!fileModel.IsRoot && fileModel.Parent == null)
                    fileModel.Parent = FileModel.FromPath(fileModel.ParentPath);

                fileModel = fileModel.Parent;

                if (fileModel != null && fileModel.Folders == null)
                    fileModel.Folders = await FileSystemHelper.GetFolders(fileModel);
            }

            breadCrumbs.Reverse();
            breadCrumbControl.BreadCrumbs = new List<FileModel>(breadCrumbs);
        }

        private void OnButtonItemLinkLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is IBarItemLinkControl barItem)
            {
                if (barItem.Link.Item is BarSplitButtonItem)
                    barItem.AddHandler(MouseEnterEvent, new MouseEventHandler(OnSplitButtonItemLinkMouseEnter), true);
            }
        }

        private void OnButtonItemLinkMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                e.Handled = true;

                if (sender is IBarItemLinkControl barItem)
                    ItemMiddleClickCommand?.Execute(barItem.Link?.Item?.CommandParameter);
            }
        }

        private void OnSplitButtonItemLinkMouseEnter(object sender, MouseEventArgs e)
        {
            if (OpenedPopupMenuCounter == 0)
                return;

            if (sender is IBarSplitButtonItemLinkControl splitButton)
                splitButton.ShowPopup();
        }

        private void OnPopupMenuOpening(object sender, CancelEventArgs e)
        {
            if (sender is PopupMenu popupMenu && popupMenu.Items.Count == 0)
            {
                FileModel fileModel = popupMenu.DataContext as FileModel;
                if (fileModel?.Folders?.Count > 0)
                {
                    BarLinkContainerItem linkContainer = new BarLinkContainerItem();
                    popupMenu.Items.Add(linkContainer);

                    fileModel.Folders.ToList().ForEach(x => linkContainer.Items.Add(new BarButtonItem { DataContext = x, Style = BreadCrumbButtonStyle }));
                }
            }
        }

        private void OnPopupMenuOpened(object sender, EventArgs e)
        {
            OpenedPopupMenuCounter++;
        }

        private void OnPopupMenuClosed(object sender, EventArgs e)
        {
            OpenedPopupMenuCounter--;
        }

        private int OpenedPopupMenuCounter;
    }
}
