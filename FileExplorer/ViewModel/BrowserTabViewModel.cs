using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using FileExplorer.Core;
using FileExplorer.Helpers;
using FileExplorer.Messages;
using FileExplorer.Model;
using FileExplorer.Properties;

namespace FileExplorer.ViewModel
{
    public class BrowserTabViewModel : ISupportParentViewModel, IDocumentContent
    {
        #region Interfaces

        public virtual object ParentViewModel { get; set; }

        public virtual IDocumentOwner DocumentOwner { get; set; }

        public virtual object Title { get; protected set; }

        public void OnClose(CancelEventArgs e)
        {
            Messenger.Default.Unregister<NotificationMessage>(this);
        }

        public void OnDestroy()
        {
        }

        #endregion

        #region Properties

        public virtual FileModel CurrentFolder { get; set; }

        public virtual Layout Layout { get; protected set; }

        public virtual CriteriaOperator FilterCriteria { get; protected set; }

        public virtual FileModelCollection SearchResults { get; protected set; }

        public virtual FileModelReadOnlyCollection DisplayItems { get; protected set; }        

        public virtual IDialogService DialogService { get { return null; } }        

        public Settings Settings { get; } = new Settings();

        public FileModel QuickAccess { get; } = FileSystemHelper.QuickAccess;

        public FileModelCollection RecentLocations { get; } = new FileModelCollection();

        public FileModelCollection RootFolders { get; } = new FileModelCollection { FileSystemHelper.QuickAccess, FileSystemHelper.Computer, FileSystemHelper.Network };

        #endregion

        #region Themes

        public ICollectionView ThemeCollection
        {
            get
            {
                if (themeCollection == null)
                    themeCollection = ThemeHelper.GetThemes();

                return themeCollection;
            }
        }
        private ICollectionView themeCollection;

        #endregion

        public BrowserTabViewModel()
        {
            Layout = (Layout)Settings.Layout;

            UpdateFilterCriteria();
            Settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Settings.ShowHiddenItems) || (e.PropertyName == nameof(Settings.ShowSystemItems)))
                    UpdateFilterCriteria();

