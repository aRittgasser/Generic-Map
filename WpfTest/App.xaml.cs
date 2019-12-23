using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using WpfTest.Startup;
using WpfTest.ViewModel;

namespace WpfTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            var butstrapper = new Bootstrapper();
            var container = butstrapper.Bootstrap();

            var mainWindow = container.Resolve<MainWindow>();

            mainWindow.DataContext = container.Resolve<MainViewModel>();

            mainWindow.Show();


        }
    }
}
