using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftBots.Protocol.Server.Forge
{
    enum FMLHandshakeClientState : byte
    {
        START,
        HELLO,
        WAITINGSERVERDATA,
        WAITINGSERVERCOMPLETE,
        PENDINGCOMPLETE,
        COMPLETE,
        DONE,
        ERROR
    }
}
