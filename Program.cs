using System.Text.Json;
using System.Text.Json.Serialization;
using backup_client.models;
using backup_client.services;
using Quartz;
using Quartz.Impl;

namespace backup_client
{
    public class Program
    {
        static async Task Main()
        {
            // Read config to jobs
            string baseDir = AppContext.BaseDirectory;
            string configPath = Path.Combine(baseDir, "data", "config.json");
            string examplePath = Path.Combine(baseDir, "data", "config.example.json");

            // Check if untouched release version or not
            if (!File.Exists(configPath))
            {
                if (File.Exists(examplePath))
                {
                    File.Copy(examplePath, configPath);
                    Console.WriteLine($"[Program][SETUP] Created default config.json at: {configPath}");
                    Console.WriteLine("[Program][SETUP] Edit the default config.json before restarting the application");
                }
                else
                {
                    Console.WriteLine("[Program][SETUP][ERROR] Configuration file is missing.");
                }
                return;
            }

            List<BackupJob> jobs = ReadConfig(configPath);

            // Create scheduler
            IScheduler scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            await scheduler.Start();

            // Process and schedule each job
            foreach (BackupJob job in jobs)
            {
                try
                {
                    // Converted CRON timing
                    string quartzCron = ConvertUnixToQuartzCron(job.Timing);

                    // Job details
                    IJobDetail detail = JobBuilder.Create<QuartzBackupJob>()
                        .UsingJobData(new JobDataMap
                        {
                            { "job", job }
                        })
                        .Build();

                    // Job trigger (CRON/timing)
                    ITrigger trigger = TriggerBuilder.Create()
                        .WithCronSchedule(quartzCron)
                        .Build();

                    // Scheduling 
                    await scheduler.ScheduleJob(detail, trigger);

                    // Initial schedule message
                    Console.WriteLine($"[Program] Backup scheduled on {trigger.GetNextFireTimeUtc()?.ToLocalTime()}");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"[Program][ERROR] Failed to schedule job: {e.Message}");
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
                string data;

                try
                {
                    data = File.ReadAllText(path);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"[Program][ERROR] Failed to read config: {e.Message}");
                    return jobEntries;
                }
                
                if (!string.IsNullOrEmpty(data))
                {
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                    };

                    jobEntries = JsonSerializer.Deserialize<List<BackupJob>>(data, options)!;
                }
                else
                {
                    return jobEntries;
                }
            }
            else
            {
                Console.Error.WriteLine($"[Program][ERROR] Config file not found.");
            }

            return jobEntries;
        }

        // Convert UNIX CRON to Quartz compatible CRON (later change UNIX CRON format in config.json)
        private static string ConvertUnixToQuartzCron(string unixCron)
        {
            const string SAFE_CRON = "0 0 0 1 1 ? 2099"; // so it doesn't return null

            // Check if empty
            if (string.IsNullOrWhiteSpace(unixCron))
            {
                Console.Error.WriteLine("[Program][ERROR] Found empty CRON expression in config.json.");
                return SAFE_CRON;
            }

            // Trim & add 'seconds' to 5 length standard
            List<string> parts = unixCron.Trim().Split(' ').ToList();
            if (parts.Count == 5)
                parts.Insert(0, "0");

            // Check others lengths
            if (parts.Count < 6 || parts.Count > 7)
            {
                Console.Error.WriteLine("[Program][ERROR] Invalid CRON length (expected 5, 6 or 7).");
                return SAFE_CRON;
            }

            // Fix "* *" -> "* ?"
            if (parts[3] == "*" && parts[5] == "*")
                parts[5] = "?";
            // Fix "? ?" -> "* ?" (assume daily)
            else if (parts[3] == "?" && parts[5] == "?")
                parts[3] = "*";
            // Fix "* x" -> "? x"
            else if (parts[3] == "*" && parts[5] != "*" && parts[5] != "?")
                parts[3] = "?";
            // Fix "x *" -> "x ?"
            else if (parts[3] != "*" && parts[3] != "?" && parts[5] == "*")
                parts[5] = "?";
            // Allow valid "* ?" or "? *"
            else if ((parts[3] == "*" && parts[5] == "?") || (parts[3] == "?" && parts[5] == "*")) 
                { }
            // Allow valid "? x" or "x ?"
            else if ((parts[3] == "?" && parts[5] != "?" && parts[5] != "*") || (parts[3] != "*" && parts[3] != "?" && parts[5] == "?")) 
                { }
            else
            {
                Console.Error.WriteLine("[Program][ERROR] Invalid CRON format.");
                return SAFE_CRON;
            }

            return string.Join(" ", parts);
        }
    }
}
