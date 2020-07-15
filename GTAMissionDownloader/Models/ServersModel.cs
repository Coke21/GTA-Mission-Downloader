using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GTAMissionDownloader.Models
{
    public class ServersModel : INotifyPropertyChanged
    {
        public string ContentButton { get; set; }

        private bool _isJoinButtonEnabled;
        public bool IsJoinButtonEnabled
        {
            get { return _isJoinButtonEnabled; }
            set
            {
                _isJoinButtonEnabled = value;

                OnPropertyChanged();
            }
        }

        private string _serverInfo;
        public string ServerInfo
        {
            get { return _serverInfo; }
            set
            {
                _serverInfo = value;

                OnPropertyChanged();
            }
        }

        public string ToolTip { get; set; }

        private string _serverNoteInfo;
        public string ServerNoteInfo
        {
            get { return _serverNoteInfo; }
            set
            {
                _serverNoteInfo = value;

                OnPropertyChanged();
            }
        }

        public string ServerIp { get; set; }
        public string ServerQueryPort { get; set; }
        public string TsSelector { get; set; }
        public string TsSelectorUrl { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
