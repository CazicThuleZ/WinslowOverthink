using Quartz;
using Quartz.Spi;
using Microsoft.Extensions.DependencyInjection;

public static class QuartzExtensions
{
    public static IServiceCollectionQuartzConfigurator AddJobAndTrigger<T>(this IServiceCollectionQuartzConfigurator quartz, string cronSchedule) where T : IJob
    {
        var jobKey = new JobKey(typeof(T).Name);
        quartz.AddJob<T>(opts => opts.WithIdentity(jobKey));
        quartz.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity($"{jobKey.Name}-trigger")
            .WithCronSchedule(cronSchedule));
        return quartz;
    }
}
