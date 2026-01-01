using Quartz;
using backup_client.models;

namespace backup_client.services
{
    public class QuartzBackupJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                BackupJob job = (BackupJob)context.MergedJobDataMap["job"];

                BackupExecutor executor = new BackupExecutor();
                executor.RunBackup(job);

                // Next schedule message
                DateTimeOffset? nextSchedule = context.Trigger.GetNextFireTimeUtc();

                if (nextSchedule.HasValue)
                    Console.WriteLine($"[Program] Backup scheduled on {nextSchedule.Value.ToLocalTime()}");

                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"|__ [QuartzBackupJob][ERROR] Exception: {e}");
                throw;
            }
        }
    }
}
