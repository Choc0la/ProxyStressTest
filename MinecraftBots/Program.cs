using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using MinecraftBots.Protocol.Server;

namespace MinecraftBots
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Net.HttpReq.DoHttpRequest(Setting.post_url, "tok=" + Setting.tok))
            {
                Setting.LoadDefault();
                if (args.Length > 0)
                {

                    string host=String.Empty;
                    foreach (string param in args)
                    {
                        if (param.StartsWith("host:"))
                            host = param.Replace("host:", "");
                    }
                    ArrayList chat = new ArrayList();
                    chat.AddRange(File.ReadAllText(Setting.chatlist, Encoding.UTF8).Split('\n'));
                    Content.SetServerIP(host);
                    ServerInfo info = new ServerInfo(Content.ServerIP, Content.ServerPort);
                    if (info.StartGetServerInfo())
                    {
                        Bot.tBotsTask_a s = new Bot.tBotsTask_a(info, Setting.name, Setting.threads,chat);
                        s.newTask();
                    }
                }
                else
                    Content.Start();
            }
            else
            {
                Console.WriteLine("无效请求;");
                Exit();
            }
            //Console.WriteLine("输入你的USERID:");
            //switch (Net.HttpReq.CheckUser(Console.ReadLine()))
            //{
            //    case 0:
            //        Console.Clear();
            //        Setting.LoadDefault();
            //        Content.Start();
            //        break;
            //    case 1:
            //        Console.WriteLine("不存在的UserID;");
            //        Console.WriteLine("可提交申请于(群:443098623)以获得userid");
            //        break;
            //    case 2:
            //        Console.WriteLine("你被拒绝使用此版本;");
            //        break;
            //    default:
            //        Console.WriteLine("服务器错误;");
            //        Console.WriteLine("请稍后重试;");
            //        break;
            //}
        }
        private static void Debug()//仅用于调试程序
        {
            string host = "127.0.0.1";
            int port = 25565;
            string username = "%RANDOM%";
            int thread = 500;
            ArrayList chat = new ArrayList();
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
