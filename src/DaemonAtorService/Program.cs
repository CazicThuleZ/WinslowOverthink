using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace DaemonAtorService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
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