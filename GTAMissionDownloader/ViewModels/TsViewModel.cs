using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using GTADownloader;
using GTAMissionDownloader.Models;
using GTAMissionDownloader.Views;

namespace GTAMissionDownloader.ViewModels
{
    public class TsViewModel : Screen
    {
        public static TsViewModel TsVm;
        public string WindowName => "TeamSpeakWindow";

        private MainViewModel _mvm;
        public TsViewModel(MainViewModel mvm)
        {
            TsVm = this;
            _mvm = mvm;

            Persistence.Tracker.Configure<TsViewModel>()
                .Id(p => p.WindowName, includeType: false)
                .Properties(p => new { p.Top, p.Left })
                .Property(p => p.Height, 350, "Window Height")
                .Property(p => p.Width, 310, "Window Width")
                .Property(p => p.ColumnWidth, 290, "Column Width")

                .Property(p => p.TsItems, "Saved TS Channel paths")

                .Property(p => p.TsChannelNameText, string.Empty, "Default TS channel")
                .Property(p => p.TsChannelPasswordText, string.Empty, "Default TS password")

                .PersistOn(nameof(PropertyChanged));

            Persistence.Tracker.Track(this);
        }

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

        private double _top;
        public double Top
        {
            get { return _top; }
            set
            {
                _top = value;

                NotifyOfPropertyChange(() => Top);
            }
        }

        private double _left;
        public double Left
        {
            get { return _left; }
            set
            {
                _left = value;

                NotifyOfPropertyChange(() => Left);
            }
        }

        public void WindowSizeChanged() => ColumnWidth = Width - 20;

        private BindableCollection<TsModel> _tsItems = new BindableCollection<TsModel>();
        public BindableCollection<TsModel> TsItems
        {
            get => _tsItems;
            set
            {
                _tsItems = value;

                NotifyOfPropertyChange(() => TsItems);
            }
        }

        private TsModel _selectedItem;
        public TsModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;

                NotifyOfPropertyChange(() => SelectedItem);
            }
        }

        //HotKey
        public void LvItemHotKeys(KeyEventArgs keyArgs)
        {
            if (keyArgs.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                Clipboard.SetDataObject(SelectedItem.ChannelPath);

            if (keyArgs.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                AddFile();

            if (keyArgs.Key == Key.Delete)
                DeletePath();
        }

        public void LvMouseEnter(ListView lv) => lv.Focus();

        //DragDrop
        public void LvMouseMoveDragDrop(TsView view)
        {
            if (SelectedItem == null)
                return;

            if (Mouse.LeftButton != MouseButtonState.Pressed) 
                return;

            DragDrop.DoDragDrop(view, SelectedItem.ChannelPath, DragDropEffects.Copy);
        }

        //On drop on listview
        public void PathDropLv(DragEventArgs dragArgs)
        {
            if (!dragArgs.Data.GetDataPresent(DataFormats.Text)) 
                return;

            Clipboard.SetDataObject((string)dragArgs.Data.GetData(DataFormats.Text) ?? throw new InvalidOperationException());
            AddFile();
        }
        //Unselect items
        public void LvMouseDown(TsView view, Point e)
        {
            HitTestResult r = VisualTreeHelper.HitTest(view, e);
            if (r.VisualHit.GetType() != typeof(ListViewItem))
                foreach (var item in TsItems)
                    item.IsSelected = false;
        }
        //Add, copy, remove
        public void AddFile()
        {
            if (Clipboard.GetText() == "") 
                return;

            if (TsItems.All(item => item.ChannelPath != Clipboard.GetText()))
                TsItems.Add(new TsModel()
                {
                    ChannelPath = Clipboard.GetText()
                });
            else
                MessageBox.Show($"The \"{Clipboard.GetText()}\" parameter is already in the list!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void CopyPath()
        {
            foreach (var item in TsItems)
                if (item.IsSelected)
                    Clipboard.SetDataObject(item.ChannelPath);
        }

        public void DeletePath()
        {
            foreach (var item in TsItems.ToList())
                if (item.IsSelected)
                    TsItems.Remove(item);
        }

        //Channel Name drop
        private string _tsChannelNameText;
        public string TsChannelNameText
        {
            get { return _tsChannelNameText; }
            set
            {
                _tsChannelNameText = value; 
                
                NotifyOfPropertyChange(() => TsChannelNameText);
            }
        }
        public void DropChannelName(DragEventArgs e)
        {
            TsChannelNameText = string.Empty;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                TsChannelNameText = (string)e.Data.GetData(DataFormats.FileDrop);
        }

        //Channel Password drop
        private string _tsChannelPasswordText;
        public string TsChannelPasswordText
        {
            get { return _tsChannelPasswordText; }
            set
            {
                _tsChannelPasswordText = value; 

                NotifyOfPropertyChange(() => TsChannelPasswordText);
            }
        }
        public void DropChannelPassword(DragEventArgs e)
        {
            TsChannelPasswordText = string.Empty;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                TsChannelPasswordText = (string)e.Data.GetData(DataFormats.FileDrop);
        }

        //Grid Column Width
        private double _columnWidth;
        public double ColumnWidth
        {
            get { return _columnWidth; }
            set
            {
                _columnWidth = value;

                NotifyOfPropertyChange(() => ColumnWidth);
            }
        }

        public async Task CloseWindow() => await TryCloseAsync();
    }
}
