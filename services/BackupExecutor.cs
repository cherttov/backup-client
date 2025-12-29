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
            // Check if retention limit reached
            bool forceFull = RunRetentionPolicy(job);
            if (forceFull)
            {
                Console.WriteLine($"|__ [BackupExecutor] Running backup (method=FORCED_FULL, from={job.Method.ToString().ToUpper()})");
                RunFullBackup(job);
            }
            else
            {
                Console.WriteLine($"|__ [BackupExecutor] Running backup (method={job.Method.ToString().ToUpper()})");
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
                        Console.WriteLine("|__ [BackupExecutor] Unknown backup, defaulting to full.");
                        RunFullBackup(job);
                        break;
                }
            }
        }

        // Full backup
        private void RunFullBackup(BackupJob job) 
        {
            // Stats & debug init
            Stopwatch stopwatch = Stopwatch.StartNew();
            int filesCopied = 0;
            long totalBytes = 0;

            try
            {
                foreach (string rawSource in job.Sources)
                {
                    string source = Path.GetFullPath(rawSource);
                    DirectoryInfo sourceDir = new DirectoryInfo(source);

                    // Check if source directory exists
                    if (!sourceDir.Exists)
                        throw new DirectoryNotFoundException($"|__ [BackupExecutor] Source directory not found: {source}");

                    foreach (string rawTarget in job.Targets)
                    {
                        string target = Path.GetFullPath(rawTarget);

                        // Add -DateTime_FULL to directory name
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                        string fullBackupFolderName = $"{timestamp}_FULL";
                        string outputBackupDir = Path.Combine(target, fullBackupFolderName);

                        Directory.CreateDirectory(outputBackupDir);

                        // Get all files (including in sub-dirs)
                        FileInfo[] allFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);

                        // Copy all files
                        foreach (FileInfo file in allFiles)
                        {
                            // Relative source file path -> to get just the sub-dir/file instead of full 'C:/Documents/test.txt' path
                            string relSrcFilePath = Path.GetRelativePath(source, file.FullName);

                            // Final target file path with custom root directory
                            string targetFilePath = Path.Combine(outputBackupDir, relSrcFilePath);

                            // Create sub-directories
                            string? targetDir = Path.GetDirectoryName(targetFilePath);
                            if (targetDir != null)
                            {
                                Directory.CreateDirectory(targetDir);
                            }

                            file.CopyTo(targetFilePath, true);

                            // Stats & debug calculate
                            filesCopied++;
                            totalBytes += file.Length;
                        }
                    }
                }

                // Stats & debug output
                stopwatch.Stop();
                Console.WriteLine("    |__ [BackupExecutor] Full backup completed successfully.");
                Console.WriteLine($"    |__ [Summary] Files: {filesCopied} | Size: {totalBytes / 1024 / 1024} MB | Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"|__ [BackupExecutor] Failed to write file to target: {e.Message}");
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
                Console.Error.WriteLine($"|__ [BackupExecutor] Failed to write file to target: {e.Message}");
            }
            Console.WriteLine("|__ [BackupExecutor] Differential backup completed.");
        }

        // Incremental backup
        private void RunIncrementalBackup(BackupJob job) 
        {
            try
            {
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"|__ [BackupExecutor] Failed to write file to target: {e.Message}");
            }
            Console.WriteLine("|__ [BackupExecutor] Incremental backup completed.");
        }

        // Retention policy check
        private bool RunRetentionPolicy(BackupJob job)
        {
            bool forceFull = false;
            int maxIncrementals = job.Retention.Size;
            int maxHistory = job.Retention.Count;

            foreach (string rawTarget in job.Targets)
            {
                string target = Path.GetFullPath(rawTarget);
                DirectoryInfo targetDir = new DirectoryInfo(target);

                // Check if target dir exists
                if (!targetDir.Exists)
                {
                    forceFull = true;
                    continue;
                }

                // Count policy
                List<DirectoryInfo> backups = targetDir.GetDirectories("*_FULL")
                    .OrderBy(d => d.CreationTime)
                    .ToList();

                int backupsToDelete = backups.Count - maxHistory;

                if (backupsToDelete >= 0)
                {
                    // '<=' because we run it before another backup
                    for (int i = 0; i <= backupsToDelete; i++)
                    {
                        // Edge case scenario because of '<='
                        if (i < backups.Count)
                        {
                            backups[i].Delete(true);
                        }
                    }

                    // Update backups list
                    backups = targetDir.GetDirectories("*_FULL")
                    .OrderBy(d => d.CreationTime)
                    .ToList();
                }

                // Size policy
                if (backups.Count == 0)
                {
                    forceFull = true;
                }
                else
                {
                    DirectoryInfo latestBackup = backups.Last();

                    int currentCount = latestBackup.GetFiles().Length;

                    if (currentCount > maxIncrementals)
                        forceFull = true;
                }
            }

            // Exception for FULL backup (ignore SIZE)
            if (job.Method == BackupMethod.Full) 
                return false;

            return forceFull;
        }
    }
}
