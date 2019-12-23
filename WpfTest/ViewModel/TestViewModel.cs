using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors.Core;
using WpfTest.Services;

namespace WpfTest.ViewModel
{
    public class TestViewModel : ViewModelBase, ITestViewModel
    {
        private readonly int _id;
        private string _textToShow;

        public TestViewModel()
        {
            //MessageBox.Show("yxq");
        }

        public TestViewModel(ISomeService someService)
        {
            _id = someService.GetRandomInt();
            ClickCommand = new ActionCommand(OnClickCommandExecute);
           // MessageBox.Show("wow" +_id);

            TextToShow = _id.ToString();

            TextToShow = "sadsads";
        }

        private void OnClickCommandExecute()
        {

            MessageBox.Show("click " + _id);
        }

        public string TextToShow
        {
            get => _textToShow;
            set
            {
                _textToShow = value; 
                OnPropertyChanged();
            }
        }

        public ICommand ClickCommand { get; set; }
    }
}
