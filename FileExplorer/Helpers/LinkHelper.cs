using System;
using System.IO;
using Lnk;
using Vanara.Windows.Shell;

namespace FileExplorer.Helpers
{
	public static class LinkHelper
	{
		public static ShellLink GetShellLinkFromFileInfo(this FileSystemInfo fileInfo, bool update = true)
		{
			try
			{
				LinkResolution linkResolution = LinkResolution.NoUI;
				if (update)
					linkResolution |= LinkResolution.Update;
				else
					linkResolution |= LinkResolution.NoUpdate | LinkResolution.NoSearch;

				return new ShellLink(fileInfo.FullName, linkResolution);
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static FileSystemInfo GetTargetInfoFromLinkInfo(this FileSystemInfo fileInfo, bool update = true)
		{
			try
			{
				using (ShellLink shellLink = fileInfo.GetShellLinkFromFileInfo(update))
				{
					if (shellLink.Target.IsFolder && Directory.Exists(shellLink.TargetPath))
						return new DirectoryInfo(shellLink.TargetPath);
					else if (File.Exists(shellLink.TargetPath))
						return new FileInfo(shellLink.TargetPath);
				}

				return new LinkFileInfo(fileInfo.FullName);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}

	public class LinkFileInfo : FileSystemInfo
	{
		public override string Name { get; }

		public override bool Exists { get; }

		public override void Delete()
		{
			throw new NotImplementedException();
		}

		public new DateTime CreationTime { get; }

		public new DateTime LastWriteTime { get; }

		public new DateTime LastAccessTime { get; }

		public new FileAttributes Attributes { get; }

		public DirectoryInfo Parent { get; }

		public string LinkPath { get; }

		public long Length { get; }

		public LinkFileInfo(string path)
		{
			LinkPath = path;
			LnkFile lnkFile = Lnk.Lnk.LoadFile(path);

			FullPath = lnkFile.LocalPath;
			OriginalPath = lnkFile.LocalPath;
			Name = Path.GetFileName(FullName);
			Attributes = (FileAttributes)lnkFile.Header.FileAttributes;
			Parent = new DirectoryInfo(Path.GetDirectoryName(FullName));

			if (Attributes.HasFlag(FileAttributes.Directory))
				Exists = Directory.Exists(FullName);
			else
				Exists = File.Exists(FullName);

			Length = lnkFile.Header.FileSize;

			CreationTime = lnkFile.Header.TargetCreationDate.DateTime;
			LastWriteTime = lnkFile.Header.TargetModificationDate.DateTime;
			LastAccessTime = lnkFile.Header.TargetLastAccessedDate.DateTime;
		}
	}
}
