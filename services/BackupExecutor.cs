using backup_system.models;

namespace backup_system.services
{
    public class BackupExecutor
    {
        // Async backup for Quartz
        public Task RunBackupAsync(BackupJob job)
        {
            return Task.Run(() => RunBackup(job));
        }

        // Backup execution
        public void RunBackup(BackupJob job)
        {
            Console.WriteLine($"[BackupExecutor] Running backup (method={job.Method.ToString().ToUpper()})");

            switch (job.Method)
            {
                case BackupMethod.Full:
                    RunFullBackup(job);
                    break;

                case BackupMethod.Differential:
                    RunDifferentialBackup(job);
                    break;

                case BackupMethod.Incremental:
                    RunIncrementalBackup(job);
                    break;
                default:
                    Console.WriteLine("[BackupExecutor] Unknown backup, defaulting to full.");
                    RunFullBackup(job);
                    break;
            }
        }

        // Full backup
        private void RunFullBackup(BackupJob job) 
        {
            foreach (var target in job.Targets)
            {
                try
                {
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"[BackupExecutor] Failed to write file to target '{target}: {e.Message}");
                }
            }
            Console.WriteLine("[BackupExecutor] Full backup completed.");
        }

        // Differential backup
        private void RunDifferentialBackup(BackupJob job) 
        {
            foreach (var target in job.Targets)
            {
                try
                {
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"[BackupExecutor] Failed to write file to target '{target}: {e.Message}");
                }
            }
            Console.WriteLine("[BackupExecutor] Differential backup completed.");
        }

        // Incremental backup
        private void RunIncrementalBackup(BackupJob job) 
        {
            foreach (var target in job.Targets)
            {
                try
                {
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"[BackupExecutor] Failed to write file to target '{target}: {e.Message}");
                }
            }
            Console.WriteLine("[BackupExecutor] Incremental backup completed.");
        }
    }
}
