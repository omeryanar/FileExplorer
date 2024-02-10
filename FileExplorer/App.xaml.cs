using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using System.Windows.Threading;
using CommandLine;
using DevExpress.Data.Filtering;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Core;
using FileExplorer.Core;
using FileExplorer.Helpers;
using FileExplorer.Model;
using FileExplorer.Persistence;
using FileExplorer.Properties;
using FileExplorer.View;
using FileExplorer.ViewModel;
using Microsoft.Win32;

namespace FileExplorer
{
    public partial class App : Application
    {
        public static AssemblyName AssemblyName { get; private set; }

        public static Repository Repository { get; private set; }

        public static PackageManager PackageManager { get; private set; }

        public static ExtensionManager ExtensionManager { get; private set; }        

        public static UserControl TaskbarIconContainer { get; private set; }

        public App()
        {
            Theme.RegisterPredefinedPaletteThemes();
            CompatibilitySettings.UseLightweightThemes = true;

            ApplicationThemeHelper.ApplicationThemeName = Settings.Default.ThemeName;
            LightweightThemeManager.CurrentThemeChanged += (x, y) =>
            {
                if (Settings.Default.ThemeName != ApplicationThemeHelper.ApplicationThemeName)
                {
                    Settings.Default.ThemeName = ApplicationThemeHelper.ApplicationThemeName;
                    Settings.Default.Save();
                }
            };

            Current.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        public static void UpdateAndRestart()
        {
            PackageManager.RestartRequired = true;
            SaveSessionAndShutdown();
        }

        public static bool BringToFront(Window window)
        {
            if (window.WindowState == WindowState.Minimized)
                SystemCommands.RestoreWindow(window);

            return window.Activate();
        }

        public static void ParseArgumentsAndRun(IEnumerable<string> args, bool firstRun = false)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(async (options) =>
            {
                if (options.Shutdown)
                    SaveSessionAndShutdown();
                else if (options.Background)
                    await PreloadAsync();
                else if (firstRun && options.Folders.Count() == 0)
                    await RestoreLastSession();
                else
                    await CreateFolderTabs(options.Folders);

            }).WithNotParsed(async (errors) =>
            {
                await CreateFolderTabs();
            });
        }

        public static void CreateNewWindow(FileModel fileModel = null)
        {
            MainView mainView = new MainView();
            MainViewModel mainViewModel = ViewModelSource.Create<MainViewModel>();

            mainView.DataContext = mainViewModel;
            mainViewModel.CreateNewTab(fileModel);

            mainView.Show();
        }

        private static async Task PreloadAsync()
        {
            await ApplicationThemeHelper.PreloadAsync(PreloadCategories.Controls, PreloadCategories.Core, PreloadCategories.Docking,
                PreloadCategories.ExpressionEditor, PreloadCategories.Grid, PreloadCategories.LayoutControl, PreloadCategories.Ribbon);
        }

        private static async Task CreateFolderTabs(IEnumerable<string> folders = null, MainView mainView = null, bool bringToFront = true)
        {
            mainView = mainView ?? Current.Windows.OfType<MainView>().FirstOrDefault();
            if (mainView == null)
            {
                mainView = new MainView();
                mainView.DataContext = ViewModelSource.Create<MainViewModel>();
            }
            mainView.Show();

            MainViewModel mainViewModel = mainView.DataContext as MainViewModel;
            await mainViewModel.CreateFolderTabs(folders);

            if (bringToFront)
                App.BringToFront(mainView);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            CriteriaOperator.RegisterCustomFunction(new ToggleCaseFunction());
            CriteriaOperator.RegisterCustomFunction(new TitleCaseFunction());
            CriteriaOperator.RegisterCustomFunction(new SentenceCaseFunction());
            CriteriaOperator.RegisterCustomFunction(new RemoveInvalidFileNameCharactersFunction());
            CriteriaOperator.RegisterCustomFunction(new RegexMatchFunction());
            CriteriaOperator.RegisterCustomFunction(new RegexIsMatchFunction());
            CriteriaOperator.RegisterCustomFunction(new RegexConcatFunction());
            CriteriaOperator.RegisterCustomFunction(new RegexReplaceFunction());
            CriteriaOperator.RegisterCustomFunction(new StringFormatFunction());

            Assembly entryAssembly = Assembly.GetEntryAssembly();
            AssemblyName = entryAssembly.GetName();

            Repository = new Repository("Data.db");
            PackageManager = new PackageManager();
            ExtensionManager = new ExtensionManager("PreviewExtensions");
            TaskbarIconContainer = FindResource("TaskbarIconContainer") as UserControl;

            FileSystemWatcherHelper.Start();
            
            InitializeJumpList();
            ParseArgumentsAndRun(e.Args, true);

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (PackageManager.UpdateStatus == UpdateStatus.ReadyToInstall)
                PackageManager.LaunchUpdater();

            Journal.Shutdown();
            base.OnExit(e);
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            SaveSessionAndShutdown();
            base.OnSessionEnding(e);
        }

