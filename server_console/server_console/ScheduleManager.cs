using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections;

namespace server_console
{
    public delegate void ChangedEventHandler(object sender, EventArgs e);
    public class ScheduleManager
    {
        public event EventHandler<CurrentTimeEventArgs> CurrentTimeEvent;
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
            while (!_shouldStop)
            {
                string currentTime = GetCurrentTime();
                CurrentTimeEventArgs time = new CurrentTimeEventArgs();
                time.currentTime = currentTime;
                TimeGetReached(time);
                Thread.Sleep(20000);
            }

        }

        protected virtual void TimeGetReached(CurrentTimeEventArgs e)
        {
            EventHandler<CurrentTimeEventArgs> handler = CurrentTimeEvent;
            if (handler != null)
                handler(this, e);
        }

        public void ScheduledBackupRestart(object sender, CurrentTimeEventArgs e)
        {
            if (e.currentTime == backupManager.GetDailyBackupTime())
            {
                ColorConsoleOutput.YellowEvent("Time for a backup! Server will be shut down and restarted.");
                commandProcessor.ExecuteCommand("stop");
                backupManager.DoBackup();
                Thread.Sleep(20000);
                Process.Start("shutdown", "-r -t 0");
            }
        }

    }

    public class CurrentTimeEventArgs : EventArgs
    {
        public string currentTime { get; set; }
    }
}
