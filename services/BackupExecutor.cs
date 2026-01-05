using backup_client.models;
using System.Diagnostics;
using System.IO;

namespace backup_client.services
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
                        Console.WriteLine("|__ [BackupExecutor][ERROR] Unknown backup, defaulting to full.");
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
            bool hasErrors = false;

            try
            {
                foreach (string rawSource in job.Sources)
                {
                    string source = Path.GetFullPath(rawSource);
                    DirectoryInfo sourceDir = new DirectoryInfo(source);

                    // Check if source directory exists
                    if (!sourceDir.Exists)
                    {
                        Console.Error.WriteLine($"|__ [BackupExecutor][ERROR] Source directory not found: {source}");
                        hasErrors = true;
                        continue;
                    }

                    foreach (string rawTarget in job.Targets)
                    {
                        string target = Path.GetFullPath(rawTarget);

                        // Create a container (for full it's useless, but keeps the structure cleaner for other methods)
                        string containerName = $"Container_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                        string containerPath = Path.Combine(target, containerName);

                        // Add -DateTime_FULL to directory name
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                        string fullBackupFolderName = $"Part_{timestamp}_FULL";
                        string outputBackupDir = Path.Combine(containerPath, fullBackupFolderName);

                        Directory.CreateDirectory(outputBackupDir);

                        // Get & copy all files (including in sub-dirs)
                        FileInfo[] allFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);

                        foreach (FileInfo file in allFiles)
                        {
                            // Relative source file path -> to get just the sub-dir/file instead of full 'C:/Documents/test.txt' path
                            string relSrcFilePath = Path.GetRelativePath(source, file.FullName);

                            // Final target file path with custom root directory
                            string targetFilePath = Path.Combine(outputBackupDir, relSrcFilePath);

                            // Create sub-directories
                            string? targetDirName = Path.GetDirectoryName(targetFilePath);
                            if (targetDirName != null)
                            {
                                Directory.CreateDirectory(targetDirName);
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
                if (hasErrors)
                {
                    Console.WriteLine("    |__ [BackupExecutor][WARNING] Full backup completed with errors.");
                }
                else
                {
                    Console.WriteLine("    |__ [BackupExecutor] Full backup completed successfully.");
                }
                Console.WriteLine($"    |__ [Summary] Files: {filesCopied} | Size: {totalBytes / 1024 / 1024} MB | Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"|__ [BackupExecutor][ERROR] Failed to write file to target: {e.Message}");
            }
        }

        // Differential backup
        private void RunDifferentialBackup(BackupJob job) 
        {
            // Stats & debug init
            Stopwatch stopwatch = Stopwatch.StartNew();
            int filesCopied = 0;
            long totalBytes = 0;
            bool hasErrors = false;

            try
            {
                foreach (string rawSource in job.Sources)
                {
                    string source = Path.GetFullPath(rawSource);
                    DirectoryInfo sourceDir = new DirectoryInfo(source);

                    if (!sourceDir.Exists)
                    {
                        Console.Error.WriteLine($"|__ [BackupExecutor][ERROR] Source directory not found: {source}");
                        hasErrors = true;
                        continue;
                    }
                }
                // Stats & debug output
                stopwatch.Stop();
                if (hasErrors)
                {
                    Console.WriteLine("    |__ [BackupExecutor][WARNING] Full backup completed with errors.");
                }
                else
                {
                    Console.WriteLine("    |__ [BackupExecutor] Full backup completed successfully.");
                }
                Console.WriteLine($"    |__ [Summary] Files: {filesCopied} | Size: {totalBytes / 1024 / 1024} MB | Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"|__ [BackupExecutor][ERROR] Failed to write file to target: {e.Message}");
            }
        }

        // Incremental backup
        private void RunIncrementalBackup(BackupJob job) 
        {
            // Stats & debug init
            Stopwatch stopwatch = Stopwatch.StartNew();
            int filesCopied = 0;
            long totalBytes = 0;
            bool hasErrors = false;

            try
            {
                foreach (string rawSource in job.Sources)
                {
                    string source = Path.GetFullPath(rawSource);
                    DirectoryInfo sourceDir = new DirectoryInfo(source);

                    // Check if source directory exists
                    if (!sourceDir.Exists)
                    {
                        Console.Error.WriteLine($"|__ [BackupExecutor][ERROR] Source directory not found: {source}");
                        hasErrors = true;
                        continue;
                    }

                    foreach (string rawTarget in job.Targets)
                    {
                        string target = Path.GetFullPath(rawTarget);
                        DirectoryInfo targetDir = new DirectoryInfo(target);

                        // Find latest container
                        DirectoryInfo? latestContainer = targetDir.GetDirectories("Container_*")
                            .OrderByDescending(d => d.CreationTime)
                            .FirstOrDefault();

                        // This shouldn't happen (RunRetentionPolicy() takes care of it), but just in case
                        if (latestContainer == null)
                        {
                            Console.Error.WriteLine($"|__ [BackupExecutor][ERROR] No existing container found.");
                            hasErrors = true;
                            continue;
                        }

                        // Find latest part
                        DirectoryInfo? lastPart = latestContainer.GetDirectories("Part_*")
                            .OrderByDescending(d => d.CreationTime)
                            .FirstOrDefault();

                        // This shouldn't happen as well, but just in case
                        if (lastPart == null)
                        {
                            Console.Error.WriteLine($"|__ [BackupExecutor][ERROR] Missing the necessary initial full backup.");
                            hasErrors = true;
                            continue;
                        }

                        // Cutoff DateTime from which to increment
                        DateTime lastBackupTime = lastPart.CreationTime;

                        // Add -DateTime_INCR to directory name
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                        string fullBackupFolderName = $"Part_{timestamp}_INCR";
                        string outputBackupDir = Path.Combine(latestContainer.FullName, fullBackupFolderName);

                        Directory.CreateDirectory(outputBackupDir);

                        // Get & copy all files (including in sub-dirs)
                        FileInfo[] allFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);

                        foreach (FileInfo file in allFiles)
                        {
                            // If newer than last backup -> save
                            if (file.LastWriteTime >= lastBackupTime || file.CreationTime >= lastBackupTime)
                            {
                                // Relative source file path
                                string relSrcFilePath = Path.GetRelativePath(source, file.FullName);

                                // Final target file path with custom root directory
                                string targetFilePath = Path.Combine(outputBackupDir, relSrcFilePath);

                                // Create sub-directories
                                string? targetDirName = Path.GetDirectoryName(targetFilePath);
                                if (targetDirName != null)
                                {
                                    Directory.CreateDirectory(targetDirName);
                                }

                                file.CopyTo(targetFilePath, true);

                                // Stats & debug calculate
                                filesCopied++;
                                totalBytes += file.Length;
                            }
                        }
                    }
                }

                // Stats & debug output
                stopwatch.Stop();
                if (hasErrors)
                {
                    Console.WriteLine("    |__ [BackupExecutor][WARNING] Full backup completed with errors.");
                }
                else
                {
                    Console.WriteLine("    |__ [BackupExecutor] Full backup completed successfully.");
                }
                Console.WriteLine($"    |__ [Summary] Files: {filesCopied} | Size: {totalBytes / 1024 / 1024} MB | Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"|__ [BackupExecutor][ERROR] Failed to write file to target: {e.Message}");
            }
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

                // Get all containers
                List<DirectoryInfo> containers = targetDir.GetDirectories("Container_*")
                    .OrderBy(d => d.CreationTime)
                    .ToList();

                // Size policy
                if (containers.Count == 0)
                {
                    forceFull = true;
                }
                else
                {
                    DirectoryInfo latestContainer = containers.Last();

                    int currentPartCount = latestContainer.GetDirectories("Part_*").Length;

                    if (currentPartCount >= maxIncrementals)
                        forceFull = true;
                }

                // Count policy
                int currentContainerCount = containers.Count;
                int containersToDelete = 0;

                if (forceFull || job.Method == BackupMethod.Full)
                {
                    // New container is going to be created
                    if (currentContainerCount >= maxHistory)
                        containersToDelete = (currentContainerCount - maxHistory) + 1;
                }
                else
                {
                    // We are staying in the existing container (delete if already over the limit)
                    if (currentContainerCount > maxHistory)
                        containersToDelete = currentContainerCount - maxHistory;
                }

                if (containersToDelete > 0)
                {
                    containersToDelete = Math.Min(containersToDelete, containers.Count);

                    for (int i = 0; i < containersToDelete; i++)
                    {
                        try
                        {
                            containers[i].Delete(true);
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine($"|__ [BackupExecutor][ERROR] Failed to delete container: {e.Message}");
                        }
                    }

                    // Update backups list
                    containers = targetDir.GetDirectories("Container_*")
                    .OrderBy(d => d.CreationTime)
                    .ToList();
                }
            }

            // Exception for FULL backup (ignore size & print 'full' instead of 'forced_full')
            if (job.Method == BackupMethod.Full) 
                return false;

            return forceFull;
        }
    }
}
