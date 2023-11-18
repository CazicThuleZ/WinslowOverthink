using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using DaemonDo.Journal.AutoClassify;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main(string[] args)
    {        
        try
        {
            var host = new HostBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    logging.AddNLog();
                })
                .ConfigureHostConfiguration(hostConfig =>
                {
                    hostConfig.AddCommandLine(args)
                    .AddEnvironmentVariables();
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {

                    var env = Environment.GetEnvironmentVariable("DAEMONDO_ENVIRONMENT");
                    Console.WriteLine($"Environment Name: {env}");
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient("WinslowOverthink", c =>
                    {
                        c.DefaultRequestHeaders.Add("User-Agent", "Winslow-HttpClientFactory");
                    })
                    .ConfigurePrimaryHttpMessageHandler(() =>
                    {
                        return new HttpClientHandler()
                        {
                            UseDefaultCredentials = true
                        };
                    });

                    services.AddHostedService<AutoClassify>();

                })
                .Build();

                host.Start();

            //await host.RunAsync();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application exit (avoid segmentation fault on Linux)
            NLog.LogManager.Shutdown();
        }
    }
}