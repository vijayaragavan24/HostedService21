using HostedService.Models;
using HostedService.Scheduling;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HostedService.Jobs
{
    public class TimeTask : IScheduledTask
    {
        private IHostingEnvironment _env;

        public TimeTask(IHostingEnvironment env)
        {
            _env = env;
        }

        public int TaskId => 1;
        public string TaskName => "Time Job";

        public string Schedule => "*/30 * * * *";

        public string TaskTypeName => nameof(TimeTask);

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var path = string.Format("{0}\\Data\\TimeTask.txt", _env.ContentRootPath);
            var jobsDataPath = string.Format("{0}\\Data\\Jobs.json", _env.ContentRootPath);
            var dataStore = new DataStore(jobsDataPath);
            var dummyData = new
            {
                guid = Guid.NewGuid(),
                time = DateTime.Now
            };

            var json = JsonConvert.SerializeObject(dummyData, Formatting.Indented);
            using (TextWriter writer = new StreamWriter(path, append: true))
            {
                writer.WriteLine(json);
            }

            var collection = dataStore.GetCollection<Job>();
            var job = collection.AsQueryable().FirstOrDefault(a => a.Id == TaskId);
            job.LastRunTime = DateTime.Now;
            collection.UpdateOne(a=>a.Id == job.Id, job);

            return Task.CompletedTask;
        }
    }
}
