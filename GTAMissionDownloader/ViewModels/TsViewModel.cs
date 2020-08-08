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
        public string WindowName => "TeamSpeakWindow";

        public static TsViewModel TsVm;
        private MainViewModel _mvm;
        public TsViewModel(MainViewModel mvm)
        {
            TsVm = this;
            _mvm = mvm;

            Persistence.Tracker.Configure<TsViewModel>()
                .Id(p => p.WindowName, includeType: false)
                .Properties(p => new { p.Top, p.Left })
                .Property(p => p.Height, 430, "Window Height")
                .Property(p => p.Width, 310, "Window Width")

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

        public BindableCollection<TsModel> TsItems { get; set; } = new BindableCollection<TsModel>();

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
            if (keyArgs.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                if (TsItems.All(item => item.ChannelPath != TsChannelNameText))
                    TsItems.Add(new TsModel()
                    {
                        ChannelPath = TsChannelNameText,
                        ChannelPassword = TsChannelPasswordText
                    });
                else
                    MessageBox.Show($"The \"{TsChannelNameText}\" channel is already in the list!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            if (keyArgs.Key == Key.Delete)
                DeletePath();
        }

        //Unselect items
        public void LvMouseDown(TsView view, Point e)
        {
            HitTestResult r = VisualTreeHelper.HitTest(view, e);
            if (r.VisualHit.GetType() != typeof(ListViewItem))
                foreach (var item in TsItems)
                    item.IsSelected = false;
        }

        public void LvMouseEnter(ListView lv) => lv.Focus();

        //Resize columns
        public void LvLoaded(ListView listview) => LvSizeChanged(listview);
        public void LvSizeChanged(ListView listView)
        {
            GridView gridView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - 5;
            var col1 = 0.50;
            var col2 = 0.50;

            if (workingWidth < 0)
                return;

            gridView.Columns[0].Width = workingWidth * col1;
            gridView.Columns[1].Width = workingWidth * col2;
        }

        //On drop on listview
        public void ChannelDropLv(DragEventArgs dragArgs)
        {
            if (!dragArgs.Data.GetDataPresent(DataFormats.Text)) 
                return;

            if (TsItems.All(item => item.ChannelPath != TsChannelNameText))
                TsItems.Add(new TsModel()
                {
                    ChannelPath = TsChannelNameText,
                    ChannelPassword = TsChannelPasswordText
                });
            else
                MessageBox.Show($"The \"{TsChannelNameText}\" channel is already in the list!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        //DragDrop
        public void LvMouseMoveDragDrop(TsView view)
        {
            if (Mouse.LeftButton != MouseButtonState.Pressed) 
                return;

            if (SelectedItem == null)
                return;

            if (Mouse.DirectlyOver.GetType() == typeof(TextBox))
                return;

            DragDrop.DoDragDrop(view, new DataObject(SelectedItem), DragDropEffects.Copy);
        }

        public void AddFile()
        {
            if (string.IsNullOrWhiteSpace(TsChannelNameText))
                return;

            if (TsItems.All(item => item.ChannelPath != TsChannelNameText))
                TsItems.Add(new TsModel()
                {
                    ChannelPath = TsChannelNameText,
                    ChannelPassword = TsChannelPasswordText
                });
            else
                MessageBox.Show($"The \"{TsChannelNameText}\" channel is already in the list!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void DeletePath()
        {
            if (SelectedItem == null)
                return;

            TsItems.Remove(SelectedItem);
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
        public void DragOverChannelName(DragEventArgs e) => e.Handled = true;
        public void DropChannelName(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(TsModel)))
                return;

            var channel = e.Data.GetData(typeof(TsModel)) as TsModel;

            TsChannelNameText = channel.ChannelPath;
            TsChannelPasswordText = channel.ChannelPassword;
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
        public void DragOverChannelPassword(DragEventArgs e) => e.Handled = true;
        public void DropChannelPassword(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(TsModel)))
                return;

            var channel = e.Data.GetData(typeof(TsModel)) as TsModel;

            TsChannelNameText = channel.ChannelPath;
            TsChannelPasswordText = channel.ChannelPassword;
        }

        public void OnClose() => _mvm.IsExpanderOpened = false;
        public async Task CloseWindow() => await TryCloseAsync();
    }
}
