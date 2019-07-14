using System;
using System.Collections.Generic;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MinecraftBots.Bot;
using MinecraftBots.Protocol.Server;
using MinecraftBots.Protocol.Client.Handler;
using MinecraftBots.Net;
using MinecraftBots.Protocol.Server.Forge;
using MinecraftBots.Protocol.Client;
using Starksoft.Net.Proxy;
using System.Threading.Tasks;

namespace MinecraftBots.Bot
{
    class tBotsTask_a
    {
        int threads;
        public List<MinecraftBot> Bots = new List<MinecraftBot>();

        List<string> Chat;
        List<string> proxyip = new List<string>();
        string username;
        public bool TaskWorking = true;
        public static ServerInfo Info;
        public static int protocol;
        public tBotsTask_a(ServerInfo info, string player, int thr, List<string> chatlist, int v = 0)
        {
            Chat = chatlist;
            username = player;
            Info = info;
            threads = thr;
            if (v == 0)
                protocol = Info.ProtocolVersion;
            else
                protocol = v;
        }
        internal void newTask(int cooldown = 0)
        {
            MinecraftBot connection;
            try
            {
                while (TaskWorking)
                {
                    if(Bots.Count < threads)
                    {
                        if (proxyip.Count <= 0)
                        {
                            ConsoleIO.AddMsgSeq("正在获取代理...", "Proxy");
                            HttpReq.getips_(proxyip);
                        }
                        connection = new MinecraftBot(Info.ServerIP, Info.ServerPort, username, protocol, Info.ForgeInfo, this);
                        connection.Chatlist = Chat;
                        connection.AddPlayer();

                        Thread.Sleep(cooldown);
                    }
                    else
                        Clear();

                }
            }
            catch (Exception e)
            {
                ConsoleIO.AddMsgSeq(e.Message, "Error");
            }
        }
        internal string getSocks()
        {
            string socks = proxyip[0];
            proxyip.RemoveAt(0);
            return socks;
        }
        private void Clear()
        {
            Thread.Sleep(Setting.t_clear);
            int l = 0;
            int clear = 0;
            for (; l < Bots.Count; l++)
            {
                TimeSpan ts = DateTime.Now.Subtract(Bots[l].alivetime);
                if (ts.Seconds > 15)
                {
                    Bots[l].Dispose();
                    clear++;
                }
            }
            ConsoleIO.AddMsgSeq("清理线程数目:" + clear, "Thread");
            ConsoleIO.AddMsgSeq("当前运行线程:" + Bots.Count, "Thread");
            ConsoleIO.AddMsgSeq("代理数量:" + proxyip.Count, "Thread");
            GC.Collect();
        }
        public class MinecraftBot : IMinecraftCom,IDisposable
        {
            TcpClient Tcp;
            public DateTime alivetime = DateTime.Now;
            string ProxyIP;
            int ProxyPort=0;
            int ProtocolVersion;
            string Host;
            int Port;
            string playerID = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";
            string playername;
            ForgeInfo forgeInfo;
            MinecraftProtocol client;
            public List<string> Chatlist;
            tBotsTask_a bots;

            Thread mainThread;
            Thread tabComplete;
            internal MinecraftBot(string host, int port, string username, int protocolver, ForgeInfo forge, tBotsTask_a s)
            {
                ProtocolVersion = protocolver;
                Host = host;
                Port = port;
                forgeInfo = forge;
                bots = s;
                if (username != null)
                    playername = username.Replace("%RANDOM%", randomName());
            }
            internal void AddPlayer()
            {
                mainThread = new Thread(new ThreadStart(() =>
                  {
                      try
                      {
                          bots.Bots.Add(this);
                          if (Setting.sendmotd)
                          {
                              Tcp = getProxyClient();
                              SendMotd(Tcp);
                              Thread.Sleep(300);
                          }
                          Tcp = getProxyClient();
                          Tcp.ReceiveTimeout = 10000;
                          Tcp.SendTimeout = 10000;
                          client = new MinecraftProtocol(Tcp, ProtocolVersion, this, forgeInfo);
                          if (client.Login(Host, Port, playername))
                          {
                              if (Setting.t_tabcomplete > 0)
                                  TabComplete();
                              ConsoleIO.AddMsgSeq(playername + " Join the game", "Player");
                              client.StartUpdating(false);
                          }
                      }
                      catch
                      {
                          Dispose();
                      }
                  }));
                mainThread.Start();
            }
            public void OnGameJoin()
            {
                if(Setting.sendsetting)
                    client.SendClientSettings("en_US", 9, 0, 0, false, 65, 0);
                alivetime = DateTime.Now;
            }
            public void OnKeepAlive()
            { 
                foreach (string msg in Chatlist)
                {
                    client.SendChatMessage(msg);
                }
            }
            public void OnChat()
            {
                alivetime = DateTime.Now;
            }
            public void OnConnectionLost(BotUtils.DisconnectReason type, string msg)
            {
                if (type == BotUtils.DisconnectReason.InGameKick)
                {
                    ConsoleIO.AddMsgSeq(string.Format("{0}连接丢失:{1}",playername, Helper.ParseMsg(msg)), "Connection");
                    if (Setting.t_rejoin > 0)
                        ReJoin();
                    else
                        Dispose();
                }
                else if (type == BotUtils.DisconnectReason.LoginRejected)
                {
                    ConsoleIO.AddMsgSeq(string.Format("{0}拒绝连接:{1}", playername, Helper.ParseMsg(msg)), "Connection");
                    if (Setting.t_rejoin > 0)
                        ReJoin();
                    else
                        Dispose();
                }
                else if (type == BotUtils.DisconnectReason.ConnectionLost)
                {
                    Dispose();
                }
            }

