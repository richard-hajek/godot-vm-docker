using System;
using System.IO;

namespace hackfest2.addons.Utils
{
    public static class Logger
    {

        private static string logFolder = Path.Combine(GameFiles.UserDirectoryPath, "log");
        private static string _logFile;
        
        public static string LogFile
        {
            get
            {
                if (_logFile != null)
                    return _logFile;
            
                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);
                
                _logFile = Path.Combine(logFolder, DateTime.Now.ToString("MM,dd,HH:mm:ss") + ".log");

                return _logFile;
            }
        }

        public static void Log(StreamReader reader, string tag)
        {
            ReadStreamAsync(reader, tag);
        }

        private static async void ReadStreamAsync(StreamReader reader, string tag)
        {
            while (!reader.EndOfStream)
            {
                var message = await reader.ReadLineAsync();
                Log($"[{tag}] {message}");
            }
        }
        
        public static void Log(string message)
        {
            File.AppendAllText(LogFile, $"{DateTime.Now:HH:mm:ss} {message} \n");
        }

        public static void WriteLine(string message)
        {
            Log(message);
        }
    }
}