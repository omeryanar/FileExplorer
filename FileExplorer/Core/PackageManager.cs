using System;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm.CodeGenerators;
using FileExplorer.Properties;
using Onova;
using Onova.Models;
using Onova.Services;

namespace FileExplorer.Core
{
    [GenerateViewModel]
    public partial class PackageManager
    {
        public bool RestartRequired { get; set; }

        [GenerateProperty(SetterAccessModifier = AccessModifier.Private)]
        private UpdateStatus updateStatus;

        [GenerateProperty(SetterAccessModifier = AccessModifier.Private)]
        private double downloadPercentage;        

        public async Task<bool> CheckForUpdates(bool forceCheck = false, CancellationToken cancellationToken = default)
        {
            if (!forceCheck && DateTime.Now.Subtract(Settings.Default.LastUpdate).TotalHours < 12)
                return false;

            Settings.Default.LastUpdate = DateTime.Now;
            Settings.Default.Save();

            using (IUpdateManager updateManager = new UpdateManager(packageResolver, packageExtractor))
            {
                latestCheckForUpdatesResult = await updateManager.CheckForUpdatesAsync(cancellationToken);
                if (latestCheckForUpdatesResult.CanUpdate)
                    UpdateStatus = UpdateStatus.ReadyToDownload;

                return latestCheckForUpdatesResult.CanUpdate;
            }
        }

        public async Task<bool> PerformUpdate(CancellationToken cancellationToken = default)
        {
            using (IUpdateManager updateManager = new UpdateManager(packageResolver, packageExtractor))
            {
                if (latestCheckForUpdatesResult?.LastVersion == null)
                    return false;

                try
                {
                    UpdateStatus = UpdateStatus.DownloadInProgress;

                    Progress<double> downloadProgress = new Progress<double>();
                    downloadProgress.ProgressChanged += (s, e) => { DownloadPercentage = Math.Round(e, 2); };

                    await updateManager.PrepareUpdateAsync(latestCheckForUpdatesResult.LastVersion, downloadProgress, cancellationToken);
                    if (updateManager.IsUpdatePrepared(latestCheckForUpdatesResult.LastVersion))
                    {
                        UpdateStatus = UpdateStatus.ReadyToInstall;
                        return true;
                    }
                    else
                    {
                        UpdateStatus = UpdateStatus.Failed;
                        return false;
                    }
                }
                catch(TaskCanceledException)
                {
                    UpdateStatus = UpdateStatus.Cancelled;
                    return false;
                }
                catch(Exception)
                {
                    UpdateStatus = UpdateStatus.Failed;
                    return false;
                }
            }
        }

        public void LaunchUpdater()
        {
            if (latestCheckForUpdatesResult?.LastVersion != null)
            {
                using (IUpdateManager updateManager = new UpdateManager(packageResolver, packageExtractor))
                {
                    if (updateManager.IsUpdatePrepared(latestCheckForUpdatesResult.LastVersion))
                        updateManager.LaunchUpdater(latestCheckForUpdatesResult.LastVersion, RestartRequired);
                }
            }
        }

        private CheckForUpdatesResult latestCheckForUpdatesResult;

        private readonly IPackageExtractor packageExtractor = new ZipPackageExtractor();

        private readonly IPackageResolver packageResolver = new GithubPackageResolver("omeryanar", "FileExplorer", "*.zip");
    }
}
