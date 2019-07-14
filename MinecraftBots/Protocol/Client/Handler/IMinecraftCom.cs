using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftBots.Protocol.Client.Handler
{
    public interface IMinecraftCom
    {
        void OnGameJoin();
        void OnConnectionLost(BotUtils.DisconnectReason type,string msg);
        void OnKeepAlive();
        void OnChat();
    }
}
