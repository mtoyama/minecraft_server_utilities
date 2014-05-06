﻿using System;
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

                //ConfigManager.SerializeAndOutputConfigXML();
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
                //ProcessInfo.WorkingDirectory = @"C:\Windows\SysWOW64";
                ProcessInfo.CreateNoWindow = false;
                ProcessInfo.UseShellExecute = false;
                ProcessInfo.RedirectStandardOutput = false; // Someday we could redirect the STDOUT to do additional processing on it. Someday.
                ProcessInfo.RedirectStandardInput = true; // Need to capture STDIN to push console input to server

                // Start the Java process
                serverJavaProcess = Process.Start(ProcessInfo);

                // Create the StreamWriter I'm going to use to pass inputs back to the java process
                StreamWriter writeToServer = serverJavaProcess.StandardInput;

                // Create my Backup and Schedule Managers
                // Schedule manager needs objects for anything it's going to control
                BackupManager backupManager = new BackupManager(serverRootDirectory, totalBackupRotations, dailyBackupTime);
                ScheduleManager scheduleManager = new ScheduleManager(backupManager);

                // Create my ApplicationCommandProcessor
                ApplicationCommandProcessor commandProcessor = new ApplicationCommandProcessor(applicationInputPrefix, writeToServer, serverJavaProcess, backupManager);
                scheduleManager.SetCommandProcessor(commandProcessor);

                // Start my thread to monitor user input
                Thread consoleInputThread = new Thread(commandProcessor.UserInputMonitor);
                consoleInputThread.IsBackground = true;
                consoleInputThread.Start();

                // Start my thread to monitor time
                Thread timeMonitorThread = new Thread(scheduleManager.TimeMonitor);
                timeMonitorThread.Start();

                serverJavaProcess.WaitForExit();
                ColorConsoleOutput.YellowEvent("Server has been shut down.");
                
                // Restart the server if we did not manually shut it down.
                if (commandProcessor.manualShutdown == false)
                {
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