            /// <summary>
            /// 防止被Clear清理掉
            /// </summary>
            private void ReJoin()
            {
                if (Setting.t_rejoin < 1000)
                {
                    Thread.Sleep(Setting.t_rejoin);
                    alivetime = DateTime.Now;
                }
                else
                {
                    int sec= Setting.t_rejoin / 1000;
                    for(int i = 0; i < sec; i++)
                    {
                        Thread.Sleep(1000);
                        alivetime = DateTime.Now;
                    }
                }
                AddPlayer();
            }
            public void SendMotd(TcpClient tcp)
            {
                byte[] packet_id = ProtocolHandler.getVarInt(0);
                byte[] protocol_version = ProtocolHandler.getVarInt(-1);
                byte[] server_adress_val = Encoding.UTF8.GetBytes(Host);
                byte[] server_adress_len = ProtocolHandler.getVarInt(server_adress_val.Length);
                byte[] server_port = BitConverter.GetBytes((ushort)Port); Array.Reverse(server_port);
                byte[] next_state = ProtocolHandler.getVarInt(1);
                byte[] packet2 = ProtocolHandler.concatBytes(packet_id, protocol_version, server_adress_len, server_adress_val, server_port, next_state);
                byte[] tosend = ProtocolHandler.concatBytes(ProtocolHandler.getVarInt(packet2.Length), packet2);

                tcp.Client.Send(tosend, SocketFlags.None);

                byte[] status_request = ProtocolHandler.getVarInt(0);
                byte[] request_packet = ProtocolHandler.concatBytes(ProtocolHandler.getVarInt(status_request.Length), status_request);

                tcp.Client.Send(request_packet, SocketFlags.None);

                ProtocolHandler handler = new ProtocolHandler(tcp);
                int packetLength = handler.readNextVarIntRAW();
            }
            private TcpClient getProxyClient()
            {
                if (ProxyPort == 0)
                {
                    string[] proxyip = bots.getSocks().Split(':');
                    if (proxyip.Length == 2)
                    {
                        ProxyIP = proxyip[0];
                        ProxyPort = int.Parse(proxyip[1]);
                    }
                    else
                    {
                        ProxyIP = proxyip[0];
                        ProxyPort = 8080;
                    }
                }
                ProxyClientFactory factory = new ProxyClientFactory();
                IProxyClient proxy = factory.CreateProxyClient(ProxyType.Http, ProxyIP, ProxyPort);
                return proxy.CreateConnection(Host, Port);
            }
            private void TabComplete()
            {
                tabComplete = new Thread(new ThreadStart(() =>
                {
                    while (Tcp != null && Tcp.Connected)
                    {
                        Thread.Sleep(Setting.t_tabcomplete);
                        client.AutoTabComplete("/");
                    }
                }));
                tabComplete.IsBackground = true;
                tabComplete.Start();
            }
            private string randomName()
            {
                Random random = new Random();
                int name_len = random.Next(5, 15);
                string id = "";
                for (int l = 0; l < name_len; l++)
                {
                    id += playerID[random.Next(0, playerID.Length - 1)];
                }
                return id;
            }

            public void Dispose()
            {
                mainThread.Abort();
                if (client != null)
                    client.Dispose();
                else
                {
                    if (Tcp != null)
                        Tcp.Close();
                }
                Tcp = null;
                if(tabComplete != null)
                    tabComplete.Abort();
                bots.Bots.Remove(this);
                //GC.SuppressFinalize(this);
            }
        }
    }
}
