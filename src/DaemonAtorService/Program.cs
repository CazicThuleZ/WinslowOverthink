using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Configuration;
using DaemonAtorService.Models;
using DaemonAtorService.Helper;

namespace DaemonAtorService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build())
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

                    services.Configure<JournalSettings>(context.Configuration.GetSection("JournalSettings"));
                    services.Configure<CryptoSettings>(context.Configuration.GetSection("CryptoSettings"));
                    services.Configure<GmailApiSettings>(context.Configuration.GetSection("GmailApiSettings"));
                    services.Configure<ArchiveFileSettings>(context.Configuration.GetSection("ArchiveFileSettings"));
                    services.Configure<GlobalSettings>(context.Configuration.GetSection("GlobalSettings"));
                    services.Configure<List<DirectorySyncSettings>>(context.Configuration.GetSection("DirectorySyncSettings"));
                    services.Configure<PostgresBackupSettings>(context.Configuration.GetSection("PostgresBackupSettings"));

                    services.AddHttpClient();
                    services.AddSingleton<LogHandlerFactory>();

                    services.AddSingleton<ILoggingStrategy>(sp =>
                      new JsonFileLoggingStrategy(context.Configuration.GetSection("GlobalSettings:DashboardLogLocation").Value));

                    services.AddTransient<BankAccountBalanceHandler>();
                    services.AddTransient<LoseItDailySummaryHandler>();
                    services.AddTransient<ActivityCountHandler>();
                    services.AddTransient<ActivityDurationHandler>();
                    services.AddTransient<DietScaleHandler>();
                    services.AddTransient<CryptoPriceHandler>();
                    services.AddTransient<TokenUsageHandler>();
                    services.AddTransient<OtherLogHandler>();

                    services.AddTransient<PokeTheOracle>();
                    services.AddTransient<PostgresBackupService>();

                    var kernel = SemanticKernelConfig.InitializeKernel(context.Configuration);
                    services.AddSingleton(kernel);

                    services.AddHostedService<Worker>();

                    services.AddQuartz(q =>
                    {
                        q.AddJobAndTrigger<CryptoPriceJob>(jobSchedules.CryptoPriceJob);
                        q.AddJobAndTrigger<EmailReadJob>(jobSchedules.EmailReadJob);
                        q.AddJobAndTrigger<JournalIntakeJob>(jobSchedules.JournalIntakeJob);
                        q.AddJobAndTrigger<EndOfDayJob>(jobSchedules.EndOfDayJob);
                        q.AddJobAndTrigger<NotificationJob>(jobSchedules.NotificationJob);
                        q.AddJobAndTrigger<DatabaseSnapshotsJob>(jobSchedules.DatabaseSnapshotsJob);
                    });

                    services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
                });
    }
}