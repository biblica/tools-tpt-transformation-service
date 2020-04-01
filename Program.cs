using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;

namespace TptMain
{
    /// <summary>
    /// Main driver class for the service.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Creates, configures, and builds the site host.
        /// </summary>
        /// <param name="args">Program arguments array.</param>
        /// <returns>The HostBuilder</returns>
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile(Path.Combine("Properties", "serviceSettings.json"));
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddEventLog();

                logging.AddFilter("Microsoft", LogLevel.Warning);
                logging.AddFilter("System", LogLevel.Warning);
                logging.AddFilter("LoggingConsoleApp.Program", LogLevel.Debug);
            })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}