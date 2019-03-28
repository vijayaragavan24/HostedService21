using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HostedService.Models;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Hosting;
using NCrontab;
using Newtonsoft.Json;

namespace HostedService.Scheduling
{
    public class SchedulerHostedService : HostedService
    {
        private IHostingEnvironment _env;
        public event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException;
            
        private readonly List<SchedulerTaskWrapper> _scheduledTasks = new List<SchedulerTaskWrapper>();

        public SchedulerHostedService(IEnumerable<IScheduledTask> scheduledTasks, IHostingEnvironment env)
        {
            _env = env;

            var path = string.Format("{0}\\Data\\Jobs.json", _env.ContentRootPath);

            var store = new DataStore(path);

            var referenceTime = DateTime.Now;

            var jobs = new List<Job>();

            foreach (var scheduledTask in scheduledTasks)
            {
                _scheduledTasks.Add(new SchedulerTaskWrapper
                {
                    Schedule = CrontabSchedule.Parse(scheduledTask.Schedule),
                    CronString = CrontabSchedule.Parse(scheduledTask.Schedule).ToString(),
                    Task = scheduledTask,
                    TaskName = scheduledTask.TaskName,
                    NextRunTime = referenceTime
                });

                jobs.Add(new Job
                {
                    Id = scheduledTask.TaskId,
                    Name = scheduledTask.TaskName,
                    TypeName = scheduledTask.TaskTypeName,
                    CronString = CrontabSchedule.Parse(scheduledTask.Schedule).ToString(),
                    LastRunTime = null,
                    NextRunTime = CrontabSchedule.Parse(scheduledTask.Schedule).GetNextOccurrence(referenceTime)
                });
            }

            var collection = store.GetCollection<Job>();
            
            foreach (var job in jobs)
            {
                var query = collection.AsQueryable().Where(a => a.Id == job.Id).FirstOrDefault();
                if(query == null)
                {
                    collection.InsertOne(job);
                }
                else
                {
                    collection.UpdateOne(a => a.Id == job.Id, job);
                }
            }

            
              

            //var json = JsonConvert.SerializeObject(jobs, Formatting.Indented);
            //using (TextWriter writer = new StreamWriter(path, append: true))
            //{
            //    writer.WriteLine(json);
            //}

        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ExecuteOnceAsync(cancellationToken);
                
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }

        private async Task ExecuteOnceAsync(CancellationToken cancellationToken)
        {
            var taskFactory = new TaskFactory(TaskScheduler.Current);
            var referenceTime = DateTime.Now;
            
            var tasksThatShouldRun = _scheduledTasks.Where(t => t.ShouldRun(referenceTime)).ToList();

            foreach (var taskThatShouldRun in tasksThatShouldRun)
            {
                taskThatShouldRun.Increment();

                await taskFactory.StartNew(
                    async () =>
                    {
                        try
                        {
                            await taskThatShouldRun.Task.ExecuteAsync(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            var args = new UnobservedTaskExceptionEventArgs(
                                ex as AggregateException ?? new AggregateException(ex));
                            
                            UnobservedTaskException?.Invoke(this, args);
                            
                            if (!args.Observed)
                            {
                                throw;
                            }
                        }
                    },
                    cancellationToken);
            }
        }

        private class SchedulerTaskWrapper
        {
            public CrontabSchedule Schedule { get; set; }
            public IScheduledTask Task { get; set; }
            public string TaskName { get; set; }

            public string CronString { get; set; }

            public DateTime LastRunTime { get; set; }
            public DateTime NextRunTime { get; set; }

            public void Increment()
            {
                LastRunTime = NextRunTime;
                NextRunTime = Schedule.GetNextOccurrence(NextRunTime);
            }

            public bool ShouldRun(DateTime currentTime)
            {
                return NextRunTime < currentTime && LastRunTime != NextRunTime;
            }
        }
    }
}