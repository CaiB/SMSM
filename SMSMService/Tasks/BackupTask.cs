using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSMService.Tasks
{
    public static class BackupTask
    {
        private const string BACKUP_DIR_NAME = "SMSMBackups";
        private const string DATE_FORMAT = "yyyy-MM-dd\\_HH-mm-ss";

        // These get set by the config reader.
        public static int MaxBackupCount;
        public static string[] Exclusions = Array.Empty<string>();

        private static string? RootPath = null;
        private static string? BackupPath = null;
        private static Matcher? FileMatcher;

        public static void Init()
        {
            if (SMSM.ServerDir == null || !Directory.Exists(SMSM.ServerDir)) { Log.Error($"Backup could not be performed because the server directory '{SMSM.ServerDir}' is invalid."); return; }
            RootPath = SMSM.ServerDir;
            BackupPath = Path.Combine(SMSM.ServerDir, BACKUP_DIR_NAME);
            if (!Directory.Exists(BackupPath)) { Directory.CreateDirectory(BackupPath); }
            FileMatcher = new();
            FileMatcher.AddExclude($"{BACKUP_DIR_NAME}/*");
            FileMatcher.AddExclude($"{BACKUP_DIR_NAME}/**/*");
            FileMatcher.AddExcludePatterns(Exclusions);

            FileMatcher.AddInclude("*");
            FileMatcher.AddInclude("**/*");
        }

        public static void Run()
        {
            if (BackupPath == null || FileMatcher == null || RootPath == null) { Log.Error("Not ready to take a backup. Check the SMSM log for errors at startup."); return; }

            // Delete oldest backups if there are too many
            List<string> CurrentBackups = Directory.GetFiles(BackupPath, "SMSM_*.zip").ToList();
            while (CurrentBackups.Count >= MaxBackupCount)
            {
                DateTime OldestFile = DateTime.MaxValue;
                int OldestFileIndex = -1;
                for (int i = 0; i < CurrentBackups.Count; i++) // Find the oldest file
                {
                    string ThisDate = Path.GetFileNameWithoutExtension(Path.GetRelativePath(BackupPath, CurrentBackups[i]));
                    ThisDate = ThisDate.Substring(5);
                    if (ThisDate.Length != 19) { continue; }
                    DateTime ThisFileDate = DateTime.ParseExact(ThisDate, DATE_FORMAT, CultureInfo.InvariantCulture);
                    if (ThisFileDate < OldestFile)
                    {
                        OldestFile = ThisFileDate;
                        OldestFileIndex = i;
                    }
                }

                if (OldestFileIndex != -1) // If we found a file to delete
                {
                    string FileToDelete = CurrentBackups[OldestFileIndex];
                    try
                    {
                        Log.Info($"Deleting old backup \"{FileToDelete}\"");
                        File.Delete(Path.Combine(BackupPath, FileToDelete));
                        CurrentBackups.RemoveAt(OldestFileIndex);
                    }
                    catch (Exception Exc)
                    {
                        Log.Warn($"Could not delete old backup file \"{FileToDelete}\".");
                        Log.Warn(Exc.ToString());
                    }
                }
            }

            // Create new backup
            string Date = DateTime.Now.ToString(DATE_FORMAT);
            string CurrentBackup = Path.Combine(BackupPath, $"SMSM_{Date}.zip");

            int FileCount = 0;
            using (FileStream ZIPFileStream = new(CurrentBackup, FileMode.Create))
            {
                using (ZipArchive ZIPFile = new(ZIPFileStream, ZipArchiveMode.Create))
                {
                    foreach (string FilePath in FileMatcher.GetResultsInFullPath(RootPath))
                    {
                        try
                        {
                            string Relative = Path.GetRelativePath(RootPath, FilePath);
                            using (FileStream InputFile = new(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                            {
                                ZipArchiveEntry ZIPEntry = ZIPFile.CreateEntry(Relative);
                                using (Stream ZIPContentStream = ZIPEntry.Open())
                                {
                                    InputFile.CopyTo(ZIPContentStream);
                                }
                            }
                            FileCount++;
                        }
                        catch (Exception Exc)
                        {
                            Log.Error($"Failed to back up file \"{FilePath}\"");
                            Log.Error(Exc.ToString());
                        }
                    }
                }
            }
            Log.Info($"Backed up {FileCount} files.");
        }
    }
}
