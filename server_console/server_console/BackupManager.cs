using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace server_console
{
    public class BackupManager
    {
        int backupRotations;
        string serverRoot;
        string backupDirectory;
        string folderFormat = @"yyyymmdd_HHmmss";
        string dailyBackupTime;

        public BackupManager(string pServerRoot, int pBackupRotations, string pDailyBackupTime, string pBackupDirectory)
        {
            backupRotations = pBackupRotations;
            serverRoot = pServerRoot;
            backupDirectory = pBackupDirectory;
            dailyBackupTime = pDailyBackupTime;

            CreateAndSetBackupRootFolder();
        }

        public string GetDailyBackupTime()
        {
            return dailyBackupTime;
        }

        public void CreateAndSetBackupRootFolder()
        {
            //string tempBackupDirectory = Path.Combine(serverRoot, "backups");
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }
            //backupDirectory = tempBackupDirectory;
        }

        public string DetermineBackupFolder()
        {
            DirectoryInfo dir = new DirectoryInfo(backupDirectory);
            DirectoryInfo[] dirs = dir.GetDirectories();

            int backupFolderCount = 0;
            DateTime oldestDirectory = DateTime.Now;
            string oldestDirectoryPath = "";
            foreach (DirectoryInfo subdir in dirs)
            {
                if (subdir.FullName.Contains("serverbackup_"))
                {
                    backupFolderCount = backupFolderCount + 1;
                    if (oldestDirectory > subdir.CreationTime)
                    {
                        oldestDirectory = subdir.CreationTime;
                        oldestDirectoryPath = subdir.FullName;
                    }
                }
            }

            //ColorConsoleOutput.YellowEvent("Backup folder count: " + backupFolderCount);
            string backupFolderName = Path.Combine(backupDirectory, "serverbackup_" + DateTime.Now.ToString(folderFormat));
            if (backupFolderCount < backupRotations)
            {
                Directory.CreateDirectory(backupFolderName);
                return backupFolderName;
            }
            else
            {
                ColorConsoleOutput.YellowEvent("Maximum rotations reached. Replacing oldest backup.");
                // This is shit. If I somehow return "", the program will explode.
                Directory.Delete(oldestDirectoryPath, true);
                Directory.CreateDirectory(backupFolderName);
                return backupFolderName;
            }
        }


        public void DoBackup()
        {
            DoBackupRecursion(serverRoot, DetermineBackupFolder());
        }


        public void DoBackupRecursion(string pSourceDir, string pDestDir)
        {
            // FUN!!!
            // DO NOT INCLUDE BACKUPS!
            DirectoryInfo dir = new DirectoryInfo(pSourceDir);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Server directory does not exist or could not be found: "
                    + dir);
            }

            if (!Directory.Exists(pDestDir))
            {
                Directory.CreateDirectory(pDestDir);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(pDestDir, file.Name);
                file.CopyTo(temppath, true);
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                if (subdir.FullName.Contains(backupDirectory))
                {
                    continue;
                }
                string temppath = Path.Combine(pDestDir, subdir.Name);
                DoBackupRecursion(subdir.FullName, temppath);
            }

        }

    }
}
