using Autofac;
using WpfTest.Services;
using WpfTest.ViewModel;

namespace WpfTest.Startup
{
    public class Bootstrapper
    {

        public IContainer Bootstrap()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<MainWindow>().AsSelf();
            builder.RegisterType<MainViewModel>().UsingConstructor(typeof(ITestViewModel), typeof(IHomeViewModel));
            builder.RegisterType<TestViewModel>().As<ITestViewModel>().UsingConstructor(typeof(ISomeService));
            builder.RegisterType<SomeService>().As<ISomeService>();
            builder.RegisterType<HomeViewModel>().As<IHomeViewModel>();



            return builder.Build();
        }
    }
}
