using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using ControlzEx.Theming;
using GTADownloader;
using GTAMissionDownloader.Classes;
using GTAMissionDownloader.Models;
using GTAMissionDownloader.Views;
using Hardcodet.Wpf.TaskbarNotification;
using Point = System.Windows.Point;

namespace GTAMissionDownloader.ViewModels
{
    public class MainViewModel : Screen
    {
        public string AppTitle => $"GTA Mission Downloader | {Properties.AppVersion} by Coke";
        public string IconPath => "/Images/gtaIcon.ico";
        public string WindowName => "PrimaryWindow";

        private double _height;
        public double Height
        {
            get { return _height; }
            set
            {
                _height = value;

                NotifyOfPropertyChange(() => Height);
            }
        }

        private double _width;
        public double Width
        {
            get { return _width; }
            set
            {
                _width = value;

                NotifyOfPropertyChange(() => Width);
            }
        }

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

        //Main window stuff
        public void WindowLocationChanged()
        {
            var win = Application.Current.MainWindow;

            TsViewModel.TsVm.Top = win.Top;
            TsViewModel.TsVm.Left = win.Left + win.ActualWidth + 1;
        }
        public void WindowSizeChanged() => WindowLocationChanged();
        public void WindowStateChanged()
        {
            if (WindowState == WindowState.Minimized)
                if (IsHiddenChecked && ShowInTaskbar)
                {
                    ShowInTaskbar = false;
                    WindowVisibility = Visibility.Hidden;
                }
        }

