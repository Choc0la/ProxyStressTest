using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace MinecraftBots
{
    class Program
    {
        static void Main(string[] args)
        {
            Setting.LoadDefault();
            Content.Init();
        }
        private static void Debug()//仅用于调试程序
        {
            string host = "127.0.0.1";
            int port = 12345;
            string username = "%RANDOM%";
            int thread = 500;
            ArrayList chat = new ArrayList();
            chat.Add("仅供测试！");
            StressTask s = new StressTask(host, port, username, thread, chat);
            s.PrintServerInfo();
            s.newTask();
        }
        private static bool Encrypt(string filename)
        {
            if (File.Exists(filename))
            {
                string code=File.ReadAllText(filename);
                if (code == Setting.tok)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
