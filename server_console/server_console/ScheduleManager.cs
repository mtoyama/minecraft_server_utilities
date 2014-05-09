using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Timers;

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
            DateTime currentTime = DateTime.Now;
            string parsedCurrentTime = currentTime.ToString(_timePatt);
            return parsedCurrentTime;
        }


        public void ScheduledBackupRestart(object sender, ElapsedEventArgs e)
        {
            int currentTimeMinutes = Convert.ToInt32(e.SignalTime.TimeOfDay.TotalMinutes);
            int backupTimeMinutes = Convert.ToInt32(backupManager.dailyBackupTime.TimeOfDay.TotalMinutes);

            if (currentTimeMinutes == backupTimeMinutes - 60)
            {
                commandProcessor.ServerCommandExternal(
                                @"/say WARNING: Server will be restarting for scheduled maintenance in 60 minutes.");
            }

            if (currentTimeMinutes == backupTimeMinutes - 15)
            {
                commandProcessor.ServerCommandExternal(
                                @"/say WARNING: Server will be restarting for scheduled maintenance in 15 minutes.");
            }
           

            if (currentTimeMinutes == backupTimeMinutes)
            {
                ColorConsoleOutput.YellowEvent("Time for a backup! Server will be shut down and restarted.");
                commandProcessor.ServerCommandExternal(
                                @"/say SERVER RESTART INITIATED! It will be back up momentarily.");
                commandProcessor.ExecuteCommand("stop");
                backupManager.DoBackup();
                Thread.Sleep(20000);
                Process.Start("shutdown", "-r -t 0");
            }
        }

    }

}
