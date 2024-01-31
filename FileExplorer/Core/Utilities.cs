using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using FileExplorer.Native;
using FileExplorer.Properties;
using NLog;
using Onova;
using Onova.Models;
using Onova.Services;

namespace FileExplorer.Core
{
    public class Utilities
    {
        public const string PreferredDropEffect = "Preferred DropEffect";

        public const string ShellIdListArray = "Shell IDList Array";

        public const string AppName = "FileExplorer";

        public static readonly string AppPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileExplorer.exe");

        public static void OpenFile(String file)
        {
            try
            {
                Process.Start(file);
            }
            catch (Exception ex)
            {
                ShowMessage(ex);
            }
        }

        public static void OpenFile(String file, String arguments, bool useShellExecute = false)
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo(file);
                processStartInfo.Arguments = arguments;
                processStartInfo.UseShellExecute = useShellExecute;

                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                ShowMessage(ex);
            }
        }

        public static void ShowMessage(Exception ex)
        {
            ThemedMessageBox.Show(Properties.Resources.ApplicationError, String.Format("{0}: {1}", ex.GetType(), ex.Message), 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowMessage(string title, string text, MessageBoxImage image)
        {
            ThemedMessageBox.Show(title, text, MessageBoxButton.OK, image);
        }

        public static void SetCreationTime(String path, DateTime dateTime)
        {
            File.SetCreationTime(path, dateTime);
        }

        public static void SetLastWriteTime(String path, DateTime dateTime)
        {
            File.SetLastWriteTime(path, dateTime);
        }

        public static void SetLastAccessTime(String path, DateTime dateTime)
        {
            File.SetLastAccessTime(path, dateTime);
        }

        public static bool CreateFolder(String path, String name)
        {
            using (FileOperation fileOperation = new FileOperation())
            {
                fileOperation.NewItem(path, name, FileAttributes.Directory);
                return fileOperation.PerformOperations();
            }
        }

        public static bool CreateFile(String path, String name, bool openFile = false)
        {
            bool successful = false;
            using (FileOperation fileOperation = new FileOperation())
            {
                fileOperation.NewItem(path, name, FileAttributes.Normal);
                successful = fileOperation.PerformOperations();
            }

            if (successful && openFile)
                OpenFile(Path.Combine(path, name));

            return successful;
        }

        public static bool RenameFile(String path, String newName)
        {
            using (FileOperation fileOperation = new FileOperation())
            {
                fileOperation.RenameItem(path, newName.Trim());
                return fileOperation.PerformOperations();
            }
        }

        public static void RenameFiles(IList<String> files, IList<String> newNames)
        {
            Task.Run(() =>
            {
                using (FileOperation fileOperation = new FileOperation())
                {
                    for (int i = 0; i < files.Count; i++)
                        fileOperation.RenameItem(files[i], newNames[i].Trim());

                    fileOperation.PerformOperations();
                }
            });
        }        

        public static void RecycleFiles(IEnumerable<String> files)
        {
            Task.Run(() =>
            {
                using (FileOperation fileOperation = new FileOperation())
                {
                    foreach (string file in files)
                        fileOperation.DeleteItem(file);

                    fileOperation.AllowUndo();
                    fileOperation.PerformOperations();
                }
            });
        }

        public static void DeleteFiles(IEnumerable<String> files)
        {
            Task.Run(() =>
            {
                using (FileOperation fileOperation = new FileOperation())
                {
                    foreach (string file in files)
                        fileOperation.DeleteItem(file);

                    fileOperation.PerformOperations();
                }
            });
        }

        public static void MoveFiles(IEnumerable<String> files, string destination)
        {
            Task.Run(() =>
            {
                using (FileOperation fileOperation = new FileOperation())
                {
                    foreach (string file in files)
                        fileOperation.MoveItem(file, destination, Path.GetFileName(file));

                    fileOperation.PerformOperations();
                }
            });
        }

        public static void CopyFiles(IEnumerable<String> files, string destination)
        {
            Task.Run(() =>
            {
                using (FileOperation fileOperation = new FileOperation())
                {
                    foreach (string file in files)
                        fileOperation.CopyItem(file, destination, Path.GetFileName(file));

                    fileOperation.PerformOperations();
                }
            });
        }

        public static void CopyFilesToClipboard(IEnumerable<String> files, bool cut)
        {
            if (files != null)
            {
                IDataObject dataObject = CreateDataObject(files.ToArray(), cut);
                Clipboard.SetDataObject(dataObject);
            }
        }

        public static void PasteFromClipboard(string path)
        {
            IDataObject data = Clipboard.GetDataObject();
            if (!data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] files = data.GetData(DataFormats.FileDrop) as string[];
            MemoryStream memoryStream = data.GetData(PreferredDropEffect, true) as MemoryStream;
            
            int flag = memoryStream.ReadByte();
            if (flag != 2 && flag != 5)
                return;

            if (flag == 2)
                MoveFiles(files, path);
            else
                CopyFiles(files, path);
        }

        public static void SetClipboardText(string text)
        {
            Clipboard.SetText(text);
        }

        public static void ShowProperties(string path)
        {
            ShowMultipleProperties(new string[] { path });
        }

        public static void ShowMultipleProperties(IEnumerable<String> files)
        {
            DataObject data = CreateDataObject(files.ToArray());
            data.SetData(ShellIdListArray, SafeNativeMethods.CreateShellIdList(files.ToArray()), true);

            SafeNativeMethods.MultiFileProperties(data);
        }

        public static bool FileExistsInClipboard()
        {
            IDataObject data = Clipboard.GetDataObject();
            return data.GetDataPresent(DataFormats.FileDrop);
        }

        public static int GetCopyOrMoveFlagInClipboard()
        {
            IDataObject data = Clipboard.GetDataObject();
            MemoryStream memoryStream = data.GetData(PreferredDropEffect, true) as MemoryStream;

            return memoryStream.ReadByte();
        }

        public static DataObject CreateDataObject(string[] files, bool cut = false)
        {
            byte[] bytes = new byte[] { (byte)(cut ? 2 : 5), 0, 0, 0 };
            MemoryStream memoryStream = new MemoryStream(4);
            memoryStream.Write(bytes, 0, bytes.Length);

            DataObject dataObject = new DataObject(DataFormats.FileDrop, files);
            dataObject.SetData(PreferredDropEffect, memoryStream);

            return dataObject;
        }

        public static string SaveFilesAsEmail(string body, string[] files = null)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string emlFilePath = Path.Combine(tempDirectory, "email.eml");
            Directory.CreateDirectory(tempDirectory);

            using (SmtpClient client = new SmtpClient())
            {
                MailMessage mailMessage = new MailMessage();
                mailMessage.Headers.Add("X-Unsent", "1");
                mailMessage.From = new MailAddress("someone@somewhere.com");
                mailMessage.To.Add("someone@somewhere.com");

                if (files != null)
                {
                    foreach (string file in files)
                        mailMessage.Attachments.Add(new Attachment(file));
                }

                mailMessage.IsBodyHtml = true;
                mailMessage.Body = body;

                client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                client.PickupDirectoryLocation = tempDirectory;
                client.Send(mailMessage);

                string filePath = Directory.GetFiles(tempDirectory).Single();
                using (StreamReader streamReader = new StreamReader(filePath))
                {
                    using (StreamWriter streamWriter = new StreamWriter(emlFilePath))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            if (!line.StartsWith("X-Sender:") &&
                                !line.StartsWith("From:") &&
                                !line.StartsWith("X-Receiver:") &&
                                !line.StartsWith("To:"))
                            {
                                streamWriter.WriteLine(line);
                            }
                        }
                    }
                }
            }

            return emlFilePath;
        }
    }

    public static class UIHelper
    {
        public static bool ParentExists<T>(this DependencyObject child) where T : DependencyObject
        {
            return TryFindParent<T>(child) != null;
        }

        public static T TryFindParent<T>(this DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = GetParentObject(child);
            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;
            else
                return TryFindParent<T>(parentObject);
        }

        public static DependencyObject GetParentObject(this DependencyObject child)
        {
            if (child == null)
                return null;

            if (child is ContentElement contentElement)
            {
                DependencyObject parent = ContentOperations.GetParent(contentElement);
                if (parent != null)
                    return parent;

                return contentElement is FrameworkContentElement fce ? fce.Parent : null;
            }

            if (child is FrameworkElement frameworkElement)
            {
                DependencyObject parent = frameworkElement.Parent;
                if (parent != null)
                    return parent;
            }

            return VisualTreeHelper.GetParent(child);
        }

        public static GridViewHitInfoBase CalcHitInfo(this DataViewBase source, DependencyObject target)
        {
            switch (source)
            {
                case TableView view:
                    return view.CalcHitInfo(target);
                case TreeListView view:
                    return view.CalcHitInfo(target);
                case CardView view:
                    return view.CalcHitInfo(target);
                default:
                    return null;
            }
        }
    }

    public class ThemeHelper
    {
        public static ICollectionView GetThemes()
        {
            List<LightweightTheme> themeList = new List<LightweightTheme>
            {
                LightweightTheme.Win10Dark,
                LightweightTheme.Win10Light,
                LightweightTheme.Win10System,
                LightweightTheme.Win11Dark,
                LightweightTheme.Win11Light,
                LightweightTheme.Win11System,
                LightweightTheme.Office2019Black,
                LightweightTheme.Office2019Colorful,
                LightweightTheme.Office2019System,
                LightweightTheme.VS2019Dark,
                LightweightTheme.VS2019Light,
                LightweightTheme.VS2019System
            };

            ICollectionView themes = CollectionViewSource.GetDefaultView(themeList.Select(x => new ThemeViewModel(x)).ToList());
            themes.GroupDescriptions.Add(new PropertyGroupDescription("Theme.Category"));

            foreach (ThemeViewModel theme in themes)
                theme.UseSvgGlyphs = true;

            return themes;
        }
    }

    public class UpdateHelper
    {
        public static async Task<bool> CheckForUpdates(bool forceCheck = false)
        {
            if (!forceCheck && DateTime.Now.Subtract(Settings.Default.LastUpdate).TotalHours < 12)
                return false;

            Settings.Default.LastUpdate = DateTime.Now;
            Settings.Default.Save();

            using (IUpdateManager updateManager = new UpdateManager(packageResolver, packageExtractor))
            {
                CheckForUpdatesResult result = await updateManager.CheckForUpdatesAsync();
                return result.CanUpdate;
            }
        }

        public static async Task<Version> PerformUpdate(IProgress<double> progress, bool restart = true, CancellationToken cancellationToken = default)
        {
            using (IUpdateManager updateManager = new UpdateManager(packageResolver, packageExtractor))
            {
                CheckForUpdatesResult result = await updateManager.CheckForUpdatesAsync();
                if (result.CanUpdate)
                {
                    await updateManager.PrepareUpdateAsync(result.LastVersion, progress, cancellationToken);

                    if (result.LastVersion != null && updateManager.IsUpdatePrepared(result.LastVersion))
                        return result.LastVersion;
                }

                return null;
            }
        }

        public static void LaunchUpdater(Version version, bool restart = false)
        {
            using (IUpdateManager updateManager = new UpdateManager(packageResolver, packageExtractor))
            {
                if (updateManager.IsUpdatePrepared(version))
                    updateManager.LaunchUpdater(version, restart);

                UpdaterLaunched = true;
            }
        }

        public static void Restart(Version version)
        {
            if (UpdaterLaunched == false)
                LaunchUpdater(version, true);

            App.Current.Shutdown();
        }

        private static bool UpdaterLaunched = false;

        private static readonly IPackageExtractor packageExtractor = new ZipPackageExtractor();

        private static readonly IPackageResolver packageResolver = new GithubPackageResolver("omeryanar", "FileExplorer", "*.zip");
    }

    public class Journal
    {
        public static void WriteLog(string message, bool fatal = false)
        {
            if (fatal)
                logger.Fatal(message);
            else
                logger.Error(message);

            LogManager.Flush();
        }

        public static void WriteLog(Exception ex, bool fatal = false)
        {
            if (fatal)
                logger.Fatal(ex);
            else
                logger.Error(ex);

            LogManager.Flush();
        }

        public static void Shutdown()
        {
            LogManager.Shutdown();
        }

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    }
}
