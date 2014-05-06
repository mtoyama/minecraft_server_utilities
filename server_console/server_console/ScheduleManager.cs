using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace server_console
{

    public class ScheduleManager
    {
        private volatile bool _shouldStop = false;
        private static string _timePatt = @"HH:mm";
        BackupManager backupManager;
        ApplicationCommandProcessor commandProcessor;

        public ScheduleManager(BackupManager pBackupManager)
        {
            backupManager = pBackupManager;
        }

        public void SetCommandProcessor(ApplicationCommandProcessor pCommandProcessor)
        {
            commandProcessor = pCommandProcessor;
        }

        public static string GetCurrentTime()
        {
            DateTime currentTime = DateTime.Now; //$TODO Need to change all of these calls to TimeOfDay, then pass around the DateTime object
            // so that I can parse the object instead of pass around the parsed string
            string parsedCurrentTime = currentTime.ToString(_timePatt);
            return parsedCurrentTime;
        }

        public void TimeMonitor()
        {
            // Right now this is super hard-coded to perform a backup. We need to set this up like an event system at some point.
            while (!_shouldStop)
            {
                string currentTime = GetCurrentTime();
                if (currentTime == backupManager.GetDailyBackupTime())
                {
                    ColorConsoleOutput.YellowEvent("Time for a backup!");
                    commandProcessor.ExecuteCommand("stop");
                    backupManager.DoBackup();
                    Thread.Sleep(20000);
                    Process.Start("shutdown", "-r -t 0");
                    continue;
                }
                Thread.Sleep(20000);
            }
        }
    }
}
