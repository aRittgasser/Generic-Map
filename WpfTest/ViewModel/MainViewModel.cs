using System.Windows;
using WpfTest.Services;

namespace WpfTest.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private ITestViewModel _testViewModel;
        private IHomeViewModel _homeViewModel;

        public MainViewModel()
        {
            //MessageBox.Show("yxq");
        }
        public MainViewModel(ITestViewModel testViewModel, IHomeViewModel homeViewModel)
        {
            TestViewModel = testViewModel;
            _homeViewModel = homeViewModel;
            // MessageBox.Show("wow");
        }

        public ITestViewModel TestViewModel
        {
            get => _testViewModel;
            set
            {
                _testViewModel = value;
                OnPropertyChanged();
            }
        }

        public IHomeViewModel HomeViewModel
        {
            get => _homeViewModel;
            set
            {
                _homeViewModel = value;
                OnPropertyChanged();
            }
        }
    }
}
