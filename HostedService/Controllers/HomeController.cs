using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HostedService.Models;
using Microsoft.AspNetCore.Hosting;
using JsonFlatFileDataStore;
using HostedService.Scheduling;

namespace HostedService.Controllers
{
    public class HomeController : Controller
    {
        private DataStore jsonDataStore;

        private IHostingEnvironment _env;

        private IEnumerable<IScheduledTask> _scheduledTasks;

        public HomeController(IHostingEnvironment env, IEnumerable<IScheduledTask> scheduledTasks)
        {
            _env = env;
            _scheduledTasks = scheduledTasks;
            var path = string.Format("{0}\\Data\\Jobs.json", _env.ContentRootPath);
            jsonDataStore = new DataStore(path);
        }

        public IActionResult Index()
        {
            var jobs = GetJobs();

            var model = new List<JobViewModel>();

            foreach (var item in jobs)
            {
                var job = new JobViewModel();
                job.Id = item.Id;
                job.JobName = item.Name;
                job.CronString = item.CronString;
                job.NextRunTime = item.NextRunTime;
                job.LastRunTime = item.LastRunTime;
                job.TypeName = item.TypeName;
                model.Add(job);
            }

            return View(model);
        }

        public IActionResult Run(int jobId)
        {
            var job = GetJobs().FirstOrDefault(a => a.Id == jobId);

            if(job == null)
            {
                return Error();
            }

            var task = _scheduledTasks.Where(a => a.TaskId == job.Id).FirstOrDefault();

            if (task == null)
            {
                return Error();
            }
            task.ExecuteAsync(new System.Threading.CancellationToken());

            return RedirectToAction("Index");
        }

        private List<Job> GetJobs()
        {
            return jsonDataStore.GetCollection<Job>().AsQueryable().ToList();
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
