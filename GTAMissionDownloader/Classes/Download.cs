using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Google.Apis.Download;
using GTAMissionDownloader.Models;
using GTAMissionDownloader.ViewModels;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace GTAMissionDownloader.Classes
{
    class Download
    {
        private static MainViewModel _mvm;
        public Download(MainViewModel mvm)
        {
            _mvm = mvm;
        }

        public enum Option
        {
            ProgramUpdate = 0,
            MissionFile = 1
        }

        public static async Task FileAsync(string fileId, MissionModel selectedItem, CancellationToken cancellationToken, Option option = Option.MissionFile)
        {
            _mvm.MfRowHeight = new GridLength(0, GridUnitType.Auto);
            _mvm.IsProgressBarVisible = Visibility.Visible;
            _mvm.IsStopDownloadVisible = Visibility.Visible;

            var request = Helper.GetFileRequest(fileId, "size, name");
            request.MediaDownloader.ChunkSize = 1024 * 1024;

            var requestedFile = await request.ExecuteAsync();

            string mFPath = Path.Combine(Properties.GetArma3MissionFolderPath, requestedFile.Name);
            string programPath = string.Empty;
            if (option == Option.ProgramUpdate)
            {
                File.Move(Process.GetCurrentProcess().MainModule.FileName, Process.GetCurrentProcess().MainModule.FileName + "OLD.exe");
                programPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), requestedFile.Name + ".exe");
            }

            await using (MemoryStream stream = new MemoryStream())
            await using (FileStream file = new FileStream(option == Option.MissionFile ? mFPath : programPath, FileMode.Create, FileAccess.Write))
            {
                request.MediaDownloader.ProgressChanged += async progress =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading:
                            double bytesIn = progress.BytesDownloaded;
                            double currentValue = Math.Truncate(bytesIn / 1000000);
                            double totalValue = Math.Truncate((double)requestedFile.Size / 1000000);

                            await Application.Current.Dispatcher.BeginInvoke(() => _mvm.TaskbarManager.SetProgressState(TaskbarProgressBarState.Normal));
                            await Application.Current.Dispatcher.BeginInvoke(() => _mvm.TaskbarManager.SetProgressValue((int)currentValue, (int)totalValue));

                            _mvm.ProgressBarValue = Convert.ToDouble(progress.BytesDownloaded * 100 / requestedFile.Size);
                            _mvm.DownloadInfoText = $"Downloading '{requestedFile.Name}' - " + currentValue + "MB/" + totalValue + "MB";
                            break;

                        case DownloadStatus.Failed:
                            selectedItem.IsMissionUpdated = "Outdated";
                            selectedItem.IsModifiedTimeUpdated = "Outdated";

                            await Application.Current.Dispatcher.BeginInvoke(() => _mvm.TaskbarManager.SetProgressState(TaskbarProgressBarState.Error));
                            await Task.Delay(500);
                            await Application.Current.Dispatcher.BeginInvoke(() => _mvm.TaskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress));
                            break;

                        case DownloadStatus.Completed:
                            stream.WriteTo(file);

                            selectedItem.IsMissionUpdated = "Updated";
                            selectedItem.IsModifiedTimeUpdated = "Updated";

                            await Application.Current.Dispatcher.BeginInvoke(() => _mvm.TaskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress));
                            break;
                    }
                };

                try
                {
                    await request.DownloadAsync(stream, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    _mvm.MfRowHeight = new GridLength(0);
                    _mvm.ProgressBarValue = 0;
                    _mvm.IsProgressBarVisible = Visibility.Hidden;
                    _mvm.DownloadInfoText = string.Empty;
                    _mvm.IsStopDownloadVisible = Visibility.Hidden;

                    return;
                }
            }

            _mvm.MfRowHeight = new GridLength(0);
            _mvm.ProgressBarValue = 0;
            _mvm.IsProgressBarVisible = Visibility.Hidden;
            _mvm.DownloadInfoText = string.Empty;
            _mvm.IsStopDownloadVisible = Visibility.Hidden;

            if (option == Option.ProgramUpdate)
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
