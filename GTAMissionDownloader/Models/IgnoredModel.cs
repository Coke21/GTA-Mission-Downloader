using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GTAMissionDownloader.Models
{
    public class IgnoredModel : INotifyPropertyChanged
    {
        private string _item;
        public string Item
        {
            get { return _item; }
            set
            {
                _item = value;

                OnPropertyChanged();
            }
        }

        private string _fileId;
        public string FileId
        {
            get => _fileId;
            set
            {
                _fileId = value;

                OnPropertyChanged();
            }
        }

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
