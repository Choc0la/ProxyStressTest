using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using MinecraftBots.Protocol.Server;

namespace MinecraftBots
{
    class Content
    {
        static string ServerIP;
        static int ServerPort;
        static int ProtocolVersion;
        public static void Init()
        {
            try {
                Console.WriteLine("欢迎使用ProxyStressTest项目. By:Lialy");
                Console.WriteLine("群号：526876433");
                Console.WriteLine("////////////////////////////////////////////////////");
                Console.WriteLine("正在初始化你的配置.");
                SetServerIP();
                Console.Write("服务器协议号:(0为自动获取)");
                ProtocolVersion = int.Parse(Console.ReadLine());
                Console.WriteLine("测试方案:");
                Console.WriteLine("1:(代理)集群机器人测试.");
                int Method = int.Parse(Console.ReadLine());
                ArrayList chat = new ArrayList();
                switch (Method)
                {
                    case 1:
                        chat.AddRange(File.ReadAllText(Setting.chatlist, Encoding.UTF8).Split('\n'));
                        StressTask s;
                        if (ProtocolVersion == 0)
                        {
                            s = new StressTask(ServerIP, ServerPort, Setting.name, Setting.threads, chat);
                        }
                        else
                        {
                            s = new StressTask(ServerIP, ServerPort, Setting.name, Setting.threads, chat, ProtocolVersion);
                        }
                        s.PrintServerInfo();
                        s.newTask(Setting.cooldown);
                        break;
                    case 2:
                        break;
                    default:
                        break;
                }
            }catch(Exception e)
            {
                Console.WriteLine("Error"+e.Message);
            }
        }
        public static bool SetServerIP()
        {
            Console.Write("服务器IP地址:");
            string server= Console.ReadLine();
            server = server.ToLower();
            string[] sip = server.Split(':');
            string host = sip[0];
            ushort port = 25565;

            if (sip.Length > 1)
            {
                try
                {
                    ServerIP = host;
                    ServerPort = Convert.ToUInt16(sip[1]);
                    return true;
                }
                catch (FormatException e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            if (host == "localhost" || host.Contains('.'))
            {
                //Server IP (IP or domain names contains at least a dot)
                if (sip.Length == 1 && host.Contains('.'))
                {       
                    //Domain name without port may need Minecraft SRV Record lookup
                    if(ProtocolHandler.MinecraftServiceLookup(ref host, ref port))
                    {
                        ServerIP = host;
                        ServerPort = port;
                    }
                    else
                    {
                        ServerIP = host;
                        Console.Write("服务器端口:");
                        ServerPort = int.Parse(Console.ReadLine());
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
