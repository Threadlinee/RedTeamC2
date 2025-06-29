using System;
using System.IO;

namespace Agent.Utils
{
    public static class Logger
    {
        private static readonly string logFile = "agent.log";

        public static void Log(string msg)
        {
            File.AppendAllText(logFile, DateTime.Now + " " + msg + Environment.NewLine);
        }

        public static void LogOpsec(string msg)
        {
            File.AppendAllText(logFile, DateTime.Now + " [OPSEC] " + msg + Environment.NewLine);
        }
    }
}
