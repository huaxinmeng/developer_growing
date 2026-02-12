using avalonia.samples.plugin.Views;
using Avalonia.Controls;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using System.Reflection;

namespace avalonia.samples.plugin
{
    public class PluginModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            //var regionManager = containerProvider.Resolve<IRegionManager>();
            //regionManager.RegisterViewWithRegion("ContentRegion", typeof(TabSample));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //throw new NotImplementedException();

            containerRegistry.RegisterForNavigation<TabSample>();
        }
    }
}