using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Serialization;

namespace server_console
{
    public class Configuration
    {
        public string javaPath;
        public string serverRoot;
        public string jarName;
        public string appInputPrefix;
        public string backupLocation;
        public string dailyBackupTime;
        public List<string> serverStartupArguments;
    }
    
    class ConfigManager
    {


        public static void SerializeAndOutputConfigXML()
        {
            Configuration template = new Configuration();
            template.javaPath = @"C:\Program Files\Java\jre7\bin\java.exe";
            template.serverRoot = "D:\\Users\\mtoyama\\Desktop\\MagicFarmServer";
            template.jarName = "FTBServer-1.6.4-965.jar";
            template.appInputPrefix = "/serverconfig";
            template.backupLocation = "D:\\Users\\mtoyama\\Desktop\\MagicFarmServer\\backups";
            template.dailyBackupTime = "03:00";
            template.serverStartupArguments = new List<string>{
                "-Xms2048m",
                "-XX:+UseConcMarkSweepGC",
                "-XX:+CMSIncrementalPacing",
                "-server",
                "-XX:+AggressiveOpts",
                "-XX:UseSSE=7",
                "-XX:+UseFastAccessorMethods",
                "-XX:CMSFullGCsBeforeCompaction=1",
                "-XX:+CMSParallelRemarkEnabled",
                "-XX:+UseCMSCompactAtFullCollection",
                "-XX:+UseParNewGC",
                "-XX:+DisableExplicitGC",
                "-XX:parallelGCThreads=2",
                "nogui"
            };

            using (StreamWriter sw = new StreamWriter(@".\config.xml"))
            {
                XmlSerializer xs = new XmlSerializer(typeof(Configuration));
                xs.Serialize(sw, template);
            }
        }
    }
}
