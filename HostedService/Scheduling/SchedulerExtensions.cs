using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HostedService.Scheduling
{
    public static class SchedulerExtensions
    {
        public static IServiceCollection AddScheduler(this IServiceCollection services)
        {
            return services.AddSingleton<IHostedService, SchedulerHostedService>();
        }

        public static IServiceCollection AddScheduler(this IServiceCollection services, EventHandler<UnobservedTaskExceptionEventArgs> unobservedTaskExceptionHandler)
        {
            return services.AddSingleton<IHostedService, SchedulerHostedService>(serviceProvider =>
            {
                var instance = new SchedulerHostedService(serviceProvider.GetServices<IScheduledTask>(), serviceProvider.GetService<Microsoft.AspNetCore.Hosting.IHostingEnvironment>());
                instance.UnobservedTaskException += unobservedTaskExceptionHandler;
                return instance;
            });
        }

        
    }
}