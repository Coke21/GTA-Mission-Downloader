using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Google.Apis.Download;
using GTAMissionDownloader.Models;
using GTAMissionDownloader.ViewModels;

namespace GTAMissionDownloader.Classes
{
    class Download
    {
        private static MainViewModel _mvm;
        public Download(MainViewModel mvm)
        {
            _mvm = mvm;
        }

        public static async Task FileAsync(string fileId, MissionModel selectedItem, CancellationToken cancellationToken, string option = "missionFile")
        {
            _mvm.IsProgressBarVisible = Visibility.Visible;
            _mvm.IsStopDownloadVisible = Visibility.Visible;

            var request = Helper.GetFileRequest(fileId, "size, name");
            request.MediaDownloader.ChunkSize = 1024 * 1024;

            var requestedFile = await request.ExecuteAsync();

            string mFPath = Path.Combine(Properties.GetArma3MissionFolderPath, requestedFile.Name);
            string programPath = string.Empty;
            if (option == "programUpdate")
            {
                File.Move(Process.GetCurrentProcess().MainModule.FileName, Process.GetCurrentProcess().MainModule.FileName + "OLD.exe");
                programPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), requestedFile.Name + ".exe");
            }

            await using (MemoryStream stream = new MemoryStream())
            await using (FileStream file = new FileStream(option == "missionFile" ? mFPath : programPath, FileMode.Create, FileAccess.Write))
            {
                request.MediaDownloader.ProgressChanged += progress =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading:
                            double bytesIn = progress.BytesDownloaded;
                            double currentValue = Math.Truncate(bytesIn / 1000000);
                            double totalValue = Math.Truncate((double)requestedFile.Size / 1000000);

                            _mvm.ProgressBarValue = Convert.ToDouble(progress.BytesDownloaded * 100 / requestedFile.Size);
                            _mvm.DownloadInfoText = $"Downloading '{requestedFile.Name}' - " + currentValue + "MB/" + totalValue + "MB";
                            break;

                        case DownloadStatus.Failed:
                            if (selectedItem != null)
                            {
                                selectedItem.IsMissionUpdated = "Outdated";
                                selectedItem.IsModifiedTimeUpdated = "Outdated";
                            }

                            _mvm.ProgressBarValue = 0;
                            _mvm.IsProgressBarVisible = Visibility.Hidden;
                            _mvm.DownloadInfoText = string.Empty;
                            _mvm.IsStopDownloadVisible = Visibility.Hidden;
                            break;

                        case DownloadStatus.Completed:
                            stream.WriteTo(file);

                            if (selectedItem != null)
                            {
                                selectedItem.IsMissionUpdated = "Updated";
                                selectedItem.IsModifiedTimeUpdated = "Updated";
                            }

                            _mvm.ProgressBarValue = 0;
                            _mvm.IsProgressBarVisible = Visibility.Hidden;
                            _mvm.DownloadInfoText = string.Empty;
                            _mvm.IsStopDownloadVisible = Visibility.Hidden;
                            break;
                    }
                };
                try
                {
                    await request.DownloadAsync(stream, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
            if (option == "programUpdate")
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = programPath,
                    UseShellExecute = true
                });

                Process.Start(new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = "/C choice /C Y /N /D Y /T 3 & Del \"" + Process.GetCurrentProcess().MainModule.FileName + "OLD.exe",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                });

                await _mvm.CloseApp();
            }
        }
    }
}
