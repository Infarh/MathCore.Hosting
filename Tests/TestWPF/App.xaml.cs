using System;
using System.Windows;

using MathCore.Hosting;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using TestWPF.ViewModels;

namespace TestWPF
{
    public partial class App
    {
        private static IHost __Hosting;

        public static IHost Hosting => __Hosting ??= CreateHostBuilder(Environment.GetCommandLineArgs()).Build();

        public static IServiceProvider Services => Hosting.Services;

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
           .CreateDefaultBuilder(args)
           .AddServices(typeof(App))
        //.ConfigureServices(ConfigureServices)
        //.AddServiceLocator()
        ;

        private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {
            services.AddServicesFromConfiguration(host.Configuration.GetSection("Services"), typeof(App));
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            var host = Hosting;

            var env = Services.GetRequiredService<IHostEnvironment>();
            var main_model = Services.GetService<MainWindowViewModel>();

            base.OnStartup(e);
            await host.StartAsync().ConfigureAwait(false);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using var host = Hosting;
            base.OnExit(e);
            await host.StopAsync().ConfigureAwait(false);
        }
    }
}
