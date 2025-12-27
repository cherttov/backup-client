using System.Text.Json;
using System.Text.Json.Serialization;
using backup_system.models;
using backup_system.services;
using Quartz;
using Quartz.Impl;

namespace backup_system
{
    public class Program
    {
        static async Task Main()
        {
            // Read config to jobs
            string configPath = "../../../data/config.json";
            List<BackupJob> jobs = ReadConfig(configPath);

            // Create scheduler
            var scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            await scheduler.Start();

            // Process and schedule each job
            foreach (var job in jobs)
            {
                var detail = JobBuilder.Create<QuartzBackupJob>()
                    .UsingJobData(new JobDataMap
                    {
                        { "job", job }
                    })
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .WithCronSchedule(ConvertUnixToQuartzCron(job.Timing))
                    .Build();

                await scheduler.ScheduleJob(detail, trigger);

                Console.WriteLine($"[Program] Backup scheduled on {trigger.GetNextFireTimeUtc()?.ToLocalTime()}");
            }

            await Task.Delay(Timeout.Infinite);
        }

        // Config reader (json)
        private static List<BackupJob> ReadConfig(string path)
        {
            List<BackupJob> jobEntries = new();

            if (File.Exists(path))
            {
                string data = File.ReadAllText(path);
                if (!string.IsNullOrEmpty(data))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                    };

                    jobEntries = JsonSerializer.Deserialize<List<BackupJob>>(data, options)!;
                }
            }

            return jobEntries;
        }

        private static string ConvertUnixToQuartzCron(string unixCron)
        {
            var parts = unixCron.Split(' ');
            if (parts.Length != 5)
            {
                Console.Error.WriteLine($"[Program] Invalid CRON length");
            }

            return $"0 {parts[0]} {parts[1]} {parts[2]} {parts[3]} ?";
        }
    }
}
