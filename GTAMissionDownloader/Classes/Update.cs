﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GTAMissionDownloader.Models;
using GTAMissionDownloader.ViewModels;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace GTAMissionDownloader.Classes
{
    class Update
    {
        private static MainViewModel _mvm;
        public Update(MainViewModel mvm)
        {
            _mvm = mvm;
        }

        public static async Task CheckFilesAsync(CancellationToken cancellationToken)
        {
            _mvm.IsLvEnabled = false;

            var listRequest = Helper.Service.Files.List();
            listRequest.OrderBy = "name";
            listRequest.Fields = "files(id, name, modifiedTime)";
            listRequest.Q = $"'{Properties.FolderId}' in parents";

            Google.Apis.Drive.v3.Data.FileList files = null;

            try
            {
                files = await listRequest.ExecuteAsync();
            }
            catch (Exception e)
            {
                if (_mvm.IsHideExceptionMissionChecked == false)
                    MessageBox.Show($"An Exception was thrown: {e.Message}\nIt was thrown while trying to connect to the Google servers!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            files.Files.Remove(files.Files.SingleOrDefault(r => r.Name == "readme.txt"));

            if (_mvm.IsRemoveMfsChecked)
            {
                var deadMissionFiles = _mvm.IgnoredItems.Where(i => files.Files.All(b => b.Id != i.FileId)).ToList();
                foreach (var item in deadMissionFiles)
                {
                    _mvm.IgnoredItems.Remove(item);
                    if (File.Exists(Properties.GetArma3MissionFolderPath + item.Item))
                        File.Delete(Properties.GetArma3MissionFolderPath + item.Item);
                }
            }

            foreach (var item in _mvm.IgnoredItems)
                files.Files.Remove(files.Files.SingleOrDefault(r => r.Id == item.FileId));

            if (_mvm.IsOrdered)
                files.Files = files.Files.Reverse().ToList();

            if (_mvm.MissionItems.Count > 0)
            {
                //Mission file added to the Google drive
                if (_mvm.MissionItems.Count < files.Files.Count)
                    foreach (var item in files.Files.ToList())
                    {
                        if (_mvm.MissionItems.Any(a => a.Mission == item.Name)) 
                            continue;

                        int itemPosition = files.Files.IndexOf(item);
                        _mvm.MissionItems.Insert(itemPosition, new MissionModel()
                        {
                            Mission = item.Name,
                            IsMissionUpdated = "Missing",
                            ModifiedTime = item.ModifiedTime.Value.ToString("dd.MM.yyyy HH:mm:ss"),
                            IsModifiedTimeUpdated = "Missing",
                            FileId = item.Id,
                            IsChecked = false
                        });
                    }

                //Mission file removed from the Google drive
                if (_mvm.MissionItems.Count > files.Files.Count)
                    foreach (var item in _mvm.MissionItems.ToList().Where(item => files.Files.All(a => a.Name != item.Mission)))
                    {
                        _mvm.MissionItems.Remove(item);
                        if (File.Exists(Properties.GetArma3MissionFolderPath + item.Mission))
                            File.Delete(Properties.GetArma3MissionFolderPath + item.Mission);
                    }

                //The same amount in both places
                if (_mvm.MissionItems.Count == files.Files.Count)
                    foreach (var (item1, item2) in _mvm.MissionItems.Zip(files.Files, Tuple.Create).ToList().Where(item => item.Item1.ModifiedTime != item.Item2.ModifiedTime.Value.ToString("dd.MM.yyyy HH:mm:ss")))
                    {
                        string status = await CheckItemAsync(item2.Name, item2.Id);
                        item1.Mission = item2.Name;
                        item1.IsMissionUpdated = status;
                        item1.ModifiedTime = item2.ModifiedTime.Value.ToString("dd.MM.yyyy HH:mm:ss");
                        item1.IsModifiedTimeUpdated = status;
                        item1.FileId = item2.Id;
                        item1.IsChecked = item1.IsChecked;
                    }

                foreach (var item in _mvm.MissionItems.ToList())
                {
                    string status = await CheckItemAsync(item.Mission, item.FileId);
                    item.IsMissionUpdated = status;
                    item.IsModifiedTimeUpdated = status;
                }
            }
            else
                foreach (var file in files.Files)
                {
                    string status = await CheckItemAsync(file.Name, file.Id);
                    _mvm.MissionItems.Add(new MissionModel()
                    {
                        Mission = file.Name,
                        IsMissionUpdated = status,
                        ModifiedTime = file.ModifiedTime.Value.ToString("dd.MM.yyyy HH:mm:ss"),
                        IsModifiedTimeUpdated = status,
                        FileId = file.Id,
                        IsChecked = false
                    });
                }

            _mvm.IsLvEnabled = true;

            var requestedProgram = await Helper.GetFileRequest(Properties.ProgramId, "md5Checksum").ExecuteAsync();
            string programMd5Checksum = CalculateMd5(Process.GetCurrentProcess().MainModule.FileName);

            if (cancellationToken.IsCancellationRequested) 
                return;

            if (Equals(requestedProgram.Md5Checksum, programMd5Checksum))
                _mvm.ProgramStatus = "Updated";
            else
            {
                _mvm.ProgramStatus = "Outdated";

                _mvm.IsUpdateVisible = Visibility.Visible;

                if (_mvm.IsUpdateNotifyChecked)
                {
                    var result = System.Windows.Forms.MessageBox.Show("A new update for GTA program has been detected. Download it?", "Update", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                        await Download.FileAsync(Properties.ProgramId, null, Helper.CtsStopDownloading.Token, Download.Option.ProgramUpdate);
                }
            }
        }

        private static async Task<string> CheckItemAsync(string fileName, string fileId)
        {
            var requestedFile = await Helper.GetFileRequest(fileId, "md5Checksum").ExecuteAsync();

            string filePath = Path.Combine(Properties.GetArma3MissionFolderPath, fileName);
            string fileMd5Checksum = CalculateMd5(filePath);

            if (string.IsNullOrWhiteSpace(fileMd5Checksum))
                return "Missing";

            return Equals(requestedFile.Md5Checksum, fileMd5Checksum) ? "Updated" : "Outdated";
        }
        private static string CalculateMd5(string filePath)
        {
            if (!File.Exists(filePath))
                return string.Empty;

            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        public static async Task ItemsAsync()
        {
            while (true)
            {
                try
                {
                    if (_mvm.IsProgressBarVisible == Visibility.Visible)
                    {
                        await Task.Delay(300_000);
                        continue;
                    }

                    await Application.Current.Dispatcher.BeginInvoke(() => _mvm.TaskbarManager.SetProgressState(TaskbarProgressBarState.Indeterminate));

                    await CheckFilesAsync(Helper.CtsOnStart.Token);

                    var checkedItems = _mvm.MissionItems.Where(ps => ps.IsChecked).ToList();
                    foreach (var item in checkedItems)
                    {
                        string status = await CheckItemAsync(item.Mission, item.FileId);
                        if (status.Equals("Updated")) 
                            continue;

                        await Download.FileAsync(item.FileId, item, Helper.CtsStopDownloading.Token);
                    }

                    await Application.Current.Dispatcher.BeginInvoke(() => _mvm.TaskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress));

                    //5 min - 300_000
                    await Task.Delay(300_000);
                }
                catch (Exception)
                {
                    _mvm.IsLvEnabled = true;
                    await Application.Current.Dispatcher.BeginInvoke(() => _mvm.TaskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress));
                }
            }
        }
    }
}
