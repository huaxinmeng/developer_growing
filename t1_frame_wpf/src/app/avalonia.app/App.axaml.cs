using avalonia.model.ViewModels;
using avalonia.samples.library;
using avalonia.samples.plugin;
using avalonia.samples.plugin.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace avalonia.app
{
    public partial class App : PrismApplication
    {
        public static bool IsSingleViewLifetime =>
            Environment.GetCommandLineArgs()
                .Any(a => a == "--fbdev" || a == "--drm");

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder
                .Configure<App>()
                .UsePlatformDetect();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            base.Initialize();              // <-- Required
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register Services
            //containerRegistry.Register<IRestService, RestService>();

            // Views - Generic
            containerRegistry.Register<MainWindow>();

            // Views - Region Navigation
            //containerRegistry.RegisterForNavigation<DashboardView, DashboardViewModel>();
            //containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>();
            //containerRegistry.RegisterForNavigation<SidebarView, SidebarViewModel>();
        }

        protected override AvaloniaObject CreateShell()
        {
            if (IsSingleViewLifetime)
                return Container.Resolve<MainWindow>();// Container.Resolve<MainControl>(); // For Linux Framebuffer or DRM
            else
            {
                var ety = Container.Resolve<PageHomeWindow>();
                ety.DataContext = new PageHomeWindowViewModel();
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = ety;
                }
                return ety;
            }
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            // Register modules
            moduleCatalog.AddModule<PluginModule>();
            //moduleCatalog.AddModule<Module2.Module>();
            //moduleCatalog.AddModule<Module3.Module>();
        }

        /// <summary>Called after <seealso cref="Initialize"/>.</summary>
        protected override void OnInitialized()
        {
            // Register initial Views to Region.
            var regionManager = Container.Resolve<IRegionManager>();
            //regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(DashboardView));
            //regionManager.RegisterViewWithRegion(RegionNames.SidebarRegion, typeof(SidebarView));
            regionManager.RegisterViewWithRegion("ContentRegion", typeof(MainSample));
        }

        protected override void ConfigureViewModelLocator()
        {
            base.ConfigureViewModelLocator();

            var assembly = Assembly.Load("avalonia.model");
            ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(viewType =>
            {
                var viewName = viewType.Name;
                //viewName = viewName.Replace(".Views.", ".ViewModels.");
                //var viewAssemblyName = viewType.GetTypeInfo().Assembly.FullName;
                var suffix = viewName.EndsWith("View") ? "Model" : "ViewModel";
                var viewModelName = String.Format(CultureInfo.InvariantCulture, "{0}{1}", viewName, suffix);
                //var assembly = viewType.GetTypeInfo().Assembly;
                var type = assembly.GetType($"avalonia.model.ViewModels.{viewModelName}", true);
                return type;
            });
        }

    }
}