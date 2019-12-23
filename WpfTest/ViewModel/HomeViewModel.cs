using System.Windows.Input;
using Microsoft.Xaml.Behaviors.Core;

namespace WpfTest.ViewModel
{
    public class HomeViewModel : ViewModelBase, IHomeViewModel
    {
        private bool _isConnected;
        private string _selectedCountry ="RU";

        public HomeViewModel()
        {
            ConnectCommand = new ActionCommand(() =>
            {
                SelectedCountry = IsConnected ? "US" : "";

            });
        }

        public ICommand ConnectCommand { get; set; }

        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                _isConnected = value; 
                OnPropertyChanged();
            }
        }

        public string SelectedCountry
        {
            get => _selectedCountry;
            set
            {
                _selectedCountry = value;
                OnPropertyChanged();
            }
        }
    }
}
