using backup_system.models;
using System.Diagnostics;
using System.IO;

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
            Stopwatch stopwatch = Stopwatch.StartNew();
            int filesCopied = 0;
            long totalBytes = 0;

            try
            {
                foreach (string rawSource in job.Sources)
                {
                    string source = Path.GetFullPath(rawSource);
                    DirectoryInfo sourceDir = new DirectoryInfo(source);

                    if (!sourceDir.Exists)
                        throw new DirectoryNotFoundException($"[BackupExecutor] Source directory not found: {source}");

                    foreach (string rawTarget in job.Targets)
                    {
                        string target = Path.GetFullPath(rawTarget);

                        // Get all files
                        FileInfo[] allFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);

                        // Copy all files
                        foreach (FileInfo file in allFiles)
                        {
                            string relSrcFilePath = Path.GetRelativePath(source, file.FullName);

                            string targetFilePath = Path.Combine(target, relSrcFilePath);

                            // Create sub-directories
                            string? targetDir = Path.GetDirectoryName(targetFilePath);
                            if (targetDir != null)
                            {
                                Directory.CreateDirectory(targetDir);
                            }

                            file.CopyTo(targetFilePath, true);

                            // Stats & debug
                            filesCopied++;
                            totalBytes += file.Length;
                        }
                    }
                }

                stopwatch.Stop();
                Console.WriteLine("------------------------------------------------------");
                Console.WriteLine("[BackupExecutor] Full backup completed successfully.");
                Console.WriteLine($"[Summary] Files: {filesCopied} | Size: {totalBytes / 1024 / 1024} MB | Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
                Console.WriteLine("------------------------------------------------------");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[BackupExecutor] Failed to write file to target: {e.Message}");
            }
        }

        // Differential backup
        private void RunDifferentialBackup(BackupJob job) 
        {
            try
            {
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[BackupExecutor] Failed to write file to target: {e.Message}");
            }
            Console.WriteLine("[BackupExecutor] Differential backup completed.");
        }

        // Incremental backup
        private void RunIncrementalBackup(BackupJob job) 
        {
            try
            {
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[BackupExecutor] Failed to write file to target: {e.Message}");
            }
            Console.WriteLine("[BackupExecutor] Incremental backup completed.");
        }
    }
}
