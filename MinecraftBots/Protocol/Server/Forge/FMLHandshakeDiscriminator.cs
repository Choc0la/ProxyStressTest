using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftBots.Protocol.Server.Forge
{
    enum FMLHandshakeDiscriminator : byte
    {
        ServerHello = 0,
        ClientHello = 1,
        ModList = 2,
        RegistryData = 3,
        HandshakeAck = 255, //-1
        HandshakeReset = 254, //-2
    }
}
