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
            string configPath = "../../../data/config.json";

            List<BackupJob> jobs = ReadConfig(configPath);

            var scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            await scheduler.Start();

            foreach (var job in jobs)
            {
                var detail = JobBuilder.Create<QuartzBackupJob>()
                    .UsingJobData(new JobDataMap
                    {
                        { "job", job }
                    })
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .StartNow()
                    .Build();

                await scheduler.ScheduleJob(detail, trigger);
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
    }
}
