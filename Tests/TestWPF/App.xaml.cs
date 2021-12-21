using System;
using System.Windows;

using MathCore.DI;
using MathCore.Hosting;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using TestWPF.ViewModels;

namespace TestWPF
{
    interface IPrinter
    {
        public void Print(string msg);
    }

    class ConsolePrinter : IPrinter
    {
        public void Print(string msg) => Console.WriteLine(msg);
    }

    interface ICalculator
    {
        int Add(int a, int b);
    }

    class DefaultCalculator : ICalculator
    {
        public int Add(int a, int b) => a + b;
    }

    interface IUserService
    {
        IPrinter Printer { get; set; }
        ICalculator Calculator { get; }
    }

    public partial class App
    {
        private static IHost __Hosting;

        public static IHost Hosting => __Hosting ??= CreateHostBuilder(Environment.GetCommandLineArgs()).Build();

        public static IServiceProvider Services => Hosting.Services;

        public App()
        {
            var services = new ServiceCollection();

            services.AddServicesFromAssembly(typeof(App));
        }

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
           .CreateDefaultBuilder(args)
           .AddServices(typeof(App))
           .ConfigureServices(ConfigureServices)
           .AddServiceLocator()
        ;

        private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {
            services.AddScoped<IPrinter, ConsolePrinter>();
            services.AddScoped<ICalculator, DefaultCalculator>();
            //services.AddServicesFromConfiguration(host.Configuration.GetSection("Services"), typeof(App));
            //services.AddComposite<IUserService>();
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
