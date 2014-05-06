using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace server_console
{
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
            StreamWriter newServerStreamWriter = serverProcess.StandardInput;
            serverStreamWriter = newServerStreamWriter;
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
                    ColorConsoleOutput.YellowEvent("Next backup will occur at: ", backupManager.GetDailyBackupTime());
                    break;

                case "exit":
                    ColorConsoleOutput.YellowEvent("Shutting down server and exiting program.");
                    StopServer();
                    Environment.Exit(0);
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
}
