using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using MinecraftBots.Protocol.Server;
using System.Collections.Generic;

namespace MinecraftBots
{
    class Program
    {
        static void Main(string[] args)
        {
            Setting.LoadDefault();
            if (args.Length > 0)
            {

                string host = String.Empty;
                foreach (string param in args)
                {
                    if (param.StartsWith("host:"))
                        host = param.Replace("host:", "");
                }
                List<string> chat = new List<string>();
                chat.AddRange(File.ReadAllText(Setting.chatlist, Encoding.UTF8).Split('\n'));
                Content.SetServerIP(host);
                ServerInfo info = new ServerInfo(Content.ServerIP, Content.ServerPort);
                if (info.StartGetServerInfo())
                {
                    Bot.tBotsTask_a s = new Bot.tBotsTask_a(info, Setting.name, Setting.threads, chat);
                    s.newTask();
                }
            }
            else
                Content.Start();
        }
        private static void Debug()//仅用于调试程序
        {
            string host = "127.0.0.1";
            int port = 25565;
            string username = "%RANDOM%";
            int thread = 500;
            List<string> chat = new List<string>();
            chat.Add("仅供测试！");
            ServerInfo info = new ServerInfo(host, port);
            if (info.StartGetServerInfo())
            {
                Bot.tBotsTask_a s = new Bot.tBotsTask_a(info, username, thread, chat);
                s.newTask();
            }
        }

        public static void Exit()
        {
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
