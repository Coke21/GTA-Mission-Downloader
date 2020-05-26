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

        public static async Task FileAsync(string fileId, ListViewModel selectedItem, CancellationToken cancellationToken, string option = "missionFile")
        {
            _mvm.IsStopDownloadVisible = Visibility.Visible;

            var request = Helper.GetFileRequest(fileId, "size, name");
            request.MediaDownloader.ChunkSize = 10000000;

            var requestedFile = await request.ExecuteAsync();

            string mFPath = Path.Combine(Properties.GetArma3MissionFolderPath, requestedFile.Name);
            string programPath = Path.Combine(Properties.GetProgramFolderPath, requestedFile.Name + ".exe");

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

                            _mvm.DownloadInfoText = $"Downloading '{requestedFile.Name}' - " + currentValue + "MB/" + totalValue + "MB";
                            break;

                        case DownloadStatus.Failed:
                            if (selectedItem != null)
                            {
                                selectedItem.IsMissionUpdated = "Outdated";
                                selectedItem.IsModifiedTimeUpdated = "Outdated";
                            }

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
                File.Move(Properties.GetProgramFolderPath + Properties.GetProgramName, Properties.GetProgramFolderPath + Properties.GetProgramName + "OLD.exe");

                Process.Start(new ProcessStartInfo()
                {
                    FileName = programPath,
                    UseShellExecute = true
                });

                Process.Start(new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = "/C choice /C Y /N /D Y /T 3 & Del \"" + Properties.GetProgramFolderPath + Properties.GetProgramName + "OLD.exe",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                });

                await _mvm.CloseApp();
            }
        }
    }
}