        //Constructor
        private TsViewModel _tsViewModel;
        public MainViewModel()
        {
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;
            IsUpdateVisible = Visibility.Hidden;
            IsStopDownloadVisible = Visibility.Hidden;
            IsAutomaticUpdateEnabled = true;

            new Download(this);
            new Notification(this);
            new Update(this);
            _tsViewModel = new TsViewModel(this);

            Directory.CreateDirectory(Properties.GetArma3FolderPath);
            Directory.CreateDirectory(Properties.GetArma3MissionFolderPath);

            Persistence.Tracker.Configure<MainViewModel>()
                .Id(p => p.WindowName, includeType: false)
                .Property(p => p.Height, 430, "Window Height")
                .Property(p => p.Width, 900, "Window Width")

                .Property(p => p.MissionItems, "Saved Mission File(s)")
                .Property(p => p.MfColumnWidth, new GridLength(290, GridUnitType.Pixel), "GridSplitter Column Width")
                //.Property(p => p.IsSubscribeAllChecked, false, "Subscribe All Toggle Button")

                .Property(p => p.IgnoredItems, "Ignored Item(s)")

                .Property(p => p.TsSelectorUrlText, "https://grandtheftarma.com/", "TeamSpeak Selector URL")
                .Property(p => p.Servers, "Saved Servers")

                .Property(p => p.ThemeToggleSwitch, false, "Theme")
                .Property(p => p.Accents, "Accent Items")
                .Property(p => p.SelectedAccentIndex, 1, "Selected Accent")

                .Property(p => p.IsStartUpChecked, false, "StartUp Checkbox")
                .Property(p => p.IsHiddenChecked, false, "Hide at Startup Checkbox")
                .Property(p => p.IsServerChecked, false, "Join Game Server Automatically Checkbox")
                .Property(p => p.IsTsChecked, false, "Run TS Automatically Checkbox")
                .Property(p => p.IsAutomaticUpdateChecked, false, "Automatic Update Checkbox")
                .Property(p => p.UpdateNotify, true, "Update Notify Checkbox")

                .PersistOn(nameof(PropertyChanged));

            Persistence.Tracker.Track(this);

            #region OnStart
            new Join(this, _tsViewModel);

            if (Accents.Count == 0)
                foreach (var color in ThemeManager.Current.ColorSchemes)
                    Accents.Add(new AccentsModel() {ColorName = color});

            if (IsHiddenChecked)
            {
                Helper.MyNotifyIcon.ShowBalloonTip("Reminder!", "The program is running in the background!", BalloonIcon.Info);
                ShowInTaskbar = false;
                WindowVisibility = Visibility.Hidden;
            }

            if (IsServerChecked)
                if (Process.GetProcessesByName("arma3_x64").Length == 0 && Process.GetProcessesByName("arma3").Length == 0 && Process.GetProcessesByName("arma3launcher").Length == 0)
                    foreach (var server in Servers)
                    {
                        if (server.ContentButton == "Join TeamSpeak")
                            continue;

                        Join.Server(server);
                        break;
                    }

            if (IsTsChecked)
                if (Process.GetProcessesByName("ts3client_win64").Length == 0)
                    foreach (var server in Servers)
                    {
                        if (server.ContentButton != "Join TeamSpeak") 
                            continue;

                        Join.Server(server);
                        break;
                    }

            if (!IsAutomaticUpdateChecked)
                _= Update.FilesCheckAsync(Helper.CtsOnStart.Token);
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

        public async Task UpdateProgramAsync() => await Download.FileAsync(Properties.ProgramId, null, Helper.CtsStopDownloading.Token, "programUpdate");

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

        //Mfs ListView
        public BindableCollection<MissionModel> MissionItems { get; set; } = new BindableCollection<MissionModel>();

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
        public void LvMouseMoveDragDrop(MainView view)
        {
            if (Mouse.LeftButton != MouseButtonState.Pressed)
                return;

            List<MissionModel> myList = new List<MissionModel>();
            foreach (var missionItem in MissionItems)
                if (missionItem.IsSelected)
                    myList.Add(missionItem);

            if (myList.Count == 0)
                return;

            DragDrop.DoDragDrop(view, new DataObject(myList), DragDropEffects.Copy);
        }
        public async Task MissionDropLv(DragEventArgs dragArgs)
        {
            if (!dragArgs.Data.GetDataPresent(typeof(List<IgnoredModel>)))
                return;

            var droppedItems = dragArgs.Data.GetData(typeof(List<IgnoredModel>)) as List<IgnoredModel>;

            foreach (var droppedItem in droppedItems)
                IgnoredItems.Remove(droppedItem);

            await Update.FilesCheckAsync(Helper.CtsOnStart.Token);
        }
        public void LvMfSizeChanged(ListView listView)
        {
            GridView gridView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - 5;
            var col1 = 0.50;
            var col2 = 0.25;
            var col3 = 0.20;
            var col4 = 0.05;

            if (workingWidth < 0)
                return;

            gridView.Columns[0].Width = workingWidth * col1;
            gridView.Columns[1].Width = workingWidth * col2;
            gridView.Columns[2].Width = workingWidth * col3;
            gridView.Columns[3].Width = workingWidth * col4;
        }

        public async Task DownloadMission()
        {
            if (IsAutomaticUpdateChecked)
            {
                MessageBox.Show("You cannot have the Automatic Update Checkbox (in Options) ticked! Untick the checkbox to manually download a file!", "Information", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }

            if (IsStopDownloadVisible == Visibility.Visible)
            {
                MessageBox.Show("Only one download instance is allowed!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsAutomaticUpdateEnabled = false;

            foreach (var item in MissionItems)
                if (item.IsSelected)
                    await Download.FileAsync(item.FileId, item, Helper.CtsStopDownloading.Token);

            IsAutomaticUpdateEnabled = true;
        }

        public void DeleteMission()
        {
            foreach (var item in MissionItems)
                if (item.IsSelected)
                {
                    try
                    {
                        File.Delete(Properties.GetArma3MissionFolderPath + item.Mission);   
                        item.IsMissionUpdated = "Missing";
                        item.IsModifiedTimeUpdated = "Missing";
                    }
                    catch (IOException e)
                    {
                        MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
        }

        //private bool _isSubscribeAllChecked;
        //public bool IsSubscribeAllChecked
        //{
        //    get { return _isSubscribeAllChecked; }
        //    set
        //    {
        //        _isSubscribeAllChecked = value;

        //        NotifyOfPropertyChange(() => IsSubscribeAllChecked);
        //    }
        //}

        public void SubscribeAll()
        {
            foreach (var mission in MissionItems)
                mission.IsChecked = true;
        }

        public void UnSubscribeAll()
        {
            foreach (var mission in MissionItems)
                mission.IsChecked = false;
        }

        public void SubscribeUnChecked()
        {
            var checkedItems = MissionItems.Where(ps => ps.IsChecked).ToList();
            if (!checkedItems.Any())
                IsAutomaticUpdateChecked = false;
        }

        public void InfoClick() => MessageBox.Show("These are the current colors and the meaning behind them in the list:\n" +
                                                              "Green - You have the updated version of the mission file.\n" +
                                                              "Red - You have the outdated version of the mission file.\n" +
                                                              "Orange - You don't have the mission file on your PC.\n\n" +

                                                              "Subscription of the mission files:\n" +
                                                              "1.Choose the mission files that you want to observe.\n" +
                                                              "2.Tick them.\n" +
                                                              "3.Go to Options tab and tick the Automatic Update checkbox.\n\n" +
                                                              
                                                              "If you want to ignore certain mission files, you just drag & drop them on the right list.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

        //GridSplitter
        private GridLength _mfColumnWidth;
        public GridLength MfColumnWidth
        {
            get { return _mfColumnWidth; }
            set
            {
                _mfColumnWidth = value;

                NotifyOfPropertyChange(() => MfColumnWidth);
            }
        }

        //Ignore Listview
        public BindableCollection<IgnoredModel> IgnoredItems { get; set; } = new BindableCollection<IgnoredModel>();

        public void IgnoreLvMouseDown(MainView sender, Point e)
        {
            HitTestResult r = VisualTreeHelper.HitTest(sender, e);
            if (r.VisualHit.GetType() != typeof(ListViewItem))
                foreach (var item in IgnoredItems)
                    item.IsSelected = false;
        }
        public void LvIgnoredMouseMoveDragDrop(MainView view)
        {
            if (Mouse.LeftButton != MouseButtonState.Pressed)
                return;

            List<IgnoredModel> myList = new List<IgnoredModel>();
            foreach (var missionItem in IgnoredItems)
                if (missionItem.IsSelected)
                    myList.Add(missionItem);

            if (myList.Count == 0)
                return;

            DragDrop.DoDragDrop(view, new DataObject(myList), DragDropEffects.Copy);
        }
        public void ItemDropLv(DragEventArgs dragArgs)
        {
            if (!dragArgs.Data.GetDataPresent(typeof(List<MissionModel>)))
                return;

            var droppedItems = dragArgs.Data.GetData(typeof(List<MissionModel>)) as List<MissionModel>;

            foreach (var droppedItem in droppedItems)
            {
                if (IgnoredItems.Any(item => item.FileId == droppedItem.FileId))
                    continue;

                IgnoredItems.Add(new IgnoredModel()
                {
                    Item = droppedItem.Mission,
                    FileId = droppedItem.FileId
                });

                MissionItems.Remove(droppedItem);
            }
        }
        public void LvIgnoredSizeChanged(ListView listView)
        {
            GridView gridView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - 5;
            var col1 = 1;

            if (workingWidth < 0)
                return;

            gridView.Columns[0].Width = workingWidth * col1;
        }

        public async Task DeleteIgnoredItem()
        {
            foreach (var item in IgnoredItems.ToList())
                if (item.IsSelected)
                    IgnoredItems.Remove(item);

            await Update.FilesCheckAsync(Helper.CtsOnStart.Token);
        }

        //Below ListViews
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
        private bool _isServerFlyOutOpened;
        public bool IsServerFlyOutOpened
        {
            get { return _isServerFlyOutOpened; }
            set
            {
                _isServerFlyOutOpened = value;

                NotifyOfPropertyChange(() => IsServerFlyOutOpened);
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

        private string _tsSelectorUrlText;
        public string TsSelectorUrlText
        {
            get { return _tsSelectorUrlText; }
            set
            {
                _tsSelectorUrlText = value;

                NotifyOfPropertyChange(() => TsSelectorUrlText);
            }
        }

        public void AddTs()
        {
            if (string.IsNullOrWhiteSpace(ServerIpText) || string.IsNullOrWhiteSpace(TsSelectorUrlText))
                return;

            var tsItem = Servers.FirstOrDefault(i => i.ServerIp == ServerIpText);

            if (tsItem != null)
            {
                MessageBox.Show("There can be only one instance of the same TS server!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Servers.All(item => item.ServerIp != ServerIpText))
                Servers.Add(new ServersModel()
                {
                    ContentButton = "Join TeamSpeak",
                    IsJoinButtonEnabled = false,
                    JoinServerToolTip = $@"Join: {_tsViewModel.TsChannelNameText}
Password: {_tsViewModel.TsChannelPasswordText}",

                    ServerInfo = "Loading...",
                    ServerInfoToolTip = $@"Server Address: {ServerIpText}
TeamSpeak Selector: {TsSelectorText}
TeamSpeak Selector URL: {TsSelectorUrlText}",

                    ServerIp = ServerIpText,
                    TsSelector = TsSelectorText,
                    TsSelectorUrl = TsSelectorUrlText
                });
            else
                MessageBox.Show("The same instance of the TS server is already in the list!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    _ = TsViewModel.TsVm.CloseWindow();

                NotifyOfPropertyChange(() => IsExpanderOpened);
            }
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

        public void ShowTeamSpeakSettings() => IsServerFlyOutOpened = true;

        public void AddGtaServers()
        {
            if (Servers.Any(server => server.ServerIp == "164.132.200.53" || server.ServerIp == "164.132.202.63" || server.ServerIp == "TS.grandtheftarma.com:9987"))
                return;

            Servers.Add(new ServersModel()
            {
                ContentButton = "Join Server",
                IsJoinButtonEnabled = false,

                ServerInfo = "Loading...",
                ServerInfoToolTip = @"Server Address: 164.132.200.53
Server Query Port: 2302",

                ServerIp = "164.132.200.53",
                ServerQueryPort = "2302"
            }); 
            
            Servers.Add(new ServersModel()
            {
                ContentButton = "Join Server",
                IsJoinButtonEnabled = false,

                ServerInfo = "Loading...",
                ServerInfoToolTip = @"Server Address: 164.132.202.63
Server Query Port: 2302",

                ServerIp = "164.132.202.63",
                ServerQueryPort = "2302"
            });

            Servers.Add(new ServersModel()
            {
                ContentButton = "Join TeamSpeak",
                IsJoinButtonEnabled = false,
                JoinServerToolTip = $@"Join: {_tsViewModel.TsChannelNameText}
Password: {_tsViewModel.TsChannelPasswordText}",

                ServerInfo = "Loading...",
                ServerInfoToolTip = @"Server Address: TS.grandtheftarma.com:9987
TeamSpeak Selector: #ipsLayout_sidebar > div > ul > li:nth-child(2) > div > div:nth-child(3) > span
TeamSpeak Selector URL: http://grandtheftarma.com/",

                ServerIp = "TS.grandtheftarma.com:9987",
                TsSelector = "#ipsLayout_sidebar > div > ul > li:nth-child(2) > div > div:nth-child(3) > span",
                TsSelectorUrl = "http://grandtheftarma.com/"
            });
        }

        public void ShowServersInfo()
        {
            MessageBox.Show(@"This tab allows you to add ArmA 3/TeamSpeak servers and ""watch"" them:
1.If you want to add ArmA 3 Servers:
-Provide Server IP (e.g. 164.132.200.53) and Server Query Port (e.g. 2302),
-Click ""Add Server"" button.

2.If you want to add TeamSpeak Servers:
-Add TeamSpeak Server IP (e.g. TS.grandtheftarma.com:9987)

If you want to see the current number of TeamSpeak users:
-Click ""Show TeamSpeak settings"" button,
-Provide TeamSpeak Selector.

This is how you can get it:
1.Go to main GTA website,
2.Right click on Number/256 and click Inspect,
3.Right click on highlited HTML Elements, copy, copy selector,
4.Paste in the field.

-Provide the location of the selector, e.g. ""https://grandtheftarma.com/""

3. Click ""Add Ts"" button.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public BindableCollection<ServersModel> Servers { get; set; } = new BindableCollection<ServersModel>();

        public void ServersMouseEnter()
        {
            foreach (var server in Servers)
            {
                if (server.ContentButton == "Join Server")
                    continue;

                server.JoinServerToolTip = $@"Join: {_tsViewModel.TsChannelNameText}
Password: {_tsViewModel.TsChannelPasswordText}";
            }
        }

        public void JoinServer(ServersModel server) => Join.Server(server);

        public void MoveItemUp(ServersModel server)
        {
            int currentIndex = Servers.IndexOf(server);

            if (currentIndex <= 0)
                return;

            Servers.Move(currentIndex, currentIndex - 1);
        }
        public void MoveItemDown(ServersModel server)
        {
            int currentIndex = Servers.IndexOf(server);

            if (currentIndex < 0 || currentIndex + 1 >= Servers.Count)
                return;

            Servers.Move(currentIndex, currentIndex + 1);
        }

        public void DeleteServerItem(ServersModel server) => Servers.Remove(server);

        private string _serverIpText;
        public string ServerIpText
        {
            get { return _serverIpText; }
            set
            {
                _serverIpText = value;

                NotifyOfPropertyChange(() => ServerIpText);
            }
        }

        private string _serverQueryPortText;
        public string ServerQueryPortText
        {
            get { return _serverQueryPortText; }
            set
            {
                _serverQueryPortText = value;

                NotifyOfPropertyChange(() => ServerQueryPortText);
            }
        }

        public void AddServer()
        {
            if (string.IsNullOrWhiteSpace(ServerIpText) || string.IsNullOrWhiteSpace(ServerQueryPortText))
                return;

            if (Servers.All(item => item.ServerIp != ServerIpText))
                Servers.Add(new ServersModel()
                {
                    ContentButton = "Join Server",
                    IsJoinButtonEnabled = false,

                    ServerInfo = "Loading...",
                    ServerInfoToolTip = $@"Server Address: {ServerIpText}
Server Query Port: {ServerQueryPortText}",

                    ServerIp = ServerIpText,
                    ServerQueryPort = ServerQueryPortText
                });
            else
                MessageBox.Show($"The \"{ServerIpText}\" server address is already in the list!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                ThemeManager.Current.ChangeTheme(Application.Current, theme, ThemeManager.Current.DetectTheme().ColorScheme);

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

                var appStyle = ThemeManager.Current.DetectTheme();
                ThemeManager.Current.ChangeTheme(Application.Current, appStyle.BaseColorScheme, SelectedAccent.ColorName);

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
                    Properties.KeyStartUp.SetValue("GTA Mission Downloader", Process.GetCurrentProcess().MainModule.FileName);
                else
                    Properties.KeyStartUp.DeleteValue("GTA Mission Downloader", false);

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

        private bool _isServerChecked;
        public bool IsServerChecked
        {
            get { return _isServerChecked; }
            set
            {
                _isServerChecked = value; 

                NotifyOfPropertyChange(() => IsServerChecked);
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
                    var checkedItems = MissionItems.Where(ps => ps.IsChecked).ToList();
                    if (!checkedItems.Any())
                    {
                        IsAutomaticUpdateChecked = false;
                        return;
                    }

                    _ = Update.UpdateLvItemsCheckAsync(Helper.CtsStopDownloading.Token);
                }
                else
                {
                    Helper.CtsStopDownloading.Cancel();
                    Helper.CtsStopDownloading.Dispose();
                    Helper.CtsStopDownloading = new CancellationTokenSource();
                }

                NotifyOfPropertyChange(() => IsAutomaticUpdateChecked);
            }
        }

        private bool _isAutomaticUpdateEnabled;
        public bool IsAutomaticUpdateEnabled
        {
            get { return _isAutomaticUpdateEnabled; }
            set
            {
                _isAutomaticUpdateEnabled = value; 

                NotifyOfPropertyChange(() => IsAutomaticUpdateEnabled);
            }
        }

        private bool _updateNotify;
        public bool UpdateNotify
        {
            get { return _updateNotify; }
            set
            {
                _updateNotify = value;

                NotifyOfPropertyChange(() => UpdateNotify);
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
