using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using MinecraftBots.Protocol.Client.Handler;
using MinecraftBots.Protocol.Server.Forge;
using System.Net.Sockets;
using MinecraftBots.Protocol.Server;
using System.Net.NetworkInformation;

namespace MinecraftBots.Bot
{
    class MinecraftBot:MinecraftEvent
    {
        TcpClient Tcp;
        public DateTime alivetime= DateTime.Now;
        string ProxyIP;
        int ProxyPort;
        int ProtocolVersion;
        string Host;
        int Port;
        string playerID = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";
        string playername;
        ForgeInfo forgeInfo;
        MinecraftProtocol client;
        public ArrayList Chatlist;
        StressTask bots;

        Thread clientthread;
        Thread tabcopmletethread;
        Thread chatthread;
        public long Ping { get; private set; }
        internal MinecraftBot(string host,int port,string username,int protocolver,ForgeInfo forge,string socks, StressTask s)
        {
            string[] proxyip = socks.Split(':');
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
            ProtocolVersion = protocolver;
            Host = host;
            Port = port;
            forgeInfo = forge;
            bots = s;
            playername=username.Replace("%RANDOM%", randomName());
        }
        internal void AddPlayer()
        {
            clientthread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    bool motdview = true;
                    if (Setting.MotdSend)
                    {
                        Tcp = new TcpClient();
                        Tcp.Connect(ProxyIP, ProxyPort);
                        if (proxyip().Contains("200"))
                        {
                            SendMotd(Tcp);
                        }
                        else
                        {
                            motdview = false;
                        }
                    }
                    Tcp = new TcpClient();
                    Tcp.Connect(ProxyIP, ProxyPort);
                    if (motdview)
                    {
                        if (proxyip().Contains("200"))
                        {
                            client = new MinecraftProtocol(Tcp, ProtocolVersion, this, forgeInfo);
                            client.Handshake(Host, Port);
                            if (client.Login(playername))
                            {
                                if (Setting.TabComplete)
                                {
                                    TabComplete();
                                }
                            }
                        }
                        else
                        {
                            Stop();
                            bots.Bots.Remove(this);
                        }
                    }
                }
                catch {
                    Stop();
                    bots.Bots.Remove(this);
                }
            }));
            clientthread.IsBackground = true;
            clientthread.Start();
        }
        public void OnGameJoin()
        {
            Console.WriteLine("[Player] " + playername + " Join the game");
            alivetime = DateTime.Now;
            chatthread = new Thread(new ThreadStart(() =>
            {
                while (Tcp != null && Tcp.Connected)
                {
                    for (int l = 0; l < Chatlist.Count; l++)
                    {
                        Thread.Sleep(2000);
                        client.SendChatMessage(Chatlist[l].ToString());
                    }
                }
            }));
            chatthread.IsBackground = true;
            chatthread.Start();
        }

        public void OnTextReceived(string msg)
        {
            alivetime = DateTime.Now;
        }
        public void OnConnectionLost(int type,string msg)
        {
            if (type == 2)
            {
                Console.WriteLine("[Connection] 连接丢失: " + msg);
                if (Setting.ReJoin)
                {
                    Thread.Sleep(2000);
                    AddPlayer();
                }
                else
                {
                    Stop();
                    bots.Bots.Remove(this);
                }  
            }else if (type == 1)
            {
                Console.WriteLine("[Connection] " + playername + " " + msg);
                if (Setting.ReJoin)
                {
                    Thread.Sleep(2000);
                    AddPlayer();
                }
                else
                {
                    Stop();
                    bots.Bots.Remove(this);
                }
            }else if(type == 0)
            {
                Stop();
                bots.Bots.Remove(this);
            }
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
            ReloadPing();
        }
        public void ReloadPing()
        {
            using (Ping pinger = new Ping())
            {
                var pingResulr = pinger.Send(Host);
                if (pingResulr.Status == IPStatus.Success)
                {
                    this.Ping = pingResulr.RoundtripTime;
                }
            }
        }
        private string proxyip()
        {
                Tcp.ReceiveTimeout = 20000;
                string requeststr = string.Format("CONNECT " + Host + ":" + Port + " HTTP/1.1\nHost: " + Host + ":" + Port + "\nProxy-Connection: keep-alive\n\n");
                if (Tcp.Connected)
                {

                    byte[] request = Encoding.UTF8.GetBytes(requeststr);
                    NetworkStream stream = Tcp.GetStream();
                    Tcp.Client.Send(request);
                    byte[] response = new byte[1024];
                    int l = stream.Read(response, 0, response.Length);
                    string read = Encoding.Default.GetString(response, 0, l);
                    return read;

                }
                return "";
        }
        public void TabComplete()
        {
            tabcopmletethread = new Thread(new ThreadStart(() =>
              {
                  while (Tcp.Connected)
                  {
                      Thread.Sleep(200);
                      client.AutoTabComplete("/");
                  }
              }));
            tabcopmletethread.IsBackground = true;
            tabcopmletethread.Start();
        }
        internal string randomName()
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
        public void Stop()
        {
            try
            {
                if (client != null)
                {
                    client.Dispose();
                }
                else
                {
                    Tcp.Close();
                }
                if (clientthread != null)
                {
                    clientthread.Abort();
                }
                if (chatthread != null)
                {
                    chatthread.Abort();
                }
                if (tabcopmletethread != null)
                {
                    tabcopmletethread.Abort();
                }    
            }
            catch { }
        }
    }
}
