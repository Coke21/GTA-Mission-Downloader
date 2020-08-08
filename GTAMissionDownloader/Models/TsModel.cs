using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GTAMissionDownloader.Models
{
    public class TsModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;

                OnPropertyChanged();
            }
        }

        private string _channelPath;
        public string ChannelPath
        {
            get { return _channelPath; }
            set
            {
                _channelPath = value;

                OnPropertyChanged();
            }
        }

        private string _channelPassword;
        public string ChannelPassword
        {
            get { return _channelPassword; }
            set
            {
                _channelPassword = value;

                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
