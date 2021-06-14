using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        public static void ParseArgumentsAndRun(IEnumerable<string> args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                if (options.Shutdown)
                    Current.Shutdown();
                else if (options.Background)
                    CreateWarmupView();
                else
                    CreateMainview(options.Folders);

            }).WithNotParsed(errors =>
            {
                CreateMainview();
            });
        }

        public static void CreateNewVindow(FileModel fileModel = null)
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

        private static void CreateMainview(IEnumerable<string> folders = null)
        {
            MainView mainView = Current.Windows.OfType<MainView>().FirstOrDefault(x => x.IsActive);
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
                    FileModel fileModel = FileModel.FromPath(folder);
                    mainViewModel.CreateNewTab(fileModel);
                }
            }
            else
                mainViewModel.CreateNewTab();

            mainView.Show();
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
            TaskbarIconContainer = FindResource("TaskbarIconContainer") as UserControl;

            FileSystemWatcherHelper.Start();

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
    }
}
