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
        public static string ServerIP;
        public static int ServerPort;

        public static void Start()
        {
            Console.WriteLine("欢迎使用ProxyStressTest-INSIDE项目ver.4.10. By:Lialy");
            Console.WriteLine("软件已开源;照成影响作者及开发成员不承担任何责任.");
            Console.WriteLine("项目地址(DEV)：https://github.com/MLinksme/ProxyStressTest");
            Console.WriteLine("群号：443098623");
            Console.WriteLine("////////////////////////////////////////////////////");
            Console.WriteLine("正在初始化你的配置.");
            ConsoleIO.StartPrintMsg();
            Init();
        }
        public static void Init()
        {
            try
            {
                SetServerIP();
                Console.WriteLine("测试方案:");
                Console.WriteLine("1:(代理)Proxy-Bots 并发测试.");
                Console.WriteLine("2:(代理)Proxy-Bots 队列算法测试.(不支持Motd)");
                int Method = int.Parse(Console.ReadLine());
                ArrayList chat = new ArrayList();
                ServerInfo info = new ServerInfo(ServerIP, ServerPort);
                if (info.StartGetServerInfo())
                {
                    int protocol = info.ProtocolVersion;
                    PrintServerInfo(info, ref protocol);
                    Helper.WriteLogs(info.ServerIP, info.ServerPort, info.GameVersion, Setting.threads);
                    switch (Method)
                    {
                        case 1:
                            chat.AddRange(File.ReadAllText(Setting.chatlist, Encoding.UTF8).Split('\n'));
                            Bot.tBotsTask_a s_a;
                            if (Setting.protocol == 0)
                            {
                                s_a = new Bot.tBotsTask_a(info, Setting.name, Setting.threads, chat,protocol);
                            }
                            else
                            {
                                s_a = new Bot.tBotsTask_a(info, Setting.name, Setting.threads, chat, Setting.protocol);
                            }
                            s_a.newTask(Setting.cooldown);
                            break;
                        case 2:
                            chat.AddRange(File.ReadAllText(Setting.chatlist, Encoding.UTF8).Split('\n'));
                            Bot.tBotsTask_b s_b;
                            if (Setting.protocol == 0)
                            {
                                s_b = new Bot.tBotsTask_b(info, Setting.name, Setting.threads, chat, protocol);
                            }
                            else
                            {
                                s_b = new Bot.tBotsTask_b(info, Setting.name, Setting.threads, chat, Setting.protocol);
                            }
                            s_b.newTask(Setting.cooldown);
                            break;
                        default:
                            Console.WriteLine("未提供相应方案，请重新选择");
                            Init();
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("取服务器信息失败，请重试..");
                    Program.Exit();
                }
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        internal static void PrintServerInfo(ServerInfo Info,ref int protocol)
        {
            ConsoleIO.AddMsgSeq("连接到:" + Info.ServerIP + ":" + Info.ServerPort, "INFO");
            ConsoleIO.AddMsgSeq("/////////////////////////////////////");
            ConsoleIO.AddMsgSeq("服务器MOTD:" + Info.MOTD);
            ConsoleIO.AddMsgSeq("已确定版本:" + Info.GameVersion + " Protocol:" + Info.ProtocolVersion, "INFO");
            ConsoleIO.AddMsgSeq("在线人数:" + Info.CurrentPlayerCount + "/" + Info.MaxPlayerCount, "INFO");
            string gamever = ProtocolHandler.getGameVersion(protocol);
            if (gamever == "")
            {
                ConsoleIO.AddMsgSeq("Unknown Version.", "INFO");
                ConsoleIO.AddMsgSeq("无法确定版本，请手动输入(1.7-1.13.1)");
                protocol = ProtocolHandler.MCVer2ProtocolVersion(Console.ReadLine());
            }
            else
                ConsoleIO.AddMsgSeq("运行版本:" + gamever, "INFO");
            Console.WriteLine("正在创建线程...");
        }
        public static bool SetServerIP(string init=null)
        {
            string server = String.Empty;
            if (init == null)
            {
                Console.Write("服务器IP地址:");
                server = Console.ReadLine();
            }
            else server = init;
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
