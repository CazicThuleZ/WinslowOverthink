using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Configuration;

namespace DaemonAtorService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                 .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("Starting up the service");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "There was a problem starting the service");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureAppConfiguration((context, config) =>
                {
                    var env = context.HostingEnvironment;

                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true)
                          .AddEnvironmentVariables();
                })
                .UseWindowsService()
                .ConfigureServices((context, services) =>
                {
                    // Get schedule settings
                    var jobSchedules = context.Configuration.GetSection("ScheduleSettings").Get<ScheduleSettings>();

                    if (jobSchedules == null)
                        throw new Exception("Failed to load ScheduleSettings from configuration.");

                    services.Configure<CryptoSettings>(context.Configuration.GetSection("CryptoSettings"));
                    services.Configure<GmailApiSettings>(context.Configuration.GetSection("GmailApiSettings"));
                    services.Configure<CsvFileSettings>(context.Configuration.GetSection("CsvFileSettings"));
                    services.Configure<GlobalSettings>(context.Configuration.GetSection("GlobalSettings"));

                    services.AddSingleton<GmailServiceHelper>();

                    var kernel = SemanticKernelConfig.InitializeKernel(context.Configuration);
                    services.AddSingleton(kernel);

                    services.AddHostedService<Worker>();

                    services.AddQuartz(q =>
                    {
                        q.AddJobAndTrigger<CryptoPriceJob>(jobSchedules.CryptoPriceJob);
                        q.AddJobAndTrigger<EmailReadJob>(jobSchedules.EmailReadJob);
                        q.AddJobAndTrigger<CalorieIntakeJob>(jobSchedules.CalorieIntakeJob);
                    });

                    services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
                });
    }
}