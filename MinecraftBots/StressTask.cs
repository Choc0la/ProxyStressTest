using System;
using System.Collections.Generic;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MinecraftBots.Bot;
using MinecraftBots.Protocol.Server;
using MinecraftBots.Net;

namespace MinecraftBots
{
    class StressTask
    {
        int threads;
        public string serverhost;
        public int serverport;
        public List<MinecraftBot> Bots=new List<MinecraftBot>();

        ArrayList Chat;
        ArrayList proxyip=new ArrayList();
        string username;
        public bool TaskWorking = true;
        public static ServerInfo Info;
        public static int protocol;
        internal StressTask(string ip,int port,string player,int thr,ArrayList chatlist,int v=0)
        {
            serverhost = ip;
            serverport = port;
            Chat = chatlist;
            username = player;
            Info = new ServerInfo(ip, port);
            if (Info.StartGetServerInfo())
            {

                HttpReq.getips(proxyip);
                if (thr > proxyip.Count)
                {
                    threads = proxyip.Count;
                }
                else
                {
                    threads = thr;
                }
                if (v == 0)
                {
                    protocol = Info.ProtocolVersion;
                }
                else
                {
                    protocol = v;
                }
            }
            else
            {
                Print("无法取到服务器信息.", "WARN");
                Console.ReadKey();
            }
        }
        internal void PrintServerInfo()
        {
            Print("连接到:" + Info.ServerIP+":"+ Info.ServerPort, "INFO");
            Print("/////////////////////////////////////");
            Print("服务器MOTD:" + Info.MOTD, "INFO");
            Print("已确定版本:" + Info.GameVersion+ " Protocol:"+Info.ProtocolVersion, "INFO");
            Print("在线人数:" + Info.CurrentPlayerCount+"/"+ Info.MaxPlayerCount, "INFO");
        }
        internal void newTask(int cooldown=0)
        {
            Thread thr1 = new Thread(Clear);
            thr1.Start();
            MinecraftBot client;
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    while (TaskWorking)
                    {
                        while (Bots.Count < threads)
                        {
                            if (proxyip.Count > 0)
                            {
                                client = new MinecraftBot(serverhost, serverport, username, protocol, Info.ForgeInfo, proxyip[0].ToString(), this);
                                Bots.Add(client);
                                client.Chatlist = Chat;
                                client.AddPlayer();
                                proxyip.RemoveAt(0);
                                Thread.Sleep(cooldown);
                            }
                        }
                        Thread.Sleep(3000);
                    }
                    Bots.Clear();
                }
                catch(Exception e)
                {
                    Print(e.Message, "Error");
                }
            })).Start();
        }

        public static void Print(string tex, string title = null)
        {
            if (title != null)
            {
                Console.WriteLine("[" + title + "] " + tex);
            }
            else
            {
                Console.WriteLine(tex);
            }
        }
        internal void Clear()
        {
            while (TaskWorking)
            {
                Thread.Sleep(5000);
                int l = 0;
                int clear = 0;
                for (; l < Bots.Count; l++)
                {
                    TimeSpan ts = DateTime.Now.Subtract(Bots[l].alivetime);
                    if (ts.Seconds>15)
                    {
                        Bots[l].Stop();
                        Bots.Remove(Bots[l]);
                        clear++;
                    }
                }
                Print("清理线程数目:" + clear, "Thread");
                Print("当前运行线程:" + Bots.Count, "Thread");
                Print("代理数量:" + proxyip.Count, "Thread");
                GC.Collect();
                HttpReq.getips(proxyip);
            }
        }
    }
}
