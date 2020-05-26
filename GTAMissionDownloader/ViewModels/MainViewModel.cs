using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using GTADownloader;
using GTAMissionDownloader.Classes;
using GTAMissionDownloader.Models;
using GTAMissionDownloader.Views;
using Hardcodet.Wpf.TaskbarNotification;
using MahApps.Metro;
using Point = System.Windows.Point;

namespace GTAMissionDownloader.ViewModels
{
    public class MainViewModel : Screen
    {
        public string AppTitle => $"GTA Mission Downloader | {Properties.AppVersion} by Coke";
        public string IconPath => "/Images/gtaIcon.ico";
        public string WindowName => "PrimaryWindow";

        private WindowState windowState;
        public WindowState WindowState
        {
            get { return windowState; }
            set
            {
                windowState = value; 

                NotifyOfPropertyChange(() => WindowState);
            }
        }

        private Visibility _windowVisibility;
        public Visibility WindowVisibility
        {
            get { return _windowVisibility; }
            set
            {
                _windowVisibility = value;

                NotifyOfPropertyChange(() => WindowVisibility);
            }
        }


        private bool _showInTaskbar;
        public bool ShowInTaskbar
        {
            get { return _showInTaskbar; }
            set
            {
                _showInTaskbar = value;

                NotifyOfPropertyChange(() => ShowInTaskbar);
            }
        }

        //Second window move
        public void WindowLocationChanged()
        {
            var win = Application.Current.MainWindow;

            TsViewModel.TsVm.Top = win.Top;
            TsViewModel.TsVm.Left = win.Left + win.ActualWidth + 1;
        }

        public MainViewModel()
        {
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;
            IsUpdateVisible = Visibility.Hidden;
            IsStopDownloadVisible = Visibility.Hidden;

            new Download(this);
            new Notification(this);
            new Update(this);
            var tsViewModel = new TsViewModel(this);

            Directory.CreateDirectory(Properties.GetArma3FolderPath);
            Directory.CreateDirectory(Properties.GetArma3MissionFolderPath);

            Persistence.Tracker.Configure<MainViewModel>()
                .Id(p => p.WindowName, includeType: false)

                .Property(p => p.MissionItems, "Saved Mission file(s)")
                .Property(p => p.Accents, "Accent items")

                .Property(p => p.S1AddressText, "164.132.200.53", "Server 1 Address")
                .Property(p => p.S1PortText, "2302", "Server 1 Port")

                .Property(p => p.S2AddressText, "164.132.202.63", "Server 2 Address")
                .Property(p => p.S2PortText, "2602", "Server 2 Port")

                .Property(p => p.S3AddressText, "164.132.202.63", "Server 3 Address")
                .Property(p => p.S3PortText, "2302", "Server 3 Port")

                .Property(p => p.TsAddressText, "ts3server://TS.grandtheftarma.com:9987", "TeamSpeak Address")
                .Property(p => p.TsSelectorText, "#ipsLayout_sidebar > div > ul > li.ipsWidget.ipsWidget_vertical.ipsBox.ipsResponsive_block > div > div:nth-child(4) > a > span.ipsBadge.right", "TeamSpeak Selector")

                .Property(p => p.ThemeToggleSwitch, false, "Theme")
                .Property(p => p.SelectedAccentIndex, 1, "Selected Accent")

                .Property(p => p.IsStartUpChecked, false, "StartUp checkbox")
                .Property(p => p.IsHiddenChecked, false, "Hide at startup checkbox")
                .Property(p => p.IsTsChecked, false, "Run TS automatically checkbox")
                .Property(p => p.IsAutomaticUpdateChecked, false, "Automatic update checkbox")

                .PersistOn(nameof(PropertyChanged));

            Persistence.Tracker.Track(this);

            #region OnStart
            new Join(this, tsViewModel);
            ServersVerticalAlignment = VerticalAlignment.Center;

            if (Accents.Count == 0)
                foreach (var color in ThemeManager.ColorSchemes)
                    Accents.Add(new AccentsModel() {ColorName = color.Name});

            if (IsHiddenChecked)
            {
                Helper.MyNotifyIcon.ShowBalloonTip("Reminder!", "The program is running in the background!", BalloonIcon.Info);
                ShowInTaskbar = false;
                WindowVisibility = Visibility.Hidden;
            }

            if (IsTsChecked)
                if (Process.GetProcessesByName("ts3client_win64").Length == 0)
                    Join.Server("Ts");

            if (!IsAutomaticUpdateChecked)
                _ = Update.FilesCheckAsync(Helper.CtsOnStart.Token);
            #endregion
        }

        public async Task CloseApp() => await TryCloseAsync();

