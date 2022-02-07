using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.XtraEditors;
using FileExplorer.Native;
using NLog;

namespace FileExplorer.Core
{
    public class Utilities
    {
        public const string PreferredDropEffect = "Preferred DropEffect";

        public const string ShellIdListArray = "Shell IDList Array";

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

        public static void CreateFolder(String path, String name)
        {
            using (FileOperation fileOperation = new FileOperation())
            {
                fileOperation.NewItem(path, name, FileAttributes.Directory);
                fileOperation.PerformOperations();
            }
        }

        public static void CreateFile(String path, String name, bool openFile = false)
        {
            using (FileOperation fileOperation = new FileOperation())
            {
                fileOperation.NewItem(path, name, FileAttributes.Normal);
                fileOperation.PerformOperations();
            }

            if (openFile)
                OpenFile(Path.Combine(path, name));
        }

        public static void RenameFile(String path, String newName)
        {
            using (FileOperation fileOperation = new FileOperation())
            {
                fileOperation.RenameItem(path, newName.Trim());
                fileOperation.PerformOperations();
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

        public static void ShowImageEditor(string path)
        {
            Image image = Image.FromFile(path);
            ImageFormat format = GetImageFormat(image);
            PictureEdit pictureEdit = new PictureEdit();

            pictureEdit.Image = image;
            if (pictureEdit.ShowImageEditorDialog() == System.Windows.Forms.DialogResult.OK)
                pictureEdit.Image.Save(path, format);
        }

        public static ImageFormat GetImageFormat(Image image)
        {
            if (image.RawFormat.Equals(ImageFormat.Jpeg))
                return ImageFormat.Jpeg;
            if (image.RawFormat.Equals(ImageFormat.Bmp))
                return ImageFormat.Bmp;
            if (image.RawFormat.Equals(ImageFormat.Png))
                return ImageFormat.Png;
            if (image.RawFormat.Equals(ImageFormat.Gif))
                return ImageFormat.Gif;
            if (image.RawFormat.Equals(ImageFormat.Icon))
                return ImageFormat.Icon;
            if (image.RawFormat.Equals(ImageFormat.MemoryBmp))
                return ImageFormat.MemoryBmp;
            if (image.RawFormat.Equals(ImageFormat.Tiff))
                return ImageFormat.Tiff;
            else
                return ImageFormat.Jpeg;
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
            List<Theme> themeList = new List<Theme>
            {
                Theme.Office2019White,
                Theme.Office2019Black,
                Theme.Office2019DarkGray,
                Theme.Office2019Colorful,
                Theme.VS2017Light,
                Theme.VS2017Dark
            };

            ICollectionView themes = CollectionViewSource.GetDefaultView(themeList.Select(x => new ThemeViewModel(x)).ToList());
            themes.GroupDescriptions.Add(new PropertyGroupDescription("Theme.Category"));

            foreach (ThemeViewModel theme in themes)
                theme.UseSvgGlyphs = true;

            return themes;
        }

        public static ICollectionView GetTouchThemes()
        {
            List<Theme> themeList = new List<Theme>
            {
                Theme.Office2019WhiteTouch,
                Theme.Office2019BlackTouch,
                Theme.Office2019DarkGrayTouch,
                Theme.Office2019ColorfulTouch
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
