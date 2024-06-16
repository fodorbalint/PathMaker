using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OneWayLabyrinth
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            var logger = ServiceProvider.GetRequiredService<ILogger<App>>();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure =>
            {
                configure.ClearProviders(); // Clear default providers
                configure.AddConsole(); // Add console logging
            }).Configure<LoggerFilterOptions>(options =>
            {
                options.MinLevel = LogLevel.Debug; // Set minimum log level
            });
        }
    }
}
