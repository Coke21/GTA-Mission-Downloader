using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GTAMissionDownloader.Models
{
    public class ListViewModel : INotifyPropertyChanged
    {
        private string _mission;
        public string Mission
        {
            get => _mission;
            set
            {
                _mission = value;

                OnPropertyChanged();
            }
        }

        private string _isMissionUpdated;
        public string IsMissionUpdated
        {
            get => _isMissionUpdated;
            set
            {
                _isMissionUpdated = value;

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

        private string _modifiedTime;
        public string ModifiedTime
        {
            get => _modifiedTime;
            set
            {
                _modifiedTime = value;

                OnPropertyChanged();
            }
        }

        private string _isModifiedTimeUpdated;
        public string IsModifiedTimeUpdated
        {
            get => _isModifiedTimeUpdated;
            set
            {
                _isModifiedTimeUpdated = value;

                OnPropertyChanged();
            }
        }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;

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
