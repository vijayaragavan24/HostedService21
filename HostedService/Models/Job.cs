using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HostedService.Models
{
    public class Job
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string CronString { get; set; }
        public DateTime? LastRunTime { get; set; }
        public DateTime NextRunTime { get; set; }
    }
}