        private int _tabControlSelectedIndex;
        public int TabControlSelectedIndex
        {
            get { return _tabControlSelectedIndex; }
            set
            {
                _tabControlSelectedIndex = value; 

                NotifyOfPropertyChange(() => TabControlSelectedIndex);
            }
        }
        public void TabControlSelectionChanged(SelectionChangedEventArgs e)
        {
            if (TabControlSelectedIndex == 1) 
                return;

            IsExpanderOpened = false;
        }

        private string _programStatus;
		public string ProgramStatus
		{
			get { return _programStatus; }
            set
            {
                _programStatus = value;

                NotifyOfPropertyChange(() => ProgramStatus);
            }
		}

        public void ReadChangelog()
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "https://docs.google.com/document/d/1HzbVqK26YLsJtSBC2XJ7s_VcQ9IWH9ZWy3LEGEDwrJk/edit",
                UseShellExecute = true
            });
        }

        public async Task UpdateProgramAsync()
        {
            await Download.FileAsync(Properties.ProgramId, null, Helper.CtsStopDownloading.Token, "programUpdate");
        }

        private Visibility _isUpdateVisible;
        public Visibility IsUpdateVisible
        {
            get { return _isUpdateVisible; }
            set
            {
                _isUpdateVisible = value;

                NotifyOfPropertyChange(() => IsUpdateVisible);
            }
        }

        //ListView
        public BindableCollection<ListViewModel> MissionItems { get; set; } = new BindableCollection<ListViewModel>();

        private bool _isLvEnabled;
        public bool IsLvEnabled
        {
            get { return _isLvEnabled; }
            set
            {
                _isLvEnabled = value;

                NotifyOfPropertyChange(() => IsLvEnabled);
            }
        }

        public void LvMouseDown(MainView sender, Point e)
        {
            HitTestResult r = VisualTreeHelper.HitTest(sender, e);
            if (r.VisualHit.GetType() != typeof(ListViewItem))
                foreach (var item in MissionItems)
                    item.IsSelected = false;
        }

        public async Task DownloadMission()
        {
            if (IsAutomaticUpdateChecked)
            {
                MessageBox.Show("You cannot have the automatic update checkbox ticked! Untick the checkbox to manually download a file!", "Information", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }

            if (IsStopDownloadVisible == Visibility.Visible)
            {
                MessageBox.Show("Only one download instance is allowed!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var item in MissionItems)
                if (item.IsSelected)
                    await Download.FileAsync(item.FileId, item, Helper.CtsStopDownloading.Token);
        }

        public void DeleteMission()
        {
            foreach (var item in MissionItems)
                if (item.IsSelected)
                {
                    File.Delete(Properties.GetArma3MissionFolderPath + item.Mission);
                    item.IsMissionUpdated = "Missing";
                    item.IsModifiedTimeUpdated = "Missing";
                }
        }

        public void InfoClick() => MessageBox.Show("These are the current colors and the meaning behind them in the list:\n" +
                                                              "Green - You have the updated version of the mission file.\n" +
                                                              "Red - You have the outdated version of the mission file.\n" +
                                                              "Orange - You don't have the mission file on your PC.\n\n" +

                                                              "Subscription of the mission files:\n" +
                                                              "1.Choose the mission files that you want to observe.\n" +
                                                              "2.Tick them.\n" +
                                                              "3.Go to Options tab and tick the Automatic Update checkbox.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

        //Below ListView
        private string _downloadInfoText;
        public string DownloadInfoText
        {
            get { return _downloadInfoText; }
            set
            {
                _downloadInfoText = value;

                NotifyOfPropertyChange(() => DownloadInfoText);
            }
        }

        public void StopDownload()
        {
            Helper.CtsStopDownloading.Cancel();
            Helper.CtsStopDownloading.Dispose();
            Helper.CtsStopDownloading = new CancellationTokenSource();
        }

        private Visibility _isStopDownloadVisible;
        public Visibility IsStopDownloadVisible
        {
            get { return _isStopDownloadVisible; }
            set
            {
                _isStopDownloadVisible = value;

                NotifyOfPropertyChange(() => IsStopDownloadVisible);
            }
        }

        //Servers tab
        private VerticalAlignment _serversVerticalAlignment;
        public VerticalAlignment ServersVerticalAlignment
        {
            get { return _serversVerticalAlignment; }
            set
            {
                _serversVerticalAlignment = value;

                NotifyOfPropertyChange(() => ServersVerticalAlignment);
            }
        }

        public void JoinServer1() => Join.Server("S1");

        private bool _isJoinServer1Enabled;
        public bool IsJoinServer1Enabled
        {
            get { return _isJoinServer1Enabled; }
            set
            {
                _isJoinServer1Enabled = value;

                NotifyOfPropertyChange(() => IsJoinServer1Enabled);
            }
        }

        public void JoinServer2() => Join.Server("S2");

        private bool _isJoinServer2Enabled;
        public bool IsJoinServer2Enabled
        {
            get { return _isJoinServer2Enabled; }
            set
            {
                _isJoinServer2Enabled = value;

                NotifyOfPropertyChange(() => IsJoinServer2Enabled);
            }
        }

        public void JoinServer3() => Join.Server("S3");

        private bool _isJoinServer3Enabled;
        public bool IsJoinServer3Enabled
        {
            get { return _isJoinServer3Enabled; }
            set
            {
                _isJoinServer3Enabled = value;

                NotifyOfPropertyChange(() => IsJoinServer3Enabled);
            }
        }

        public void JoinTs() => Join.Server("Ts");

        private bool _isJoinTsEnabled;
        public bool IsJoinTsEnabled
        {
            get { return _isJoinTsEnabled; }
            set
            {
                _isJoinTsEnabled = value;

                NotifyOfPropertyChange(() => IsJoinTsEnabled);
            }
        }


        private string _server1Info;
        public string Server1Info
        {
            get { return _server1Info; }
            set
            {
                _server1Info = value;

                NotifyOfPropertyChange(() => Server1Info);
            }
        }

        private string _server2Info;
        public string Server2Info
        {
            get { return  _server2Info; }
            set
            {
                _server2Info = value;

                NotifyOfPropertyChange(() => Server2Info);
            }
        }

        private string _server3Info;
        public string Server3Info
        {
            get { return _server3Info; }
            set
            {
                _server3Info = value;

                NotifyOfPropertyChange(() => Server3Info);
            }
        }

        private string _tsInfo;
        public string TsInfo
        {
            get { return _tsInfo; }
            set
            {
                _tsInfo = value;

                NotifyOfPropertyChange(() => TsInfo);
            }
        }

        public void ShowServersSettings()
        {
            IsServerFlyOutOpened = true;
            ServersVerticalAlignment = VerticalAlignment.Top;
        }

        //Flyout shit
        private bool _isServerFlyOutOpened;
        public bool IsServerFlyOutOpened
        {
            get { return _isServerFlyOutOpened; }
            set
            {
                _isServerFlyOutOpened = value;

                if (!IsServerFlyOutOpened)
                    ServersVerticalAlignment = VerticalAlignment.Center;

                NotifyOfPropertyChange(() => IsServerFlyOutOpened);
            }
        }

        private string _s1AddressText;
        public string S1AddressText
        {
            get { return _s1AddressText; }
            set
            {
                _s1AddressText = value;

                NotifyOfPropertyChange(() => S1AddressText);
            }
        }

        private string _s1PortText;
        public string S1PortText
        {
            get { return _s1PortText; }
            set
            {
                _s1PortText = value;

                NotifyOfPropertyChange(() => S1PortText);
            }
        }

        private string _s2AddressText;
        public string S2AddressText
        {
            get { return _s2AddressText; }
            set
            {
                _s2AddressText = value;

                NotifyOfPropertyChange(() => S2AddressText);
            }
        }

        private string _s2PortText;
        public string S2PortText
        {
            get { return _s2PortText; }
            set
            {
                _s2PortText = value;

                NotifyOfPropertyChange(() => S2PortText);
            }
        }

        private string _s3AddressText;
        public string S3AddressText
        {
            get { return _s3AddressText; }
            set
            {
                _s3AddressText = value;

                NotifyOfPropertyChange(() => S3AddressText);
            }
        }

        private string _s3PortText;
        public string S3PortText
        {
            get { return _s3PortText; }
            set
            {
                _s3PortText = value;

                NotifyOfPropertyChange(() => S3PortText);
            }
        }

        private string _tsAddressText;
        public string TsAddressText
        {
            get { return _tsAddressText; }
            set
            {
                _tsAddressText = value;

                NotifyOfPropertyChange(() => TsAddressText);
            }
        }

        private string _tsSelectorText;
        public string TsSelectorText
        {
            get { return _tsSelectorText; }
            set
            {
                _tsSelectorText = value;

                NotifyOfPropertyChange(() => TsSelectorText);
            }
        }

        public void FlyOutInfoClick()
        {
            MessageBox.Show(
@"The First Box:
Specify TeamSpeak address.
The Second Box:
If you want to see the current number on TS, you need to specify the selector.
This is how you can get it:
1.Go to main GTA website
2.Right click on Number/256 and click Inspect
3.Right click on highlited HTML Elements, copy, copy selector
4.Paste in the field.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private Brush _expanderColor;
        public Brush ExpanderColor
        {
            get { return _expanderColor; }
            set
            {
                _expanderColor = value; 

                NotifyOfPropertyChange(() => ExpanderColor);
            }
        }
        private bool _isExpanderOpened;
        public bool IsExpanderOpened
        {
            get { return _isExpanderOpened; }
            set
            {
                _isExpanderOpened = value;

                if (IsExpanderOpened)
                {
                    var win = Application.Current.MainWindow;

                    TsViewModel.TsVm.Top = win.Top;
                    TsViewModel.TsVm.Left = win.Left + win.ActualWidth + 1;

                    dynamic settings = new ExpandoObject();
                    settings.Owner = win;

                    Helper.Manager.ShowWindowAsync(TsViewModel.TsVm, null, settings);
                }
                else
                    _= TsViewModel.TsVm.CloseWindow();

                NotifyOfPropertyChange(() => IsExpanderOpened);
            }
        }

        //Options tab
        private bool _themeToggleSwitch;
        public bool ThemeToggleSwitch
        {
            get { return _themeToggleSwitch; }
            set
            {
                _themeToggleSwitch = value;

                string theme = ThemeToggleSwitch switch
                {
                    false => "Dark",
                    true => "Light"
                };

                ExpanderColor = ThemeToggleSwitch switch
                {
                    false => Brushes.White,
                    true => Brushes.Black
                };

                ThemeManager.ChangeTheme(Application.Current, theme, ThemeManager.DetectTheme().ColorScheme);

                NotifyOfPropertyChange(() => ThemeToggleSwitch);
            }
        }

        public BindableCollection<AccentsModel> Accents { get; set; } = new BindableCollection<AccentsModel>();

        private AccentsModel _selectedAccent;
        public AccentsModel SelectedAccent
        {
            get { return _selectedAccent; }
            set
            {
                _selectedAccent = value;

                var appStyle = ThemeManager.DetectTheme();
                ThemeManager.ChangeTheme(Application.Current, appStyle.BaseColorScheme, SelectedAccent.ColorName);

                NotifyOfPropertyChange(() => SelectedAccent);
            }
        }

        private int _selectedAccentIndex;
        public int SelectedAccentIndex
        {
            get { return _selectedAccentIndex; }
            set
            {
                _selectedAccentIndex = value; 

                NotifyOfPropertyChange(() => SelectedAccentIndex);
            }
        }

        private bool _isStartUpChecked;
        public bool IsStartUpChecked
        {
            get { return _isStartUpChecked; }
            set
            {
                _isStartUpChecked = value;

                if (IsStartUpChecked)
                    Properties.KeyStartUp.SetValue("GTADownloader", System.Reflection.Assembly.GetExecutingAssembly().Location);
                else
                    Properties.KeyStartUp.DeleteValue("GTADownloader", false);

                NotifyOfPropertyChange(() => IsStartUpChecked);
            }
        }

        private bool _isStartUpEnabled;
        public bool IsStartUpEnabled
        {
            get { return _isStartUpEnabled; }
            set
            {
                _isStartUpEnabled = value; 

                NotifyOfPropertyChange(() => IsStartUpEnabled);
            }
        }


        private bool _isHiddenChecked;
        public bool IsHiddenChecked
        {
            get { return _isHiddenChecked; }
            set
            {
                _isHiddenChecked = value;

                if (IsHiddenChecked)
                {
                    IsStartUpChecked = true;
                    IsStartUpEnabled = false;
                }
                else
                {
                    IsStartUpEnabled = true;
                    ShowInTaskbar = true;
                }

                NotifyOfPropertyChange(() => IsHiddenChecked);
            }
        }

        private bool _isTsChecked;
        public bool IsTsChecked
        {
            get { return _isTsChecked; }
            set
            {
                _isTsChecked = value;

                NotifyOfPropertyChange(() => IsTsChecked);
            }
        }

        private bool _isAutomaticUpdateChecked;
        public bool IsAutomaticUpdateChecked
        {
            get { return _isAutomaticUpdateChecked; }
            set
            {
                _isAutomaticUpdateChecked = value;

                if (IsAutomaticUpdateChecked)
                {
                    try
                    {
                        _ = Update.UpdateLvItemsCheckAsync(Helper.CtsStopDownloading.Token);
                    }
                    catch (IOException)
                    {
                        IsAutomaticUpdateChecked = false;
                    }
                }

                NotifyOfPropertyChange(() => IsAutomaticUpdateChecked);
            }
        }

        public void OpenConfigFolder()
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = @Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/../Roaming/GTADownloader",
                UseShellExecute = true
            });
        }
        public void OpenMissionFileFolder()
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = Properties.GetArma3MissionFolderPath,
                UseShellExecute = true
            });
        }
        public void ManualDownload()
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "https://drive.google.com/drive/u/2/folders/1i8rxUqM7NRaO8hnexDDrQm5zYlWffbXy",
                UseShellExecute = true
            });
        }

        public void BrowseOfficialThread()
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "https://grandtheftarma.com/topic/116196-gta-mission-downloader/",
                UseShellExecute = true
            });
        }
    }
}
