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
                try
                {
                    // Converted CRON timing
                    string quartzCron = ConvertUnixToQuartzCron(job.Timing);

                    // Job details
                    var detail = JobBuilder.Create<QuartzBackupJob>()
                        .UsingJobData(new JobDataMap
                        {
                        { "job", job }
                        })
                        .Build();

                    // Job trigger (CRON/timing)
                    var trigger = TriggerBuilder.Create()
                        .WithCronSchedule(quartzCron)
                        .Build();

                    // Scheduling 
                    await scheduler.ScheduleJob(detail, trigger);

                    Console.WriteLine($"[Program] Backup scheduled on {trigger.GetNextFireTimeUtc()?.ToLocalTime()}");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"[Program] Failed to schedule job: {e.Message}");
                }
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
                Console.Error.WriteLine($"[Program] Invalid CRON length (check config.json).");

            // Checking that there is only dayInMonth OR dayInWeek, not both
            if (parts[2] != "*")
            {
                if (parts[4] == "*")
                    return $"0 {parts[0]} {parts[1]} {parts[2]} {parts[3]} ?";
                else
                    // return invalid CRON and let scheduler handle it
                    return $"0 {parts[0]} {parts[1]} {parts[2]} {parts[3]} {parts[4]}";
            }
            else
            {
                if (parts[4] != "*")
                    return $"0 {parts[0]} {parts[1]} ? {parts[3]} {parts[4]}";
                else
                    return $"0 {parts[0]} {parts[1]} * {parts[3]} ?";
            }
        }
    }
}
