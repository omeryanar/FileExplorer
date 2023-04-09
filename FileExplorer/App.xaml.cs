using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using System.Windows.Threading;
using CommandLine;
using DevExpress.Data.Filtering;
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
        public static Repository Repository { get; private set; }

        public static ExtensionManager ExtensionManager { get; private set; }

        public static UserControl TaskbarIconContainer { get; private set; }

        public App()
        {
            Theme.RegisterPredefinedPaletteThemes();

            ApplicationThemeHelper.ApplicationThemeName = Settings.Default.ThemeName;
            ThemeManager.ApplicationThemeChanged += (x, y) =>
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

        public static bool BringToFront(Window window)
        {
            if (window.WindowState == WindowState.Minimized)
                SystemCommands.RestoreWindow(window);

            return window.Activate();
        }

        public static void ParseArgumentsAndRun(IEnumerable<string> args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(async (options) =>
            {
                if (options.Shutdown)
                    Current.Shutdown();
                else if (options.Background)
                    CreateWarmupView();
                else
                    await CreateMainview(options.Folders);

            }).WithNotParsed(async (errors) =>
            {
                await CreateMainview();
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

        private static void CreateWarmupView()
        {
            Window warmupView = new WarmupView()
            {
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Normal,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                ShowActivated = false,
                Height = 0,
                Width = 0,
            };
            warmupView.Loaded += async (s, e) =>
            {
                await ThemeManager.PreloadThemeResourceAsync(Settings.Default.ThemeName);
                warmupView.Close();
            };

            BrowserTabViewModel viewModel = ViewModelSource.Create<BrowserTabViewModel>();
            viewModel.OpenItem(FileSystemHelper.QuickAccess);

            warmupView.DataContext = viewModel;
            warmupView.Show();
            warmupView.Hide();
        }

        private static async Task CreateMainview(IEnumerable<string> folders = null)
        {
            MainView mainView = Current.Windows.OfType<MainView>().FirstOrDefault();
            if (mainView == null)
            {
                mainView = new MainView();
                mainView.DataContext = ViewModelSource.Create<MainViewModel>();
            }

            MainViewModel mainViewModel = mainView.DataContext as MainViewModel;
            List<string> validFolders = folders?.Where(x => FileSystemHelper.DirectoryExists(x)).ToList();
            if (validFolders?.Count > 0)
            {
                foreach (string folder in validFolders)
                {
                    FileModel selectedFolder = FileModel.FromPath(folder);

                    FileModel fileModel = selectedFolder;
                    while (fileModel != null)
                    {
                        if (!fileModel.IsRoot && fileModel.Parent == null)
                            fileModel.Parent = FileModel.FromPath(fileModel.ParentPath);

                        fileModel = fileModel.Parent;

                        if (fileModel != null && fileModel.Folders == null)
                            fileModel.Folders = await FileSystemHelper.GetFolders(fileModel);
                    }

                    mainViewModel.CreateNewTab(selectedFolder);
                }
            }
            else
                mainViewModel.CreateNewTab();

            mainView.Show();
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

            Repository = new Repository("Data.db");
            ExtensionManager = new ExtensionManager("PreviewExtensions");
            TaskbarIconContainer = FindResource("TaskbarIconContainer") as UserControl;            

            FileSystemWatcherHelper.Start();

            InitializeJumpList();
            ParseArgumentsAndRun(e.Args);

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Journal.Shutdown();
            base.OnExit(e);
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            Shutdown();
            base.OnSessionEnding(e);
        }

        #region Events

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (Settings.Default.AddToStartup)
                registryKey.SetValue("FileExplorer", Utilities.AppPath + " --background");
            else
                registryKey.DeleteValue("FileExplorer", false);

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
