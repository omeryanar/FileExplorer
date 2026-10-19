using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileExplorer.Core;
using FileExplorer.Model;
using FileExplorer.Native;

namespace FileExplorer.Helpers
{
    public class SearchHelper
    {
        public static bool IsSearchEverythingAvailable(string path)
        {
            if (path.OrdinalEquals(FileSystemHelper.ComputerPath))
				return SearchEverything.IsAvailable;

			string rootPath = Path.GetPathRoot(path);
            if (!FileSystemHelper.IsDrive(rootPath))
                return false;

            DriveInfo driveInfo = new DriveInfo(rootPath);
            if (driveInfo?.DriveFormat != "NTFS")
                return false;

            return SearchEverything.IsAvailable;
        }

		public static async Task<IList<FileModel>> SearchFolder(string path, string searchPattern, CancellationToken cancellationToken = default)
        {
            FileSystemInfo[] items = null;
            await Task.Run(() =>
            {
                try
                {
                    string[] entries = Directory.GetFileSystemEntries(path, searchPattern);
                    items = new FileSystemInfo[entries.Length];

                    for (int i = 0; i < entries.Length; i++)
                    {
                        if(Directory.Exists(entries[i]))
                            items[i] = new DirectoryInfo(entries[i]);
                        else
                            items[i] = new FileInfo(entries[i]);
                    }
                }
                catch { }
            });

            List<FileModel> itemModelList = new List<FileModel>();
            if (items?.Length > 0)
            {
                foreach (FileSystemInfo item in items)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

					try
					{
						FileModel fileModel = FileModel.Create(item);
						if (fileModel != null)
							itemModelList.Add(fileModel);
					}
					catch { }
				}
            }

            return itemModelList;
        }

        public static async Task<IList<FileModel>> SearchWithEverything(string path, string searchPattern, CancellationToken cancellationToken = default)
        {
            FileSystemInfo[] items = null;
            await Task.Run(() =>
            {
                try
                {
                    using (SearchEverything searchEverything = new SearchEverything())
                    {
                        string query = path.OrdinalEquals(FileSystemHelper.ComputerPath) ? searchPattern : $"{path}\\ {searchPattern}";
						items = searchEverything.Search(query);
                    }
                }
                catch { }
            });

            List<FileModel> itemModelList = new List<FileModel>();
            if (items?.Length > 0)
            {
                foreach (FileSystemInfo item in items)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
						FileModel fileModel = FileModel.Create(item);
						if (fileModel != null)
							itemModelList.Add(fileModel);
					}
					catch { }
				}
            }

            return itemModelList;
        }

        public static async Task<string[]> GetFolderPaths(string path)
        {
            try
            {
                return await Task.Run(() => Directory.GetDirectories(path));
            }
            catch (Exception)
            {
                return [];
            }
        }
    }
}
