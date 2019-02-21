using System.Configuration;
using WhyShare.Views;
using Prism.Ioc;
using System.Windows;
using WhyShare.Infrastructure.Interfaces;
using WhyShare.Infrastructure.Provider.ShortService.Bitly;

namespace WhyShare
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance<IShortProvider>(new BitlyProvider(ConfigurationManager.AppSettings["BitlyUser"], ConfigurationManager.AppSettings["BitlyKey"]));
        }
    }
}
