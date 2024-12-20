using System.Reactive;
using AvaSend.Models;
using ReactiveUI;

namespace AvaSend.ViewModels
{
    public class ReceiveViewModel : ViewModelBase
    {
        private readonly DataService _dataService;

        public ReceiveViewModel()
        {
            _dataService = DataService.Instance;
            SaveCommand = ReactiveCommand.Create(SaveSettings);
        }

        public string UserName
        {
            get => _dataService.UserName;
            set
            {
                _dataService.UserName = value;
                SaveSettings();
            }
        }

        public bool IsAnimationEnabled
        {
            get => _dataService.IsAnimationEnabled;
            set
            {
                _dataService.IsAnimationEnabled = value;
                this.RaisePropertyChanged(nameof(IsAnimationEnabled));
                SaveSettings();
            }
        }

        public bool IsAutoSaved
        {
            get => _dataService.IsAutoSaved;
            set
            {
                _dataService.IsAutoSaved = value;
                this.RaisePropertyChanged(nameof(IsAutoSaved));
                SaveSettings();
            }
        }

        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        private void SaveSettings()
        {
            _dataService.SaveSettings();
        }
    }
}
