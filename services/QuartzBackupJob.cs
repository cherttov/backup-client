using Quartz;
using backup_system.models;

namespace backup_system.services
{
    public class QuartzBackupJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                var job = (BackupJob)context.MergedJobDataMap["job"];

                var executor = new BackupExecutor();
                executor.RunBackup(job);

                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[QuartzBackupJob] Exception: {e}");
                throw;
            }
        }
    }
}
