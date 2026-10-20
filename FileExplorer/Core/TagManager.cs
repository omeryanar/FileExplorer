using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using DevExpress.Mvvm;
using FileExplorer.Helpers;
using FileExplorer.Messages;
using FileExplorer.Persistence;
using MahApps.Metro.IconPacks;
using Vanara.Windows.Shell;

namespace FileExplorer.Core
{
	public class TagManager
	{
		public static readonly string RootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Shortcuts\Tags");

		public ICollection<TagFolder> GetTagFolders(string filePath)
		{
			if (TagDictionary.ContainsKey(filePath))
				return TagDictionary[filePath].TagFolders.OrderBy(x => x.Name).ToArray();

			return null;
		}

		public List<FileSystemInfo> GetTaggedFiles(TagFolder tagFolder)
		{
			List<FileSystemInfo> files = new List<FileSystemInfo>();

			foreach (string name in tagFolder.TaggedFiles)
			{
				FileInfo file = new FileInfo(Path.Combine(RootPath, $"{name}.lnk"));
				if (file.Exists)
				{
					FileSystemInfo fileSystemInfo = file.GetTargetInfoFromLinkInfo();
					if (fileSystemInfo != null)
						files.Add(fileSystemInfo);
				}
			}

			return files;
		}

		public string GetTaggedFilePath(string name)
		{
			foreach (KeyValuePair<string, TagInfo> keyValuePair in TagDictionary)
			{
				if (keyValuePair.Value.LinkName == name)
					return keyValuePair.Key;
			}

			return null;
		}

		public bool AddFileTag(string filePath, TagFolder tagFolder)
		{
			if (TagDictionary.ContainsKey(filePath))
			{
				TagInfo tagInfo = TagDictionary[filePath];
				if (!tagInfo.TagFolders.Contains(tagFolder))
					tagInfo.TagFolders.Add(tagFolder);

				if (tagFolder.TaggedFiles.Contains(tagInfo.LinkName))
					return false;

				tagFolder.TaggedFiles.Add(tagInfo.LinkName);
				return true;
			}
			else
			{
				ShellLink link = ShellLink.Create(Path.Combine(RootPath, $"{Guid.NewGuid()}.lnk"), filePath);
				TagInfo tagInfo = new TagInfo(link.Name, tagFolder);

				TagDictionary.Add(filePath, tagInfo);
				tagFolder.TaggedFiles.Add(tagInfo.LinkName);

				return true;
			}
		}

		public bool RemoveFileTag(string filePath, TagFolder tagFolder)
		{
			if (TagDictionary.ContainsKey(filePath))
			{
				TagInfo tagInfo = TagDictionary[filePath];
				tagInfo.TagFolders.Remove(tagFolder);

				if (tagFolder.TaggedFiles.Contains(tagInfo.LinkName))
				{
					tagFolder.TaggedFiles.Remove(tagInfo.LinkName);
					return true;
				}
			}

			return false;
		}

		public void DeleteInvalidTag(string linkPath)
		{
			FileInfo file = new FileInfo(linkPath);
			if (file.Exists)
			{
				using (ShellLink shellLink = file.GetShellLinkFromFileInfo(false))
				{
					if (shellLink != null && TagDictionary.ContainsKey(shellLink.TargetPath))
					{
						TagInfo tagInfo = TagDictionary[shellLink.TargetPath];
						foreach (TagFolder tagFolder in tagInfo.TagFolders)
						{
							if (tagFolder.TaggedFiles.Remove(tagInfo.LinkName))
								App.Repository.TagFolders.Update(tagFolder);
						}
					}
				}

				file.Delete();
			}
		}

		public void UpdateInvalidTag(string linkPath, string targetPath)
		{
			FileInfo file = new FileInfo(linkPath);
			if (file.Exists)
			{
				using (ShellLink shellLink = file.GetShellLinkFromFileInfo(false))
				{
					if (shellLink != null && TagDictionary.ContainsKey(shellLink.TargetPath))
					{
						TagInfo tagInfo = TagDictionary[shellLink.TargetPath];

						TagDictionary.Remove(shellLink.TargetPath);
						shellLink.TargetPath = targetPath;
						TagDictionary.Add(targetPath, tagInfo);
					}
				}
			}
		}