        #region Session

        public static void SaveSessionAndShutdown()
        {
            if (Settings.Default.SaveLastSession)
            {
                List<IDocument> documents = new List<IDocument>();
                StringBuilder openedFolderPaths = new StringBuilder();

                foreach (MainView view in Current.Windows?.OfType<MainView>())
                {
                    if (view.DataContext is MainViewModel mainViewModel && mainViewModel.DocumentManagerService is TabbedWindowDocumentUIService documentManagerService)
                    {
                        foreach (IDocumentGroup documentGroup in documentManagerService.Groups)
                        {
                            foreach (IDocument document in documentGroup.Documents)
                            {
                                if (!documents.Contains(document) && document.Content is BrowserTabViewModel tabViewModel)
                                {
                                    documents.Add(document);

                                    if (FileSystemHelper.DirectoryExists(tabViewModel.CurrentFolder.FullPath))
                                        openedFolderPaths.Append(tabViewModel.CurrentFolder.FullPath).Append(';');
                                }
                            }
                            openedFolderPaths.Append('|');
                        }
                    }
                }

                Settings.Default.LastSession = openedFolderPaths.ToString();
                Settings.Default.Save();
            }

            Current.Shutdown();
        }

        private static async Task RestoreLastSession()
        {
            if (Settings.Default.SaveLastSession && !String.IsNullOrWhiteSpace(Settings.Default.LastSession))
            {
                foreach (string groups in Settings.Default.LastSession.Split("|"))
                {
                    MainView mainView = new MainView();
                    mainView.DataContext = ViewModelSource.Create<MainViewModel>();

                    await CreateFolderTabs(groups.Split(";"), mainView, false);
                }
            }
            else
                await CreateFolderTabs();
        }

        #endregion

        #region Events

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (Settings.Default.AddToStartup)
                registryKey.SetValue(Utilities.AppName, Utilities.AppPath + " --background");
            else
                registryKey.DeleteValue(Utilities.AppName, false);

            FileSystemWatcherHelper.Stop();
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            Journal.WriteLog(e.Exception);
            Utilities.ShowMessage(e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                string message = String.Format("A fatal error has occurred in the application.\nSorry for the inconvenience.\n\n{0}:\n{1}",
                        e.ExceptionObject.GetType(), e.ExceptionObject.ToString());

                if (e.ExceptionObject is Exception)
                    Journal.WriteLog(e.ExceptionObject as Exception, true);
                else
                    Journal.WriteLog("Fatal Error: " + message, true);

                MessageBox.Show(message, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Journal.Shutdown();
                Environment.Exit(-1);
            }
        }

        #endregion

        #region JumpList

        public static void AddToJumpList(FileModel fileModel)
        {
            if (fileModel?.IsDirectory == true)
            {
                JumpTask jumpTask = CreateJumpTask(fileModel);
                ApplicationJumpList.JumpItems.Add(jumpTask);
                ApplicationJumpList.Apply();
            }
        }

        public static void RemoveFromJumpList(FileModel fileModel)
        {
            JumpTask jumpTask = ApplicationJumpList.JumpItems.OfType<JumpTask>().FirstOrDefault(x => x.Arguments == fileModel.FullPath);
            if (jumpTask != null)
            {
                ApplicationJumpList.JumpItems.Remove(jumpTask);
                ApplicationJumpList.Apply();
            }
        }

        private void InitializeJumpList()
        {
            foreach (FileModel fileModel in FileSystemHelper.QuickAccess.Folders)
            {
                JumpTask jumpTask = CreateJumpTask(fileModel);
                ApplicationJumpList.JumpItems.Add(jumpTask);
            }

            JumpList.SetJumpList(this, ApplicationJumpList);
            ApplicationJumpList.Apply();
        }

        private static JumpTask CreateJumpTask(FileModel fileModel)
        {
            JumpTask jumpTask = new JumpTask();
            jumpTask.CustomCategory = "Quick Access";
            jumpTask.ApplicationPath = Assembly.GetExecutingAssembly().Location;
            jumpTask.IconResourcePath = "%WINDIR%\\system32\\imageres.dll";
            jumpTask.Title = fileModel.Name;
            jumpTask.Arguments = fileModel.FullPath;
            jumpTask.Description = fileModel.Description;

            jumpTask.IconResourceIndex = folderIndexes.ContainsKey(fileModel.FullPath) ? folderIndexes[fileModel.FullPath] : 4;

            return jumpTask;
        }

        private static readonly JumpList ApplicationJumpList = new JumpList();

        private static readonly Dictionary<string, int> folderIndexes = new Dictionary<string, int>()
        {
            { FileSystemHelper.UserFolders[0], 105 },
            { FileSystemHelper.UserFolders[1], 107 },
            { FileSystemHelper.UserFolders[2], 175 },
            { FileSystemHelper.UserFolders[3], 103 },
            { FileSystemHelper.UserFolders[4], 108 },
            { FileSystemHelper.UserFolders[5], 18 }
        };

        #endregion
    }
}
