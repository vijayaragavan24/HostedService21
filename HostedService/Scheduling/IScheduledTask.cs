using System.Threading;
using System.Threading.Tasks;

namespace HostedService.Scheduling
{
    public interface IScheduledTask
    {
        int TaskId { get; }
        string TaskName { get; }
        string TaskTypeName { get; }
        string Schedule { get; }
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}