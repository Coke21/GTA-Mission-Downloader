﻿using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using GTAMissionDownloader.ViewModels;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace GTAMissionDownloader.Classes
{
    class Notification
    {
        private static MainViewModel _mvm;
        public Notification(MainViewModel mvm)
        {
            _mvm = mvm;

            Helper.MyNotifyIcon.ToolTipText = "GTA Mission Downloader";
            Helper.MyNotifyIcon.Icon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            Helper.MyNotifyIcon.TrayLeftMouseUp += (a, b) => NotifyIconBalloonTipClicked(true, true);
            Helper.MyNotifyIcon.TrayBalloonTipClicked += (a, b) => NotifyIconBalloonTipClicked(false, false);
        }
        private static async void NotifyIconBalloonTipClicked(bool stopOnStart, bool areFilesUpdated)
        {
            if (_mvm.IsStopDownloadVisible == Visibility.Visible)
                return;

            if (stopOnStart) 
                StopNotification();

            _mvm.WindowVisibility = Visibility.Visible;
            await Application.Current.Dispatcher.BeginInvoke(() => _mvm.WindowState = WindowState.Normal);

            try
            {
                if (areFilesUpdated)
                {
                    await Application.Current.Dispatcher.BeginInvoke(() => _mvm.TaskbarManager.SetProgressState(TaskbarProgressBarState.Indeterminate));
                    await Update.CheckFilesAsync(Helper.CtsOnStart.Token);
                    await Application.Current.Dispatcher.BeginInvoke(() => _mvm.TaskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress));
                }
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
