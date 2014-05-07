using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace server_console
{
    class ServerConsole
    {
        static void Main(string[] args)
        {
            
            try
            {
                // Code to regenerate config file
                // ConfigManager.SerializeAndOutputConfigXML();
                XmlSerializer serializer = new XmlSerializer(typeof(Configuration));

                Configuration inputConfig = new Configuration();
                using (Stream reader = new FileStream(@".\config.xml", FileMode.Open))
                {
                    inputConfig = (Configuration)serializer.Deserialize(reader);
                }


                // DAS VARIABLES
                string serverRootDirectory = inputConfig.serverRoot;
                string serverJarFilename = inputConfig.jarName;
                string applicationInputPrefix = inputConfig.appInputPrefix;
                string javaPath = inputConfig.javaPath;
                string dailyBackupTime = inputConfig.dailyBackupTime;
                string backupDirectory = inputConfig.backupLocation;
                int totalBackupRotations = inputConfig.totalBackupRotations;

                string serverJarFullPath = Path.Combine(serverRootDirectory, serverJarFilename);
                string serverJarFileJavaParameter = "-jar " + serverJarFullPath;

                string inputParameters = "";
                foreach (string argument in inputConfig.serverStartupArguments)
                {
                    inputParameters += argument + " ";
                }
                
                // Set current dir to server root dir
                Directory.SetCurrentDirectory(serverRootDirectory);

                // Define the properties of the Java process 
                ProcessStartInfo ProcessInfo;
                Process serverJavaProcess;
                ProcessInfo = new ProcessStartInfo(javaPath, serverJarFileJavaParameter + " " + inputParameters);
                ProcessInfo.CreateNoWindow = false;
                ProcessInfo.UseShellExecute = false;
                ProcessInfo.RedirectStandardOutput = true; // Someday we could redirect the STDOUT to do additional processing on it. Someday.
                ProcessInfo.RedirectStandardInput = true; // Need to capture STDIN to push console input to server
                ProcessInfo.RedirectStandardError = true;

                // Start the Java process
                serverJavaProcess = Process.Start(ProcessInfo);



                // Create the input and output streams for the server
                // $TODO redirect standard error
                StreamWriter writeToServer = serverJavaProcess.StandardInput;

                // Create my Backup and Schedule Managers
                // Schedule manager needs objects for anything it's going to control
                BackupManager backupManager = new BackupManager(serverRootDirectory, totalBackupRotations, dailyBackupTime, backupDirectory);
                ScheduleManager scheduleManager = new ScheduleManager(backupManager);

                // Create my ApplicationCommandProcessor
                ApplicationCommandProcessor commandProcessor = new ApplicationCommandProcessor(applicationInputPrefix, writeToServer, serverJavaProcess, backupManager);
                scheduleManager.SetCommandProcessor(commandProcessor);

                // Add event handlers for STDOUT and STDERR; everything comes out as STDERR from the server... but just in case....
                serverJavaProcess.OutputDataReceived += commandProcessor.CaptureOutput;
                serverJavaProcess.ErrorDataReceived += commandProcessor.CaptureOutput;
                serverJavaProcess.BeginOutputReadLine();
                serverJavaProcess.BeginErrorReadLine();

                // Start my thread to monitor user input
                Thread consoleInputThread = new Thread(commandProcessor.UserInputMonitor);
                //consoleInputThread.IsBackground = true;
                consoleInputThread.Start();

                // Start my thread to monitor time
                Thread timeMonitorThread = new Thread(scheduleManager.TimeMonitor);
                timeMonitorThread.IsBackground = true;
                timeMonitorThread.Start();

                // Subscribe the scheduled backup method to the current time event
                scheduleManager.CurrentTimeEvent += scheduleManager.ScheduledBackupRestart;

                serverJavaProcess.WaitForExit();
                ColorConsoleOutput.YellowEvent("Server has been shut down.");
                
                // Restart the server if we did not manually shut it down.
                if (commandProcessor.manualShutdown == false)
                {
                    writeToServer.Dispose();
                    commandProcessor.ProcessCommand("start");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.InnerException);
                Console.WriteLine(e.StackTrace);
                
            }
        }
    }
}