		public TagManager()
		{
			if (App.Repository.TagFolders.Count == 0)
			{
				foreach (var defaultTag in DefaultTags)
				{
					TagFolder tagFolder = new TagFolder
					{
						Name = defaultTag.Key,
						IconData = FontIconHelper.GetIconData(PackIconFontAwesomeKind.TagSolid),
						IconColor = defaultTag.Value
					};

					App.Repository.TagFolders.Add(tagFolder);
				}
			}

			DirectoryInfo rootDirectory = new DirectoryInfo(RootPath);
			if (!rootDirectory.Exists)
			{
				rootDirectory.Create();
				rootDirectory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
				rootDirectory.Parent.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
			}
			else
			{
				foreach (FileInfo file in rootDirectory.GetFiles("*.lnk"))
				{
					using (ShellLink shellLink = file.GetShellLinkFromFileInfo())
					{
						if (shellLink == null)
							continue;

						foreach (TagFolder tagFolder in App.Repository.TagFolders)
						{
							if (tagFolder.TaggedFiles.Any(x => file.Name.OrdinalEquals($"{x}.lnk")))
								AddToDictionary(shellLink, tagFolder);
						}
					}
				}
			}

			Messenger.Default.Register(App.Current, (NotificationMessage message) =>
			{
				if (TagDictionary.ContainsKey(message.Path))
				{
					TagInfo tagInfo = TagDictionary[message.Path];

					switch (message.NotificationType)
					{
						case NotificationType.Remove:							
							FileInfo linkInfo = new FileInfo(Path.Combine(RootPath, $"{tagInfo.LinkName}.lnk"));
							if (linkInfo != null)
							{
								FileSystemInfo targetInfo = linkInfo.GetTargetInfoFromLinkInfo();
								if (targetInfo != null && !message.Path.OrdinalEquals(targetInfo.FullName))
								{
									foreach (TagFolder tagFolder in tagInfo.TagFolders)
										tagFolder.TaggedFiles.Remove(tagInfo.LinkName);

									TagDictionary.Remove(message.Path);
									TagDictionary.Add(targetInfo.FullName, tagInfo);

									foreach (TagFolder tagFolder in tagInfo.TagFolders)
										tagFolder.TaggedFiles.Add(tagInfo.LinkName);

									break;
								}
							}

							TagDictionary.Remove(message.Path);
							break;

						case NotificationType.Rename:
							TagDictionary.Remove(message.Path);
							TagDictionary.Add(message.NewPath, tagInfo);
							break;
					}
				}
			});
		}

		private static void AddToDictionary(ShellLink link, TagFolder tagFolder)
		{
			if (TagDictionary.ContainsKey(link.TargetPath))
				TagDictionary[link.TargetPath].TagFolders.Add(tagFolder);
			else
				TagDictionary[link.TargetPath] = new TagInfo(link.Name, tagFolder);
		}

		private static Dictionary<string, TagInfo> TagDictionary = new Dictionary<string, TagInfo>(StringComparer.OrdinalIgnoreCase);

		private class TagInfo
		{
			public string LinkName { get; set; }

			public ObservableCollection<TagFolder> TagFolders { get; private set; }

			public TagInfo(string linkName, TagFolder tagFolder)
			{
				LinkName = linkName;
				TagFolders = new ObservableCollection<TagFolder> { tagFolder };
			}
		}

		private static readonly Dictionary<string, string> DefaultTags = new Dictionary<string, string>
		{
			{ "Red", "#FFF44336" },
			{ "Blue", "#FF2196F3" },
			{ "Green", "#FF4CAF50" },
			{ "Purple", "#FF9C27B0" },
			{ "Orange", "#FFFB8C00" },
			{ "Yellow", "#FFFFEB3B" }
		};
	}
}
