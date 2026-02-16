using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using FileExplorer.ViewModel;
using NaturalSort.Extension;
using NLog;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.Windows.Shell.ShellFileOperations;

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

        public static async void StartProcess(ProcessStartInfo startInfo, IDialogService dialogService, bool showErrors)
        {
            string errorData = String.Empty;

            await Task.Run(() =>
            {
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.EnableRaisingEvents = true;
                    process.ErrorDataReceived += (s, e) => { errorData += e.Data; };

                    process.Start();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }
            });

            if (showErrors && !String.IsNullOrEmpty(errorData))
            {
                MessageViewModel messageViewModel = ViewModelSource.Create(() => new MessageViewModel());
                messageViewModel.Title = Properties.Resources.RunCommand;
                messageViewModel.Icon = IconType.Exclamation;
                messageViewModel.Content = $"{startInfo.FileName} {startInfo.Arguments}";
                messageViewModel.Details = errorData;

                dialogService.ShowDialog(MessageButton.OK, Properties.Resources.Error, "MessageView", messageViewModel);
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
            using (ShellFolder shellFolder = new ShellFolder(path))
            {
                try
                {
                    NewItem(shellFolder, name, FileAttributes.Directory);
                    return true;
                }
                catch { return false; }
            }
        }

        public static bool CreateFile(String path, String name, bool openFile = false)
        {
            using (ShellFolder shellFolder = new ShellFolder(path))
            {
                try
                {
                    NewItem(shellFolder, name);

                    if (openFile)
                        OpenFile(Path.Combine(path, name));

                    return true;
                }
                catch { return false; }
            }  
        }

        public static bool RenameFile(String path, String newName)
        {
            using (ShellItem shellItem = new ShellItem(path))
            {
                try
                {
                    Rename(shellItem, newName.Trim());
                    return true;
                }
                catch { return false; }
            }
        }

        public static void RenameFiles(IList<String> files, IList<String> newNames)
        {
            using (ShellFileOperations fileOperations = new ShellFileOperations())
            {
                for (int i = 0; i < files.Count; i++)
                {
                    ShellItem shellItem = new ShellItem(files[i]);
                    fileOperations.QueueRenameOperation(shellItem, newNames[i].Trim());
                }

                fileOperations.PerformOperations();
            }
        }

        public static void RecycleFiles(IEnumerable<String> files)
        {
            Task.Run(() =>
            {
                Delete(files.Select(x => new ShellItem(x)), OperationFlags.NoConfirmMkDir | OperationFlags.RecycleOnDelete);
            });
        }

        public static void RestoreFiles(IEnumerable<String> files)
        {
            Task.Run(() =>
            {
                using (ShellFileOperations  fileOperations = new ShellFileOperations())
                {
                    foreach (string file in files)
                    {
                        ShellItem shellItem = new ShellItem(file);
                        fileOperations.QueueMoveOperation(shellItem, new ShellFolder(Path.GetDirectoryName(shellItem.Name)));
                    }

                    fileOperations.PerformOperations();
                }
            });
        }

        public static void RestoreAll()
        {
            Task.Run(() =>
            {
                RecycleBin.RestoreAll();
            });
        }

        public static void EmptyRecycleBin()
        {
            Task.Run(() =>
            {
                RecycleBin.Empty(false, false, false);
            });
        }

        public static void DeleteFiles(IEnumerable<String> files)
        {
            Task.Run(() =>
            {
                Delete(files.Select(x => new ShellItem(x)), OperationFlags.NoConfirmMkDir);
            });
        }

        public static void MoveFiles(IEnumerable<String> files, string destination)
        {
            Task.Run(() =>
            {
                Move(files.Select(x => new ShellItem(x)), new ShellFolder(destination));
            });
        }

        public static void CopyFiles(IEnumerable<String> files, string destination)
        {
            Task.Run(() =>
            {
                Copy(files.Select(x => new ShellItem(x)), new ShellFolder(destination));
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
            using (ShellItem shellItem = new ShellItem(path))
            {
                shellItem.InvokeVerb("properties");
            }                
        }

        public static void ShowMultipleProperties(IEnumerable<String> files)
        {
            var dataObject = NativeClipboard.CreateDataObjectFromShellItems(files.Select(x => new ShellItem(x)).ToArray());
            Shell32.SHMultiFileProperties(dataObject);
        }

        public static bool FileExistsInClipboard()
        {
            try
            {
                return Clipboard.ContainsData(DataFormats.FileDrop);
            }
            catch{ return false; }
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

        public static int NaturalCompare(string value1, string value2)
        {
            return NaturalSortComparer.Compare(value1, value2);
        }

        private static NaturalSortComparer NaturalSortComparer = new NaturalSortComparer(StringComparison.OrdinalIgnoreCase);
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
