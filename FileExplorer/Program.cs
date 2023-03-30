using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using CommandLine;
using FileExplorer.Properties;
using FileExplorer.Resources;
using Microsoft.VisualBasic.ApplicationServices;

namespace FileExplorer
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            SingleInstanceApp singleInstanceApp = new SingleInstanceApp();
            singleInstanceApp.Run(args);
        }
    }

    public class Options
    {
        public Options(IEnumerable<string> folders, bool background, bool shutdown)
        {
            Folders = folders;
            Shutdown = shutdown;
            Background = background;
        }

        [Value(0)]
        public IEnumerable<string> Folders { get; }

        [Option]
        public bool Background { get; }

        [Option]
        public bool Shutdown { get; }
    }

    public class SingleInstanceApp : WindowsFormsApplicationBase
    {
        public SingleInstanceApp()
        {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(StartupEventArgs e)
        {
            CultureInfo currentCulture = new CultureInfo(Settings.Default.Language);
            CultureResources.ChangeCulture(currentCulture);

            Thread.CurrentThread.CurrentCulture = currentCulture;
            Thread.CurrentThread.CurrentUICulture = currentCulture;

            App app = new App();
            app.InitializeComponent();
            app.Run();

            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs e)
        {
            base.OnStartupNextInstance(e);
            App.ParseArgumentsAndRun(e.CommandLine);
        }
    }
}
