using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FileExplorer.Core;
using FileExplorer.Properties;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Shell32;

namespace FileExplorer.Helpers
{
    public class FileSystemHelper
    {
        public const string Separator = @"\";

        public const string UncPrefix = @"\\";

        public const string QuickAccessPath = "shell:::{679F85CB-0220-4080-B29B-5540CC05AAB6}";

        public const string ComputerPath = "shell:::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";

        public const string NetworkPath = "shell:::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}";

        public static readonly string[] SpecialExtenions = [".exe", ".ico", ".cur", ".lnk", ".url"];

        public static string[] UserFolders
        {
            get
            {
                if (userFolders == null)
                {
                    userFolders = 
                    [
                        ShellUtil.GetPathForKnownFolder(KNOWNFOLDERID.FOLDERID_Desktop.Guid()),
                        ShellUtil.GetPathForKnownFolder(KNOWNFOLDERID.FOLDERID_Documents.Guid()),
                        ShellUtil.GetPathForKnownFolder(KNOWNFOLDERID.FOLDERID_Downloads.Guid()),
                        ShellUtil.GetPathForKnownFolder(KNOWNFOLDERID.FOLDERID_Music.Guid()),
                        ShellUtil.GetPathForKnownFolder(KNOWNFOLDERID.FOLDERID_Pictures.Guid()),
                        ShellUtil.GetPathForKnownFolder(KNOWNFOLDERID.FOLDERID_Videos.Guid())
                    ];
                }

                return userFolders;
            }
        }
        private static string[] userFolders;

        public static bool IsDrive(string path)
        {
            return Regex.IsMatch(path, @"^[A-Za-z]:\\?$");
        }

        public static bool IsNetworkHost(string path)
        {
            return path.StartsWith(UncPrefix) && path.Split(Separator).Length == 1;
        }

        public static bool IsNetworkShare(string path)
        {
            return path.StartsWith(UncPrefix) && path.Split(Separator).Length == 2;
        }

        public static string GetHostName(string path)
        {
            return path.Split(Separator).First();
        }

        public static string GetFileParsingName(string path)
        {
            try
            {
                if (IsNetworkHost(path))
                    return path;

                using (ShellItem shellItem = new ShellItem(path))
                {
                    return shellItem.ParsingName;
                }
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public static string GetParentFolderPath(string path)
        {
            try
            {
                if (IsDrive(path))
                    return ComputerPath;

                if (IsNetworkHost(path))
                    return NetworkPath;

                if (IsNetworkShare(path))
                    return UncPrefix + GetHostName(path);

                return Path.GetDirectoryName(path);
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public static string[] GetFolderPaths(string path, bool includeHidden, bool includeSystem)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                if (!directoryInfo.Exists)
                    return [];

                DirectoryInfo[] directories = directoryInfo.GetDirectories();                

                if (includeHidden && includeSystem)
                    return directories.Select(x => x.FullName).ToArray();
                if (includeHidden && !includeSystem)
                    return directories.Where(x => !x.Attributes.HasFlag(FileAttributes.System)).Select(x => x.FullName).ToArray();
                if (!includeHidden && includeSystem)
                    return directories.Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden)).Select(x => x.FullName).ToArray();
                else
                    return directories.Where(x => !x.Attributes.HasFlag(FileAttributes.System) && !x.Attributes.HasFlag(FileAttributes.Hidden)).Select(x => x.FullName).ToArray();
            }
            catch (Exception)
            {
                return [];
            }
        }

        public static IList<string> GetQuickAccess()
        {
            List<string> folders = new List<string>();

            if (String.IsNullOrEmpty(Settings.Default.QuickAccessFolders))
                folders.AddRange(UserFolders);
            else if (Settings.Default.QuickAccessFolders.StartsWith(";"))
                folders.AddRange(Settings.Default.QuickAccessFolders.Split(";"));
            else
            {
                folders.AddRange(UserFolders);
                folders.AddRange(Settings.Default.QuickAccessFolders.Split(";"));
            }

            return folders;
        }

        public static string GetFileFriendlyDocName(string extension)
        {
            if (FrendlyDocNameCache.TryGetValue(extension, out string frendlyDocName))
                return frendlyDocName;

            try
            {
                uint bufferSize = 260;
                StringBuilder stringBuilder = new StringBuilder((int)bufferSize);

                ShlwApi.AssocQueryString(ShlwApi.ASSOCF.ASSOCF_NOTRUNCATE | ShlwApi.ASSOCF.ASSOCF_REMAPRUNDLL, ShlwApi.ASSOCSTR.ASSOCSTR_FRIENDLYDOCNAME,
                    extension, null, stringBuilder, ref bufferSize);

                frendlyDocName = stringBuilder.ToString();
                if (!String.IsNullOrEmpty(frendlyDocName))
                    FrendlyDocNameCache.TryAdd(extension, frendlyDocName);

                return frendlyDocName;
            }
            catch
            {
                return String.Empty;
            }
        }

        private static readonly ConcurrentDictionary<String, String> FrendlyDocNameCache = new ConcurrentDictionary<String, String>();
    }
}
