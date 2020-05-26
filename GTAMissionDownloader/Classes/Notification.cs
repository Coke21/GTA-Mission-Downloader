using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using GTAMissionDownloader.ViewModels;

namespace GTAMissionDownloader.Classes
{
    class Notification
    {
        private static MainViewModel _mvm;
        public Notification(MainViewModel mvm)
        {
            _mvm = mvm;

            Helper.MyNotifyIcon.ToolTipText = "GTA Mission Downloader";
            Helper.MyNotifyIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().ManifestModule.Name);
            Helper.MyNotifyIcon.TrayLeftMouseUp += (a, b) => NotifyIconBalloonTipClicked(true, true);
            Helper.MyNotifyIcon.TrayBalloonTipClicked += (a, b) => NotifyIconBalloonTipClicked(false, false);
        }
        private static async void NotifyIconBalloonTipClicked(bool stopOnStart, bool areFilesUpdated)
        {
            if (stopOnStart) 
                StopNotification();

            _mvm.WindowState = WindowState.Normal;
            _mvm.WindowVisibility = Visibility.Visible;
            try
            {
                if (areFilesUpdated)
                    await Update.FilesCheckAsync(Helper.CtsOnStart.Token);
            }
            catch (IOException)
            {
            }
        }
        private static void StopNotification()
        {
            Helper.CtsOnStart.Cancel();
            Helper.CtsOnStart.Dispose();
            Helper.CtsOnStart = new CancellationTokenSource();

            _mvm.ProgramStatus = string.Empty;
            _mvm.IsUpdateVisible = Visibility.Hidden;
        }
    }
}
