using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using DevExpress.Mvvm;
using FileExplorer.Messages;
using FileExplorer.Persistence;
using Vanara.Windows.Shell;

namespace FileExplorer.Core
{
	public class TagManager
	{
		public ICollection<TagFolder> GetTagFolders(string filePath)
		{
			if (TagDictionary.ContainsKey(filePath))
				return TagDictionary[filePath].TagFolders;

			return null;
		}

		public List<FileSystemInfo> GetTaggedFiles(TagFolder tagFolder)
		{
			List<FileSystemInfo> files = new List<FileSystemInfo>();

			foreach (string name in tagFolder.TaggedFiles)
			{
				FileInfo file = new FileInfo(Path.Combine(RootDirectory.FullName, $"{name}.lnk"));
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
				ShellLink link = ShellLink.Create(Path.Combine(RootDirectory.FullName, $"{Guid.NewGuid()}.lnk"), filePath);
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

		public TagManager()
		{
			RootDirectory = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tags"));
			if (!RootDirectory.Exists)
			{
				RootDirectory.Create();
				RootDirectory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
			}
			else
			{
				foreach (FileInfo file in RootDirectory.GetFiles("*.lnk"))
				{
					ShellLink shellLink = file.GetShellLinkFromFileInfo();
					if (shellLink == null)
						continue;

					foreach (TagFolder tagFolder in App.Repository.TagFolders)
					{
						if (tagFolder.TaggedFiles?.Any(x => file.Name.OrdinalEquals($"{x}.lnk")) == true)
							AddToDictionary(shellLink, tagFolder);
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
							foreach (TagFolder tagFolder in tagInfo.TagFolders)
								tagFolder.TaggedFiles?.Remove(tagInfo.LinkName);

							TagDictionary.Remove(message.Path);

							FileInfo file = new FileInfo(Path.Combine(RootDirectory.FullName, $"{tagInfo.LinkName}.lnk"));
							ShellLink link = file.GetShellLinkFromFileInfo();
							if (link != null)
							{
								TagDictionary.Add(link.TargetPath, tagInfo);
								foreach (TagFolder tagFolder in tagInfo.TagFolders)
									tagFolder.TaggedFiles.Add(tagInfo.LinkName);
							}
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

		private static DirectoryInfo RootDirectory;

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
	}

	public static class LinkHelper
	{
		public static ShellLink GetShellLinkFromFileInfo(this FileInfo fileInfo)
		{
			try
			{
				ShellLink shellLink = new ShellLink(fileInfo.FullName, LinkResolution.NoUI | LinkResolution.Update);
				if ((shellLink.IsFolder && Directory.Exists(shellLink.TargetPath)) || File.Exists(shellLink.TargetPath))
					return shellLink;

				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static FileSystemInfo GetTargetInfoFromLinkInfo(this FileInfo fileInfo)
		{
			try
			{
				ShellLink shellLink = new ShellLink(fileInfo.FullName, LinkResolution.NoUI | LinkResolution.Update);
				if (shellLink.IsFolder)
					return new DirectoryInfo(shellLink.TargetPath);
				else
					return new FileInfo(shellLink.TargetPath);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
