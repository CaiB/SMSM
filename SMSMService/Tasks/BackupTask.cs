using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

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

        private static void DoBackup()
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
            string NewFileName = $"SMSM_{Date}.zip";
            string CurrentBackup = Path.Combine(BackupPath, NewFileName);

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
            Log.Info($"Backed up {FileCount} files into {NewFileName}");
        }

        private static bool SaveFinished = false;
        private static bool AutosaveDisabled = false;

        public static void Run()
        {
            if (Server.ServerReady) // Don't try to wait for the server if it's not even running
            {
                SaveFinished = false;
                AutosaveDisabled = false;
                Server.ServerOutput += HandleServerOutput;
                Server.SendInput("/save-all");
                Server.SendInput("/save-off");

                // Wait for up to 10 seconds for the server to finish saving.
                Stopwatch TimeCheck = new();
                TimeCheck.Start();
                while (TimeCheck.ElapsedMilliseconds < 10000)
                {
                    if (SaveFinished && AutosaveDisabled) { break; }
                    Thread.Sleep(100);
                }
                TimeCheck.Stop();
                Server.ServerOutput -= HandleServerOutput;

                if (!SaveFinished)
                {
                    Log.Error("Could not take backup because the server didn't finish saving the world in time.");
                    Server.SendInput("/save-on");
                    return;
                }

                Thread.Sleep(500);
            }
            DoBackup();
            Thread.Sleep(500);
            Server.SendInput("/save-on");
        }

        private static void HandleServerOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                if (e.Data.EndsWith("Turned off world auto-saving")) { AutosaveDisabled = true; }
                else if (e.Data.EndsWith("Saved the world")) { SaveFinished = true; }
            }
        }
    }
}