                SaveSettings();
            };

            Messenger.Default.Register(this, (NotificationMessage message) =>
            {
                if (SearchResults == null || HighlightedText == null)
                    return;

                if (message.NotificationType == NotificationType.Add && CurrentFolder.FullPath.OrdinalEquals(FileSystemHelper.GetParentFolderPath(message.Path)))
                {
                    FileModel fileModel = FileModel.FromPath(message.Path);
                    if (fileModel.Name.OrdinalContains(HighlightedText) && !SearchResults.Contains(fileModel))
                        SearchResults.Add(fileModel);
                }
                else if (message.NotificationType == NotificationType.Remove && message.Path.OrdinalStartsWith(CurrentFolder.FullPath))
                {
                    FileModel fileModel = SearchResults.FirstOrDefault(x => x.FullPath.OrdinalEquals(message.Path));
                    if (fileModel != null)
                        SearchResults.Remove(fileModel);
                }
            });
        }        

        public void ShowOptions()
        {
            MessageResult result = DialogService.ShowDialog(MessageButton.OKCancel, Properties.Resources.Options, "OptionsView", null);
            if (result == MessageResult.OK)
                Settings.Default.Save();
            else
                Settings.Default.Reload();
        }

        public void ShowExtensions()
        {
            ExtensionViewModel viewModel = ViewModelSource.Create<ExtensionViewModel>();
            viewModel.Extensions = App.Repository.Extensions;

            DialogService.ShowDialog(MessageButton.OK, Properties.Resources.Extensions, "ExtensionsView", viewModel);
        }

        public bool CanOpenItem(FileModel fileModel)
        {
            return fileModel != null;
        }

        public void OpenItem(FileModel fileModel)
        {
            if (CanOpenItem(fileModel))
            {
                if (fileModel.IsDirectory)
                    CurrentFolder = fileModel;
                else if (!String.IsNullOrEmpty(fileModel.LinkPath))
                    Utilities.OpenFile(Utilities.AppPath, $"\"{fileModel.LinkPath}\"");
                else
                    Utilities.OpenFile(fileModel.FullPath);
            }
        }

        public bool CanOpenInNewTab(FileModel fileModel)
        {
            return fileModel?.IsDirectory == true;
        }

        public void OpenInNewTab(FileModel fileModel)
        {
            if (CanOpenInNewTab(fileModel))
            {
                MainViewModel viewModel = ParentViewModel as MainViewModel;
                viewModel?.CreateNewTab(fileModel);
            }
        }

        public bool CanOpenInNewWindow(FileModel fileModel)
        {
            return fileModel?.IsDirectory == true;
        }

        public void OpenInNewWindow(FileModel fileModel)
        {
            if (CanOpenInNewWindow(fileModel))
                App.CreateNewWindow(fileModel);
        }

        public void SaveSettings()
        {
            Settings.Default.ShowNavigationPane = Settings.ShowNavigationPane;
            Settings.Default.ExpandFocusedNode = Settings.ExpandFocusedNode;
            Settings.Default.ShowDetailsPane = Settings.ShowDetailsPane;
            Settings.Default.ShowPreviewPane = Settings.ShowPreviewPane;

            Settings.Default.ShowFileExtensions = Settings.ShowFileExtensions;
            Settings.Default.ShowHiddenItems = Settings.ShowHiddenItems;
            Settings.Default.ShowSystemItems = Settings.ShowSystemItems;

            Settings.Default.ShowCheckBoxes = Settings.ShowCheckBoxes;
            Settings.Default.ShowRowNumbers = Settings.ShowRowNumbers;
            Settings.Default.SimplifiedRibbon = Settings.SimplifiedRibbon;

            Settings.Default.LeftPaneWidth = Settings.LeftPaneWidth;
            Settings.Default.RightPaneWidth = Settings.RightPaneWidth;

            Settings.Default.Save();
        }

        public async Task NavigatePath(string path)
        {
            string parsingName = FileSystemHelper.GetFileParsingName(path);
            if (String.IsNullOrEmpty(parsingName))
                return;

            FileModel navigationModel = FileModel.FromPath(parsingName);

            FileModel fileModel = navigationModel;
            while (fileModel != null)
            {
                if (!fileModel.IsRoot && fileModel.Parent == null)
                    fileModel.Parent = FileModel.FromPath(fileModel.ParentPath);

                fileModel = fileModel.Parent;

                if (fileModel != null && fileModel.Folders == null)
                    fileModel.Folders = await FileSystemHelper.GetFolders(fileModel);
            }

            if (navigationModel != null)
                OpenItem(navigationModel);
        }

        protected void OnCurrentFolderChanging()
        {
            if (CurrentFolder != null)
            {
                RecentLocations.Remove(CurrentFolder);
                RecentLocations.Insert(0, CurrentFolder);

                if (!BackwardItemPushLock)
                {
                    BackwardItem = CurrentFolder;
                    BackwardItems.Push(CurrentFolder);
                }
            }
        }

        protected void OnCurrentFolderChanged()
        {
            if (CurrentFolder != null)
            {
                Title = CurrentFolder.Name;

                if (String.IsNullOrEmpty(SearchText))
                    Show();
                else
                    SearchText = null;
            }
        }

        private bool BackwardItemPushLock = false;

        #region List&Search

        public virtual bool IsLoading { get; set; }

        public virtual bool IsRecursive { get; set; }

        public virtual string SearchText { get; set; }

        public virtual string HighlightedText { get; set; }

        public async void Show()
        {
            if (CurrentFolder?.Content != null)
            {
                DisplayItems = CurrentFolder.Content;
                return;
            }

            try
            {
                IsLoading = true;

                if (CurrentFolder.Files == null)
                    CurrentFolder.Files = await FileSystemHelper.GetFiles(CurrentFolder);
                if (CurrentFolder.Folders == null)
                    CurrentFolder.Folders = await FileSystemHelper.GetFolders(CurrentFolder);

                CurrentFolder.Content = new FileModelReadOnlyCollection(CurrentFolder.Folders, CurrentFolder.Files);
                DisplayItems = CurrentFolder.Content;

            }
            finally { IsLoading = false; }
        }

        public async Task Refresh()
        {
            try
            {
                IsLoading = true;

                CurrentFolder.Files = await FileSystemHelper.GetFiles(CurrentFolder);
                CurrentFolder.Folders = await FileSystemHelper.GetFolders(CurrentFolder);

                CurrentFolder.Content = new FileModelReadOnlyCollection(CurrentFolder.Folders, CurrentFolder.Files);
                DisplayItems = CurrentFolder.Content;

            }
            finally { IsLoading = false; }
        }

        public async Task Search()
        {
            if (!String.IsNullOrWhiteSpace(SearchText))
            {
                HighlightedText = SearchText.RemoveSearchWildcards();
                Show();

                if (IsRecursive)
                {
                    try
                    {
                        IsLoading = true;

                        SearchResults = new FileModelCollection();
                        DisplayItems = new FileModelReadOnlyCollection(SearchResults);

                        if (SearchText.OrdinalEquals(HighlightedText))
                            SearchText = String.Format("*{0}*", SearchText);

                        await Search(CurrentFolder.FullPath, this.GetAsyncCommand(x => x.Search()).CancellationTokenSource);
                    }
                    finally { IsLoading = false; }
                }
            }
        }

        protected async Task Search(string path, CancellationTokenSource cancellationToken)
        {
            FileModelCollection results = await FileSystemHelper.SearchFolder(path, SearchText);
            SearchResults.AddRange(results);

            string[] childFolders = await FileSystemHelper.GetFolderPaths(path);
            foreach (string childFolder in childFolders)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await Search(childFolder, cancellationToken);
            }
        }

        protected void OnSearchTextChanged()
        {
            if (String.IsNullOrWhiteSpace(SearchText))
            {
                SearchResults = null;
                HighlightedText = null;                

                Show();
            }
        }

        protected void OnIsRecursiveChanged()
        {
            this.GetAsyncCommand(x => x.Search()).Execute(null);
        }

        #endregion

        #region Navigation

        public virtual FileModel ForwardItem { get; protected set; }

        public virtual FileModel BackwardItem { get; protected set; }

        protected Stack<FileModel> ForwardItems = new Stack<FileModel>();

        protected Stack<FileModel> BackwardItems = new Stack<FileModel>();

        public bool CanMoveUp()
        {
            return CurrentFolder?.IsRoot == false;
        }

        public void MoveUp()
        {
            if (CanMoveUp())
                CurrentFolder = CurrentFolder.Parent;
        }

        public bool CanMoveNext()
        {
            return ForwardItem != null;
        }

        public void MoveNext()
        {
            if (CanMoveNext())
            {
                CurrentFolder = ForwardItem;

                if (ForwardItems.Count > 0)
                    ForwardItems.Pop();

                ForwardItem = ForwardItems.Count == 0 ? null : ForwardItems.Peek();
            }
        }

        public bool CanMoveBack()
        {
            return BackwardItem != null;
        }

        public void MoveBack()
        {
            if (CanMoveBack())
            {
                ForwardItem = CurrentFolder;
                ForwardItems.Push(CurrentFolder);

                BackwardItemPushLock = true;
                CurrentFolder = BackwardItem;
                BackwardItemPushLock = false;

                if (BackwardItems.Count > 0)
                    BackwardItems.Pop();

                BackwardItem = BackwardItems.Count == 0 ? null : BackwardItems.Peek();
            }
        }

        #endregion

        #region Methods

        protected void UpdateFilterCriteria()
        {
            string filterString = "1 = 1";

            if (!Settings.ShowHiddenItems)
                filterString += " AND [IsHidden] = False";

            if (!Settings.ShowSystemItems)
                filterString += " AND [IsSystem] = False";

            FilterCriteria = CriteriaOperator.Parse(filterString);
        }

        protected void SaveQuickAccessFolders()
        {
            string quickAccessFolders = $";{String.Join(";", QuickAccess.Folders.Select(x => x.FullPath))}";

            Settings.Default.QuickAccessFolders = quickAccessFolders;
            Settings.Default.Save();
        }

        #endregion

        #region New

        public bool CanCreateNewFolder(FileModel fileModel)
        {
            if (fileModel == null)
                fileModel = CurrentFolder;

            return fileModel?.IsRoot == false && !FileSystemHelper.IsNetworkHost(fileModel.FullPath);
        }

        public void CreateNewFolder(FileModel fileModel)
        {
            string targetPath = fileModel?.IsDirectory == true ? fileModel.FullPath : CurrentFolder.FullPath;
            string targetName = fileModel?.IsDirectory == true ? fileModel.FullName : CurrentFolder.FullName;

            RenameViewModel viewModel = ViewModelSource.Create<RenameViewModel>();
            viewModel.Name = Properties.Resources.NewFolder;

            viewModel.Parent = targetName;
            if (String.IsNullOrEmpty(viewModel.Parent))
                viewModel.Parent = targetPath.Replace(":", "");

            MessageResult result = DialogService.ShowDialog(MessageButton.OKCancel, Properties.Resources.NewFolder, "RenameView", viewModel);
            if (result == MessageResult.OK)
                Utilities.CreateFolder(targetPath, viewModel.Name);
        }

        public bool CanCreateNewFile(FileModel fileModel)
        {
            if (fileModel == null)
                fileModel = CurrentFolder;

            return fileModel?.IsRoot == false && !FileSystemHelper.IsNetworkHost(fileModel.FullPath);
        }

        public void CreateNewFile(FileModel fileModel)
        {
            string targetPath = fileModel?.IsDirectory == true ? fileModel.FullPath : CurrentFolder.FullPath;
            string targetName = fileModel?.IsDirectory == true ? fileModel.FullName : CurrentFolder.FullName;

            RenameViewModel viewModel = ViewModelSource.Create<RenameViewModel>();
            viewModel.Name = Properties.Resources.NewFile;
            viewModel.Extension = ".txt";

            viewModel.Parent = targetName;
            if (String.IsNullOrEmpty(viewModel.Parent))
                viewModel.Parent = targetPath.Replace(":", "");

            MessageResult result = DialogService.ShowDialog(MessageButton.OKCancel, Properties.Resources.NewFile, "RenameView", viewModel);
            if (result == MessageResult.OK)
                Utilities.CreateFile(targetPath, viewModel.Name + viewModel.Extension, true);
        }

        public bool CanSendAsEmail(IList<object> items)
        {
            return items != null && items.Count > 0 && items.OfType<FileModel>().All(x => !x.IsDirectory);
        }

        public void SendAsEmail(IList<object> items)
        {
            string[] filePaths = items.OfType<FileModel>().Select(x => x.FullPath).ToArray();
            string emlFilePath = Utilities.SaveFilesAsEmail(String.Empty, filePaths);

            Utilities.OpenFile(emlFilePath);
        }

        public bool CanZipItems(IList<object> items)
        {
            return items != null && items.Count > 0 && items.OfType<FileModel>().Any(x => !x.IsRoot && !x.Extension.OrdinalEquals(".zip"));
        }

        public void ZipItems(IList<object> items)
        {
            ZipArchiveViewModel viewModel = ViewModelSource.Create<ZipArchiveViewModel>();
            viewModel.FileModelList = items.OfType<FileModel>().ToList();

            if (viewModel.FileModelList.Count == 1)
                viewModel.FilePath = System.IO.Path.Combine(viewModel.FileModelList[0].ParentPath, viewModel.FileModelList[0].Name) + ".zip";
            else
                viewModel.FilePath = System.IO.Path.Combine(CurrentFolder.FullPath, CurrentFolder.Name) + ".zip";

            DialogService.ShowDialog(viewModel.UICommandList, Properties.Resources.Zip, "ZipArchiveView", viewModel);
        }

        public bool CanUnzip(FileModel fileModel)
        {
            return fileModel != null && fileModel.Extension.OrdinalEquals(".zip");
        }

        public void Unzip(FileModel fileModel)
        {
            ZipExtractViewModel viewModel = ViewModelSource.Create<ZipExtractViewModel>();
            viewModel.FileModel = fileModel;
            viewModel.FilePath = fileModel.ParentPath;

            DialogService.ShowDialog(viewModel.UICommandList, Properties.Resources.Unzip, "ZipExtractView", viewModel);
        }

        #endregion

        #region Clipboard

        public bool CanPinToQuickAccess(FileModel fileModel)
        {
            return fileModel != null && !fileModel.IsRoot && fileModel.IsDirectory && !QuickAccess.Folders.Contains(fileModel);
        }

        public void PinToQuickAccess(FileModel fileModel)
        {
            QuickAccess.Folders.Add(fileModel);
            SaveQuickAccessFolders();

            App.AddToJumpList(fileModel);
        }

        public bool CanUnpinFromQuickAccess(FileModel fileModel)
        {
            return fileModel != null && !fileModel.IsRoot && QuickAccess.Folders.Contains(fileModel);
        }

        public void UnpinFromQuickAccess(FileModel fileModel)
        {
            QuickAccess.Folders.Remove(fileModel);
            SaveQuickAccessFolders();

            App.RemoveFromJumpList(fileModel);
        }

        public bool CanCopyPathToClipboard(IList<object> items)
        {
            return items != null && items.Count > 0;
        }

        public void CopyPathToClipboard(IList<object> items)
        {
            IEnumerable<FileModel> fileModels = items.OfType<FileModel>();

            string path = String.Join(Environment.NewLine, fileModels.Select(x => x.FullPath));
            Utilities.SetClipboardText(path);
        }

        public bool CanCopyToClipboard(IList<object> items)
        {
            return items != null && items.Count > 0 && items.OfType<FileModel>().All(x => !x.IsRoot);
        }

        public void CopyToClipboard(IList<object> items)
        {
            IEnumerable<FileModel> fileModels = items.OfType<FileModel>();

            string[] files = fileModels.Select(x => x.FullPath).ToArray();
            Utilities.CopyFilesToClipboard(files, false);
        }

        public bool CanCutToClipboard(IList<object> items)
        {
            return items != null && items.Count > 0 && items.OfType<FileModel>().All(x => x.Parent?.IsRoot == false);
        }

        public void CutToClipboard(IList<object> items)
        {
            IEnumerable<FileModel> fileModels = items.OfType<FileModel>();

            string[] files = fileModels.Select(x => x.FullPath).ToArray();
            Utilities.CopyFilesToClipboard(files, true);
        }

        public bool CanPasteFromClipboard(FileModel fileModel)
        {
            if (fileModel == null)
                fileModel = CurrentFolder;

            return fileModel?.IsRoot == false && Utilities.FileExistsInClipboard();
        }

        public void PasteFromClipboard(FileModel fileModel)
        {
            string targetPath = fileModel?.IsDirectory == true ? fileModel.FullPath : CurrentFolder.FullPath;

            if (Settings.Default.ConfirmCopy && Utilities.GetCopyOrMoveFlagInClipboard() == 5)
            {
                MessageViewModel viewModel = ViewModelSource.Create<MessageViewModel>();
                viewModel.Icon = IconType.Question;
                viewModel.Title = Properties.Resources.ConfirmCopy;
                viewModel.Content = String.Format(Properties.Resources.ConfirmMultipleCopy, targetPath);

                MessageResult result = DialogService.ShowDialog(MessageButton.YesNo, Properties.Resources.Copy, "MessageView", viewModel);
                if (result == MessageResult.No)
                    return;
            }

            if (Settings.Default.ConfirmMove && Utilities.GetCopyOrMoveFlagInClipboard() == 2)
            {
                MessageViewModel viewModel = ViewModelSource.Create<MessageViewModel>();
                viewModel.Icon = IconType.Question;
                viewModel.Title = Properties.Resources.ConfirmMove;
                viewModel.Content = String.Format(Properties.Resources.ConfirmMultipleMove, targetPath);

                MessageResult result = DialogService.ShowDialog(MessageButton.YesNo, Properties.Resources.Move, "MessageView", viewModel);
                if (result == MessageResult.No)
                    return;
            }

            Utilities.PasteFromClipboard(targetPath);
        }

        #endregion

        #region Organize

        public bool CanCopyToItems(IList<object> items)
        {
            return items != null && items.Count > 0 && items.OfType<FileModel>().All(x => !x.IsRoot);
        }

        public void CopyToItems(IList<object> items, string path)
        {
            if (Settings.Default.ConfirmCopy)
            {
                MessageViewModel viewModel = ViewModelSource.Create<MessageViewModel>();
                viewModel.Icon = IconType.Question;

                if (items.Count == 1)
                {
                    viewModel.Title = Properties.Resources.ConfirmCopy;
                    viewModel.Content = String.Format(Properties.Resources.ConfirmCopyMessage, items[0], path);
                }
                else
                {
                    viewModel.Title = Properties.Resources.ConfirmCopy;
                    viewModel.Content = String.Format(Properties.Resources.ConfirmMultipleCopy, path);
                    viewModel.Details = String.Join(Environment.NewLine, items);
                }

                MessageResult result = DialogService.ShowDialog(MessageButton.YesNo, Properties.Resources.CopyTo, "MessageView", viewModel);
                if (result == MessageResult.No)
                    return;
            }

            IEnumerable<FileModel> fileModels = items.OfType<FileModel>();
            Utilities.CopyFiles(fileModels.Select(x => x.FullPath), path);
        }

        public bool CanMoveToItems(IList<object> items)
        {
            return items != null && items.Count > 0 && items.OfType<FileModel>().All(x => x.Parent?.IsRoot == false);
        }

        public void MoveToItems(IList<object> items, string path)
        {
            if (Settings.Default.ConfirmMove)
            {
                MessageViewModel viewModel = ViewModelSource.Create<MessageViewModel>();
                viewModel.Icon = IconType.Question;

                if (items.Count == 1)
                {
                    viewModel.Title = Properties.Resources.ConfirmMove;
                    viewModel.Content = String.Format(Properties.Resources.ConfirmMoveMessage, items[0], path);
                }
                else
                {
                    viewModel.Title = Properties.Resources.ConfirmMove;
                    viewModel.Content = String.Format(Properties.Resources.ConfirmMultipleMove, path);
                    viewModel.Details = String.Join(Environment.NewLine, items);
                }

                MessageResult result = DialogService.ShowDialog(MessageButton.YesNo, Properties.Resources.MoveTo, "MessageView", viewModel);
                if (result == MessageResult.No)
                    return;
            }

            IEnumerable<FileModel> fileModels = items.OfType<FileModel>();
            Utilities.MoveFiles(fileModels.Select(x => x.FullPath), path);
        }

        public bool CanShowProperties(IList<object> items)
        {
            return items != null && items.Count > 0 && items.OfType<FileModel>().All(x => !x.IsRoot);
        }

        public void ShowProperties(IList<object> items)
        {
            if (items.Count == 1)
            {
                if (items[0] is FileModel fileModel)
                    Utilities.ShowProperties(fileModel.FullPath);
            }
            else
            {
                IEnumerable<FileModel> fileModels = items.OfType<FileModel>();
                Utilities.ShowMultipleProperties(fileModels.Select(x => x.FullPath));
            }

        }

        public bool CanRecycleItems(IList<object> items)
        {
            return items != null && items.Count > 0 && items.OfType<FileModel>().All(x => x.Parent?.IsRoot == false);
        }

        public void RecycleItems(IList<object> items)
        {
            Utilities.RecycleFiles(items.OfType<FileModel>().Select(x => x.FullPath));
        }

        public bool CanDeleteItems(IList<object> items)
        {
            return items != null && items.Count > 0 && items.OfType<FileModel>().All(x => x.Parent?.IsRoot == false);
        }

        public void DeleteItems(IList<object> items)
        {
            Utilities.DeleteFiles(items.OfType<FileModel>().Select(x => x.FullPath));
        }

        public bool CanRename(FileModel fileModel)
        {
            return fileModel?.Parent?.IsRoot == false;
        }

        public void Rename(FileModel fileModel)
        {
            RenameViewModel viewModel = ViewModelSource.Create<RenameViewModel>();
            viewModel.Name = fileModel.Name;
            viewModel.Extension = fileModel.Extension;
            viewModel.DateCreated = fileModel.DateCreated;
            viewModel.DateModified = fileModel.DateModified;
            viewModel.DateAccessed = fileModel.DateAccessed;
            viewModel.ShowPatternButtons = true;
            viewModel.ShowAdvancedOptions = true;

            viewModel.Parent = fileModel.ParentName.RemoveInvalidFileNameCharacters();
            if (String.IsNullOrEmpty(viewModel.Parent))
                viewModel.Parent = fileModel.ParentPath.RemoveInvalidFileNameCharacters();

            UICommand command = DialogService.ShowDialog(viewModel.UICommandList, Properties.Resources.Rename, "RenameView", viewModel);
            if (command?.IsDefault == true)
            {
                string newName = viewModel.Name.Trim() + viewModel.Extension;
                if (newName != fileModel.FullName)
                    Utilities.RenameFile(fileModel.FullPath, newName);

                if (fileModel.DateCreated != viewModel.DateCreated)
                    Utilities.SetCreationTime(fileModel.FullPath, viewModel.DateCreated);
                if (fileModel.DateModified != viewModel.DateModified)
                    Utilities.SetLastWriteTime(fileModel.FullPath, viewModel.DateModified);
                if (fileModel.DateAccessed != viewModel.DateAccessed)
                    Utilities.SetLastAccessTime(fileModel.FullPath, viewModel.DateAccessed);

                FileModel.FromPath(fileModel.FullPath);
            }
        }

        public bool CanRenameItems(IList<object> items)
        {
            return items != null && items.Count > 0 && items.OfType<FileModel>().All(x => x.Parent?.IsRoot == false);
        }

        public void RenameItems(IList<object> items)
        {
            if (items.Count == 1)
            {
                Rename(items[0] as FileModel);
                return;
            }

            RenameBatchViewModel viewModel = ViewModelSource.Create<RenameBatchViewModel>();
            viewModel.FileModelList = new List<FileModel>(items.OfType<FileModel>());

            UICommand command = DialogService.ShowDialog(viewModel.UICommandList, Properties.Resources.Rename, "RenameBatchView", viewModel);
            if (command?.IsDefault == true)
            {
                List<string> files = new List<string>(), newNames = new List<string>();

                foreach (FileModel fileModel in viewModel.FileModelList)
                {
                    string newName = fileModel.Tag.ToString().Trim() + fileModel.Extension;
                    if (newName != fileModel.FullName)
                    {
                        files.Add(fileModel.FullPath);
                        newNames.Add(newName);
                    }
                }

                Utilities.RenameFiles(files, newNames);
            }
        }

        #endregion

        #region Update

        public virtual UpdateStatus UpdateStatus { get; set; }

        public virtual double DownloadPercentage { get; set; }

        public bool CanCheckForUpdates(bool forceCheck)
        {
            return forceCheck || Settings.Default.CheckForUpdates;
        }

        public async Task CheckForUpdates(bool forceCheck)
        {
            bool updateAvailable = await UpdateHelper.CheckForUpdates(forceCheck);
            if (updateAvailable)
            {
                UpdateStatus = UpdateStatus.ReadyToDownload;

                if (Settings.Default.DownloadUpdatesAutomatically)
                    await PerformUpdate();
                else
                    await DownloadUpdate();
            }
            else if (forceCheck)
            {
                MessageViewModel viewModel = ViewModelSource.Create<MessageViewModel>();
                viewModel.Icon = IconType.Information;
                viewModel.Title = Properties.Resources.Update;
                viewModel.Content = Properties.Resources.NoUpdateAvailable;

                DialogService.ShowDialog(MessageButton.OK, Properties.Resources.Update, "MessageView", viewModel);
            }
        }

        public bool CanDownloadUpdate()
        {
            return UpdateStatus == UpdateStatus.ReadyToDownload;
        }

        public async Task DownloadUpdate()
        {
            MessageViewModel viewModel = ViewModelSource.Create<MessageViewModel>();
            viewModel.Icon = IconType.Question;
            viewModel.Title = Properties.Resources.Update;
            viewModel.Content = Properties.Resources.UpdateConfirmation;

            MessageResult result = DialogService.ShowDialog(MessageButton.YesNo, Properties.Resources.Update, "MessageView", viewModel);
            if (result == MessageResult.Yes)
                await PerformUpdate();
        }

        public bool CanInstallUpdate()
        {
            return UpdateStatus == UpdateStatus.ReadyToInstall && availableVersion != null;
        }

        public void InstallUpdate()
        {
            MessageViewModel viewModel = ViewModelSource.Create<MessageViewModel>();
            viewModel.Icon = IconType.Question;
            viewModel.Title = Properties.Resources.Restart;
            viewModel.Content = Properties.Resources.RestartConfirmation;

            MessageResult result = DialogService.ShowDialog(MessageButton.YesNo, Properties.Resources.Restart, "MessageView", viewModel);
            if (result == MessageResult.Yes)
                UpdateHelper.Restart(availableVersion);
        }

        private async Task PerformUpdate()
        {
            if (UpdateStatus != UpdateStatus.ReadyToDownload)
                return;

            try
            {
                UpdateStatus = UpdateStatus.DownloadInProgress;

                Progress<double> downloadProgress = new Progress<double>();
                downloadProgress.ProgressChanged += (s, e) => { DownloadPercentage = Math.Round(e, 2); };

                availableVersion = await UpdateHelper.PerformUpdate(downloadProgress, false);
                if (availableVersion != null)
                    UpdateHelper.LaunchUpdater(availableVersion);
            }
            finally
            {
                UpdateStatus = UpdateStatus.ReadyToInstall;
            }
        }

        private Version availableVersion;

        #endregion
    }
}
