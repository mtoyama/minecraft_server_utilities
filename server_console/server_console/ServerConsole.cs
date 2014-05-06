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
    public class ApplicationCommandProcessor
    {
        private volatile bool _shouldStopUserInputMonitor = false;
        public bool manualShutdown = false;
        string applicationInputPrefix;
        StreamWriter serverStreamWriter;
        Process serverProcess;
        BackupManager backupManager;

        public ApplicationCommandProcessor(string pApplicationInputPrefix, StreamWriter pServerStreamWriter, Process pServerProcess, BackupManager pBackupManager)
        {
            applicationInputPrefix = pApplicationInputPrefix;
            serverStreamWriter = pServerStreamWriter;
            serverProcess = pServerProcess;
            backupManager = pBackupManager;
        }

        public void StopServer()
        {
            // Let the main method know we shut down the server.
            manualShutdown = true;
            serverStreamWriter.WriteLine("/stop");
            // Wait for the process to finish dying.
            serverProcess.WaitForExit();
        }

        public void StartServer()
        {
            // We don't want to try to start the server process until the existing process has exited.
            serverProcess.WaitForExit();
            // Reset manualShutdown to false; we only set this to true when we stop the server.
            manualShutdown = false;
            serverProcess.Start();
            // We need to re-set the stream writer because we lost it when the previous serverProcess stopped.
            StreamWriter serverStreamWriter = serverProcess.StandardInput;
        }

        public void ProcessCommand(string pApplicationCommand)
        {
            string command = pApplicationCommand;


            // Strip away the prefix if it exists. It better exist.
            int index = pApplicationCommand.IndexOf(applicationInputPrefix);
            command = (index < 0)
                ? pApplicationCommand
                : pApplicationCommand.Remove(index, applicationInputPrefix.Length);

            // Trim away leading and trailing spaces
            command = command.Trim();
            // Lower case dat bitch.
            command = command.ToLower();

            ExecuteCommand(command);
        }

        public void ExecuteCommand(string pApplicationCommandProcessed)
        {
            //ColorConsoleOutput.YellowEvent("Executing command: " + pApplicationCommandProcessed);
            switch (pApplicationCommandProcessed)
            {
                // Put valid commands here. Instead of putting the messaging in the methods, which lacks context,
                // we're putting the messaging in the command 'scripts' themselves. It's more likely that I'd want
                // a series of contextual-specific messages rather than prebaked messages every time I call a method.
                // $TODO: Handle multi-parameter commands
                // $TODO: Handle bad inputs


                case "help":
                    ColorConsoleOutput.YellowEvent("Valid arguments:");
                    ColorConsoleOutput.YellowEvent("BACKUP      Creates a backup of server files in the 'backups' directory.");
                    ColorConsoleOutput.YellowEvent("STOP        Warns users, stops the server. Server util will continue to run.");
                    ColorConsoleOutput.YellowEvent("START       Starts the server. Server must be stopped.");
                    ColorConsoleOutput.YellowEvent("RESTART     Warns users, restarts the server.");
                    ColorConsoleOutput.YellowEvent("NEXTBACKUP  Show scheduled backup time.");
                    break;

                case "backup":
                    ColorConsoleOutput.YellowEvent("Creating new backup of server files.");
                    backupManager.DoBackup();
                    ColorConsoleOutput.YellowEvent("Backup complete.");
                    break;

                case "stop":
                    ColorConsoleOutput.YellowEvent("Stopping server in 30 seconds. Warning the users first.");
                    serverStreamWriter.WriteLine(
                        "/say Server will be shutting down in 30 seconds. Have a nice day.");
                    Thread.Sleep(30000);
                    StopServer();
                    break;

                case "start":
                    ColorConsoleOutput.YellowEvent("Starting server");
                    StartServer();
                    break;

                case "restart":
                    ColorConsoleOutput.YellowEvent("Initializing restart sequence.");
                    ColorConsoleOutput.YellowEvent("Stopping server in 30 seconds. Warning the users first.");
                    serverStreamWriter.WriteLine(
                        "/say Server will be shutting down in 30 seconds. Have a nice day.");
                    Thread.Sleep(30000);
                    StopServer();
                    ColorConsoleOutput.YellowEvent("Starting server back up in 10 seconds...");
                    Thread.Sleep(10000);
                    ColorConsoleOutput.YellowEvent("Here we go!");
                    StartServer();
                    break;

                case "nextbackup":
                    ColorConsoleOutput.YellowEvent("Next backup will occur at: ",backupManager.GetDailyBackupTime());
                    break;

                default:
                    ColorConsoleOutput.YellowEvent("Invalid command entered.");
                    ExecuteCommand("help");
                    break;

            }
        }
        public void UserInputMonitor()
        {
            while (!_shouldStopUserInputMonitor)
            {
                string userInput;
                userInput = Console.ReadLine(); // Get user input
                if (userInput.StartsWith(applicationInputPrefix) == true)
                {
                    //Console.WriteLine("Application command accepted.");
                    ProcessCommand(userInput);
                    continue; // continue to next iteration
                }

                serverStreamWriter.WriteLine(userInput);
            }
        }

        public void StopUserInputMonitor()
        {
            _shouldStopUserInputMonitor = true;
        }
    }



    public class BackupManager
    {
        int backupRotations;
        string serverRoot;
        string backupDirectory;
        string folderFormat = @"yyyymmdd_HHmmss";
        string dailyBackupTime;

        public BackupManager(string pServerRoot, int pBackupRotations, string pDailyBackupTime)
        {
            serverRoot = pServerRoot;
            backupRotations = pBackupRotations;
            dailyBackupTime = pDailyBackupTime;
            CreateAndSetBackupRootFolder();
        }

        public string GetDailyBackupTime()
        {
            return dailyBackupTime;
        }

        public void CreateAndSetBackupRootFolder()
        {
            string tempBackupDirectory = Path.Combine(serverRoot, "backups");
            if (!Directory.Exists(tempBackupDirectory))
            {
                Directory.CreateDirectory(tempBackupDirectory);
            }
            backupDirectory = tempBackupDirectory;
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
