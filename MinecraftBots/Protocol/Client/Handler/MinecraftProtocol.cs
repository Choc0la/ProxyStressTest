﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using MinecraftBots.Protocol.Server.Forge;
using MinecraftBots.Net;

namespace MinecraftBots.Protocol.Client.Handler
{
    class MinecraftProtocol
    {
        ForgeInfo forgeInfo;
        TcpClient client;
        MinecraftEvent handler;
        private FMLHandshakeClientState fmlHandshakeState = FMLHandshakeClientState.START;
        private int compression_treshold = 0;
        private int protocolversion;
        private bool login_phase = true;
        int currentDimension=-1;

        private const int MC18Version = 47;
        private const int MC19Version = 107;
        private const int MC191Version = 108;
        private const int MC110Version = 210;
        private const int MC111Version = 315;
        private const int MC17w13aVersion = 318;
        private const int MC112pre5Version = 332;
        private const int MC17w31aVersion = 336;
        private const int MC17w45aVersion = 343;
        private const int MC17w46aVersion = 345;
        private const int MC17w47aVersion = 346;
        private const int MC18w01aVersion = 352;
        private const int MC18w06aVersion = 357;
        private const int MC113pre4Version = 386;
        private const int MC113pre7Version = 389;
        private const int MC113Version = 393;

        private int autocomplete_transaction_id = 0;

        Thread netRead;
        public MinecraftProtocol(TcpClient tcp, int protocolver,MinecraftEvent Handler,ForgeInfo forge)
        {
            client = tcp;
            protocolversion = protocolver;
            forgeInfo = forge;
            handler = Handler;
        }
        private void Receive(byte[] buffer, int start, int offset, SocketFlags f)
        {
            int read = 0;
            while (read < offset)
            {
                int len = client.Client.Receive(buffer, start + read, offset - read, f);
                if (len != 0)
                    read += len;
                else
                    handler.OnConnectionLost(BotUtils.DisconnectReason.ConnectionLost, "Connection Close.");
            }
        }
        private bool CompleteForgeHandshake()
        {
            int packetID = -1;
            List<byte> packetData = new List<byte>();

            while (fmlHandshakeState != FMLHandshakeClientState.DONE)
            {
                readNextPacket(ref packetID, packetData);

                if (packetID == 0x40) // Disconnect
                {
                    ConsoleIO.AddMsgSeq("[FML] Connection Lost.");
                    return false;
                }
                else
                {
                    handlePacket(packetID, packetData);
                }
            }

            return true;
        }

        private bool handlePacket(int packetID, List<byte> packetData)
        {
            if (login_phase)
            {
                switch (packetID) //Packet IDs are different while logging in
                {
                    case 0x03:
                        if (protocolversion >= MC18Version)
                            compression_treshold = readNextVarInt(packetData);
                        break;
                    default:
                        return false; //Ignored packet
                }
            }
            switch (getPacketIncomingType(packetID))
            {
                case PacketIncomingType.KeepAlive:
                    SendPacket(PacketOutgoingType.KeepAlive, packetData);
                    handler.OnKeepAlive();
                    break;
                case PacketIncomingType.JoinGame:
                    handler.OnGameJoin();
                    readNextInt(packetData);
                    readNextByte(packetData);
                    if (protocolversion >= MC191Version)
                        this.currentDimension = readNextInt(packetData);
                    else
                        this.currentDimension = (sbyte)readNextByte(packetData);
                    readNextByte(packetData);
                    readNextByte(packetData);
                    readNextString(packetData);
                    if (protocolversion >= MC18Version)
                        readNextBool(packetData);  // Reduced debug info - 1.8 and above
                    break;
                case PacketIncomingType.ChatMessage:
                    //string message = readNextString(packetData);
                    break;
                case PacketIncomingType.Respawn:
                    this.currentDimension = readNextInt(packetData);
                    readNextByte(packetData);
                    readNextByte(packetData);
                    readNextString(packetData);
                    break;
                case PacketIncomingType.PlayerPositionAndLook:
                    if (protocolversion >= MC19Version)
                    {
                        int teleportID = readNextVarInt(packetData);
                        // Teleport confirm packet
                        SendPacket(PacketOutgoingType.TeleportConfirm, getVarInt(teleportID));
                    }
                    break;
                case PacketIncomingType.ChunkData:
                    break;
                case PacketIncomingType.MultiBlockChange:
                    break;
                case PacketIncomingType.BlockChange:
                    break;
                case PacketIncomingType.TabCompleteResult:
                    if (protocolversion >= MC17w46aVersion)
                    {
                        autocomplete_transaction_id = readNextVarInt(packetData);
                    }
                    if (protocolversion >= MC17w47aVersion)
                    {
                        // Start of the text to replace - currently unused
                        readNextVarInt(packetData);
                    }

                    if (protocolversion >= MC18w06aVersion)
                    {
                        // Length of the text to replace - currently unused
                        readNextVarInt(packetData);
                    }
                    int autocomplete_count = readNextVarInt(packetData);
                    break;
                case PacketIncomingType.MapChunkBulk:
                    break;
                case PacketIncomingType.UnloadChunk:
                    break;
                case PacketIncomingType.PlayerListUpdate:
                    break;
                case PacketIncomingType.PluginMessage:
                    String channel = readNextString(packetData);
                    if (protocolversion < MC18Version)
                    {
                        if (forgeInfo == null)
                        {
                            // 1.7 and lower prefix plugin channel packets with the length.
                            // We can skip it, though.
                            readNextShort(packetData);
                        }
                        else
                        {
                            // Forge does something even weirder with the length.
                            readNextVarShort(packetData);
                        }
                    }

                    // The remaining data in the array is the entire payload of the packet.
                    //handler.OnPluginChannelMessage(channel, packetData.ToArray());

                    #region Forge Login
                    if (forgeInfo != null && fmlHandshakeState != FMLHandshakeClientState.DONE)
                    {
                        if (channel == "FML|HS")
                        {
                            FMLHandshakeDiscriminator discriminator = (FMLHandshakeDiscriminator)readNextByte(packetData);

                            if (discriminator == FMLHandshakeDiscriminator.HandshakeReset)
                            {
                                fmlHandshakeState = FMLHandshakeClientState.START;
                                return true;
                            }

                            switch (fmlHandshakeState)
                            {
                                case FMLHandshakeClientState.START:
                                    if (discriminator != FMLHandshakeDiscriminator.ServerHello)
                                        return false;

                                    // Send the plugin channel registration.
                                    // REGISTER is somewhat special in that it doesn't actually include length information,
                                    // and is also \0-separated.
                                    // Also, yes, "FML" is there twice.  Don't ask me why, but that's the way forge does it.
                                    string[] channels = { "FML|HS", "FML", "FML|MP", "FML", "FORGE" };
                                    SendPluginChannelPacket("REGISTER", Encoding.UTF8.GetBytes(string.Join("\0", channels)));

                                    byte fmlProtocolVersion = readNextByte(packetData);

                                    if (fmlProtocolVersion >= 1)
                                        this.currentDimension = readNextInt(packetData);

                                    // Tell the server we're running the same version.
                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.ClientHello, new byte[] { fmlProtocolVersion });

                                    // Then tell the server that we're running the same mods.
                                    byte[][] mods = new byte[forgeInfo.Mods.Count][];
                                    for (int i = 0; i < forgeInfo.Mods.Count; i++)
                                    {
                                        ForgeInfo.ForgeMod mod = forgeInfo.Mods[i];
                                        mods[i] = concatBytes(getString(mod.ModID), getString(mod.Version));
                                    }
                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.ModList,
                                        concatBytes(getVarInt(forgeInfo.Mods.Count), concatBytes(mods)));

                                    fmlHandshakeState = FMLHandshakeClientState.WAITINGSERVERDATA;

                                    return true;
                                case FMLHandshakeClientState.WAITINGSERVERDATA:
                                    if (discriminator != FMLHandshakeDiscriminator.ModList)
                                        return false;

                                    Thread.Sleep(2000);

                                    // Tell the server that yes, we are OK with the mods it has
                                    // even though we don't actually care what mods it has.

                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.HandshakeAck,
                                        new byte[] { (byte)FMLHandshakeClientState.WAITINGSERVERDATA });

                                    fmlHandshakeState = FMLHandshakeClientState.WAITINGSERVERCOMPLETE;
                                    return false;
                                case FMLHandshakeClientState.WAITINGSERVERCOMPLETE:
                                    // The server now will tell us a bunch of registry information.
                                    // We need to read it all, though, until it says that there is no more.
                                    if (discriminator != FMLHandshakeDiscriminator.RegistryData)
                                        return false;

                                    if (protocolversion < MC18Version)
                                    {
                                        // 1.7.10 and below have one registry
                                        // with blocks and items.
                                        int registrySize = readNextVarInt(packetData);

                                        fmlHandshakeState = FMLHandshakeClientState.PENDINGCOMPLETE;
                                    }
                                    else
                                    {
                                        // 1.8+ has more than one registry.

                                        bool hasNextRegistry = readNextBool(packetData);
                                        string registryName = readNextString(packetData);
                                        int registrySize = readNextVarInt(packetData);
                                        if (!hasNextRegistry)
                                            fmlHandshakeState = FMLHandshakeClientState.PENDINGCOMPLETE;
                                    }

                                    return false;
                                case FMLHandshakeClientState.PENDINGCOMPLETE:
                                    // The server will ask us to accept the registries.
                                    // Just say yes.
                                    if (discriminator != FMLHandshakeDiscriminator.HandshakeAck)
                                        return false;
                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.HandshakeAck,
                                        new byte[] { (byte)FMLHandshakeClientState.PENDINGCOMPLETE });
                                    fmlHandshakeState = FMLHandshakeClientState.COMPLETE;

                                    return true;
                                case FMLHandshakeClientState.COMPLETE:
                                    // One final "OK".  On the actual forge source, a packet is sent from
                                    // the client to the client saying that the connection was complete, but
                                    // we don't need to do that.

                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.HandshakeAck,
                                        new byte[] { (byte)FMLHandshakeClientState.COMPLETE });
                                    fmlHandshakeState = FMLHandshakeClientState.DONE;
                                    return true;
                            }
                        }
                    }
                    #endregion
                    return false;
                case PacketIncomingType.KickPacket:
                    handler.OnConnectionLost(BotUtils.DisconnectReason.InGameKick, readNextString(packetData));
                    return false;
                case PacketIncomingType.NetworkCompressionTreshold:
                    if (protocolversion >= MC18Version && protocolversion < MC19Version)
                        compression_treshold = readNextVarInt(packetData);
                    break;
                case PacketIncomingType.ResourcePackSend:
                    string url = readNextString(packetData);
                    string hash = readNextString(packetData);
                    //Send back "accepted" and "successfully loaded" responses for plugins making use of resource pack mandatory
                    byte[] responseHeader = new byte[0];
                    if (protocolversion < MC110Version) //MC 1.10 does not include resource pack hash in responses
                        responseHeader = concatBytes(getVarInt(hash.Length), Encoding.UTF8.GetBytes(hash));
                    SendPacket(PacketOutgoingType.ResourcePackStatus, concatBytes(responseHeader, getVarInt(3))); //Accepted pack
                    SendPacket(PacketOutgoingType.ResourcePackStatus, concatBytes(responseHeader, getVarInt(0))); //Successfully loaded
                    break;
                default:
                    return false; //Ignored packet
            }
            return true; //Packet processed
        }
        private enum PacketOutgoingType
        {
            KeepAlive,
            ResourcePackStatus,
            ChatMessage,
            ClientStatus,
            ClientSettings,
            PluginMessage,
            TabComplete,
            PlayerPosition,
            PlayerPositionAndLook,
            TeleportConfirm
        }
        private enum PacketIncomingType
        {
            KeepAlive,
            JoinGame,
            ChatMessage,
            Respawn,
            PlayerPositionAndLook,
            ChunkData,
            MultiBlockChange,
            BlockChange,
            MapChunkBulk,
            UnloadChunk,
            PlayerListUpdate,
            TabCompleteResult,
            PluginMessage,
            KickPacket,
            NetworkCompressionTreshold,
            ResourcePackSend,
            UnknownPacket
        }
        private PacketIncomingType getPacketIncomingType(int packetID)
        {
            if (protocolversion < MC19Version)
            {
                switch (packetID)
                {
                    case 0x00: return PacketIncomingType.KeepAlive;
                    case 0x01: return PacketIncomingType.JoinGame;
                    case 0x02: return PacketIncomingType.ChatMessage;
                    case 0x07: return PacketIncomingType.Respawn;
                    case 0x08: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x21: return PacketIncomingType.ChunkData;
                    case 0x22: return PacketIncomingType.MultiBlockChange;
                    case 0x23: return PacketIncomingType.BlockChange;
                    case 0x26: return PacketIncomingType.MapChunkBulk;
                    //UnloadChunk does not exists prior to 1.9
                    case 0x38: return PacketIncomingType.PlayerListUpdate;
                    case 0x3A: return PacketIncomingType.TabCompleteResult;
                    case 0x3F: return PacketIncomingType.PluginMessage;
                    case 0x40: return PacketIncomingType.KickPacket;
                    case 0x46: return PacketIncomingType.NetworkCompressionTreshold;
                    case 0x48: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocolversion < MC17w13aVersion)
            {
                switch (packetID)
                {
                    case 0x1F: return PacketIncomingType.KeepAlive;
                    case 0x23: return PacketIncomingType.JoinGame;
                    case 0x0F: return PacketIncomingType.ChatMessage;
                    case 0x33: return PacketIncomingType.Respawn;
                    case 0x2E: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x20: return PacketIncomingType.ChunkData;
                    case 0x10: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2D: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x32: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocolversion < MC112pre5Version)
            {
                switch (packetID)
                {
                    case 0x20: return PacketIncomingType.KeepAlive;
                    case 0x24: return PacketIncomingType.JoinGame;
                    case 0x10: return PacketIncomingType.ChatMessage;
                    case 0x35: return PacketIncomingType.Respawn;
                    case 0x2F: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x21: return PacketIncomingType.ChunkData;
                    case 0x11: return PacketIncomingType.MultiBlockChange;
                    case 0x0C: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1E: return PacketIncomingType.UnloadChunk;
                    case 0x2E: return PacketIncomingType.PlayerListUpdate;
                    case 0x0F: return PacketIncomingType.TabCompleteResult;
                    case 0x19: return PacketIncomingType.PluginMessage;
                    case 0x1B: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x34: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocolversion < MC17w31aVersion)
            {
                switch (packetID)
                {
                    case 0x1F: return PacketIncomingType.KeepAlive;
                    case 0x23: return PacketIncomingType.JoinGame;
                    case 0x0F: return PacketIncomingType.ChatMessage;
                    case 0x34: return PacketIncomingType.Respawn;
                    case 0x2E: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x20: return PacketIncomingType.ChunkData;
                    case 0x10: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2D: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x33: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocolversion < MC17w45aVersion)
            {
                switch (packetID)
                {
                    case 0x1F: return PacketIncomingType.KeepAlive;
                    case 0x23: return PacketIncomingType.JoinGame;
                    case 0x0F: return PacketIncomingType.ChatMessage;
                    case 0x35: return PacketIncomingType.Respawn;
                    case 0x2F: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x20: return PacketIncomingType.ChunkData;
                    case 0x10: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2E: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x34: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocolversion < MC17w46aVersion)
            {
                switch (packetID)
                {
                    case 0x1F: return PacketIncomingType.KeepAlive;
                    case 0x23: return PacketIncomingType.JoinGame;
                    case 0x0E: return PacketIncomingType.ChatMessage;
                    case 0x35: return PacketIncomingType.Respawn;
                    case 0x2F: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x21: return PacketIncomingType.ChunkData;
                    case 0x0F: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2E: return PacketIncomingType.PlayerListUpdate;
                    //TabCompleteResult accidentely removed
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x34: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocolversion < MC18w01aVersion)
            {
                switch (packetID)
                {
                    case 0x20: return PacketIncomingType.KeepAlive;
                    case 0x24: return PacketIncomingType.JoinGame;
                    case 0x0E: return PacketIncomingType.ChatMessage;
                    case 0x36: return PacketIncomingType.Respawn;
                    case 0x30: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x21: return PacketIncomingType.ChunkData;
                    case 0x0F: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1E: return PacketIncomingType.UnloadChunk;
                    case 0x2F: return PacketIncomingType.PlayerListUpdate;
                    case 0x10: return PacketIncomingType.TabCompleteResult;
                    case 0x19: return PacketIncomingType.PluginMessage;
                    case 0x1B: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x35: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocolversion < MC113pre7Version)
            {
                switch (packetID)
                {
                    case 0x20: return PacketIncomingType.KeepAlive;
                    case 0x24: return PacketIncomingType.JoinGame;
                    case 0x0E: return PacketIncomingType.ChatMessage;
                    case 0x37: return PacketIncomingType.Respawn;
                    case 0x31: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x21: return PacketIncomingType.ChunkData;
                    case 0x0F: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1E: return PacketIncomingType.UnloadChunk;
                    case 0x2F: return PacketIncomingType.PlayerListUpdate;
                    case 0x10: return PacketIncomingType.TabCompleteResult;
                    case 0x19: return PacketIncomingType.PluginMessage;
                    case 0x1B: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x36: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else
            {
                switch (packetID)
                {
                    case 0x21: return PacketIncomingType.KeepAlive;
                    case 0x25: return PacketIncomingType.JoinGame;
                    case 0x0E: return PacketIncomingType.ChatMessage;
                    case 0x38: return PacketIncomingType.Respawn;
                    case 0x32: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x22: return PacketIncomingType.ChunkData;
                    case 0x0F: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1F: return PacketIncomingType.UnloadChunk;
                    case 0x30: return PacketIncomingType.PlayerListUpdate;
                    case 0x10: return PacketIncomingType.TabCompleteResult;
                    case 0x19: return PacketIncomingType.PluginMessage;
                    case 0x1B: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x37: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
        }

        private int getPacketOutgoingID(PacketOutgoingType packet, int protocol)
        {
            if (protocol < MC19Version)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x00;
                    case PacketOutgoingType.ResourcePackStatus: return 0x19;
                    case PacketOutgoingType.ChatMessage: return 0x01;
                    case PacketOutgoingType.ClientStatus: return 0x16;
                    case PacketOutgoingType.ClientSettings: return 0x15;
                    case PacketOutgoingType.PluginMessage: return 0x17;
                    case PacketOutgoingType.TabComplete: return 0x14;
                    case PacketOutgoingType.PlayerPosition: return 0x04;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x06;
                    case PacketOutgoingType.TeleportConfirm: throw new InvalidOperationException("Teleport confirm is not supported in protocol " + protocol);
                }
            }
            else if (protocol < MC17w13aVersion)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0B;
                    case PacketOutgoingType.ResourcePackStatus: return 0x16;
                    case PacketOutgoingType.ChatMessage: return 0x02;
                    case PacketOutgoingType.ClientStatus: return 0x03;
                    case PacketOutgoingType.ClientSettings: return 0x04;
                    case PacketOutgoingType.PluginMessage: return 0x09;
                    case PacketOutgoingType.TabComplete: return 0x01;
                    case PacketOutgoingType.PlayerPosition: return 0x0C;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0D;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else if (protocolversion < MC112pre5Version)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0C;
                    case PacketOutgoingType.ResourcePackStatus: return 0x17;
                    case PacketOutgoingType.ChatMessage: return 0x03;
                    case PacketOutgoingType.ClientStatus: return 0x04;
                    case PacketOutgoingType.ClientSettings: return 0x05;
                    case PacketOutgoingType.PluginMessage: return 0x0A;
                    case PacketOutgoingType.TabComplete: return 0x02;
                    case PacketOutgoingType.PlayerPosition: return 0x0D;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0E;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else if (protocol < MC17w31aVersion)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0C;
                    case PacketOutgoingType.ResourcePackStatus: return 0x17;
                    case PacketOutgoingType.ChatMessage: return 0x03;
                    case PacketOutgoingType.ClientStatus: return 0x04;
                    case PacketOutgoingType.ClientSettings: return 0x05;
                    case PacketOutgoingType.PluginMessage: return 0x0A;
                    case PacketOutgoingType.TabComplete: return 0x02;
                    case PacketOutgoingType.PlayerPosition: return 0x0E;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0F;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else if (protocol < MC17w45aVersion)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0B;
                    case PacketOutgoingType.ResourcePackStatus: return 0x18;
                    case PacketOutgoingType.ChatMessage: return 0x02;
                    case PacketOutgoingType.ClientStatus: return 0x03;
                    case PacketOutgoingType.ClientSettings: return 0x04;
                    case PacketOutgoingType.PluginMessage: return 0x09;
                    case PacketOutgoingType.TabComplete: return 0x01;
                    case PacketOutgoingType.PlayerPosition: return 0x0D;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0E;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else if (protocol < MC17w46aVersion)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0A;
                    case PacketOutgoingType.ResourcePackStatus: return 0x17;
                    case PacketOutgoingType.ChatMessage: return 0x01;
                    case PacketOutgoingType.ClientStatus: return 0x02;
                    case PacketOutgoingType.ClientSettings: return 0x03;
                    case PacketOutgoingType.PluginMessage: return 0x08;
                    case PacketOutgoingType.TabComplete: throw new InvalidOperationException("TabComplete was accidentely removed in protocol " + protocol + ". Please use a more recent version.");
                    case PacketOutgoingType.PlayerPosition: return 0x0C;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0D;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else if (protocol < MC113pre4Version)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0B;
                    case PacketOutgoingType.ResourcePackStatus: return 0x18;
                    case PacketOutgoingType.ChatMessage: return 0x01;
                    case PacketOutgoingType.ClientStatus: return 0x02;
                    case PacketOutgoingType.ClientSettings: return 0x03;
                    case PacketOutgoingType.PluginMessage: return 0x09;
                    case PacketOutgoingType.TabComplete: return 0x04;
                    case PacketOutgoingType.PlayerPosition: return 0x0D;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0E;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else if (protocol < MC113pre7Version)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0C;
                    case PacketOutgoingType.ResourcePackStatus: return 0x1B;
                    case PacketOutgoingType.ChatMessage: return 0x01;
                    case PacketOutgoingType.ClientStatus: return 0x02;
                    case PacketOutgoingType.ClientSettings: return 0x03;
                    case PacketOutgoingType.PluginMessage: return 0x09;
                    case PacketOutgoingType.TabComplete: return 0x04;
                    case PacketOutgoingType.PlayerPosition: return 0x0E;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0F;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0E;
                    case PacketOutgoingType.ResourcePackStatus: return 0x1D;
                    case PacketOutgoingType.ChatMessage: return 0x02;
                    case PacketOutgoingType.ClientStatus: return 0x03;
                    case PacketOutgoingType.ClientSettings: return 0x04;
                    case PacketOutgoingType.PluginMessage: return 0x0A;
                    case PacketOutgoingType.TabComplete: return 0x05;
                    case PacketOutgoingType.PlayerPosition: return 0x10;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x11;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }

            throw new System.ComponentModel.InvalidEnumArgumentException("Unknown PacketOutgoingType (protocol=" + protocol + ")", (int)packet, typeof(PacketOutgoingType));
        }
        private void readNextPacket(ref int packetID, List<byte> packetData)
        {
            packetData.Clear();
            int size = readNextVarIntRAW(); //Packet size
            packetData.AddRange(readDataRAW(size)); //Packet contents

            //Handle packet decompression
            if (protocolversion >= MC18Version && compression_treshold > 0)
            {
                int sizeUncompressed = readNextVarInt(packetData);
                if (sizeUncompressed != 0) // != 0 means compressed, let's decompress
                {
                    byte[] toDecompress = packetData.ToArray();
                    byte[] uncompressed = ZlibUtils.Decompress(toDecompress, sizeUncompressed);
                    packetData.Clear();
                    packetData.AddRange(uncompressed);
                }
            }

            packetID = readNextVarInt(packetData); //Packet ID
        }
        public int readNextVarIntRAW()
        {
            int i = 0;
            int j = 0;
            int k = 0;
            byte[] tmp = new byte[1];
            while (true)
            {
                Receive(tmp, 0, 1, SocketFlags.None);
                k = tmp[0];
                i |= (k & 0x7F) << j++ * 7;
                if (j > 5) ConsoleIO.AddMsgSeq("VarInt too big");
                if ((k & 0x80) != 128) break;
            }
            return i;
        }
        public byte[] readDataRAW(int offset)
        {
            if (offset > 0)
            {
                try
                {
                    byte[] cache = new byte[offset];
                    Receive(cache, 0, offset, SocketFlags.None);
                    return cache;
                }
                catch (OutOfMemoryException) { }
            }
            return new byte[] { };
        }
        public byte[] concatBytes(params byte[][] bytes)
        {
            List<byte> result = new List<byte>();
            foreach (byte[] array in bytes)
                result.AddRange(array);
            return result.ToArray();
        }
        public byte[] getVarInt(int paramInt)
        {
            List<byte> bytes = new List<byte>();
            while ((paramInt & -128) != 0)
            {
                bytes.Add((byte)(paramInt & 127 | 128));
                paramInt = (int)(((uint)paramInt) >> 7);
            }
            bytes.Add((byte)paramInt);
            return bytes.ToArray();
        }
        public byte[] getString(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);

            return concatBytes(getVarInt(bytes.Length), bytes);
        }
        private int readNextVarInt(List<byte> cache)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            while (true)
            {
                k = readNextByte(cache);
                i |= (k & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
                if ((k & 0x80) != 128) break;
            }
            return i;
        }
        private byte readNextByte(List<byte> cache)
        {
            byte result = cache[0];
            cache.RemoveAt(0);
            return result;

        }
        public string readNextString(List<byte> cache)
        {
            int length = readNextVarInt(cache);
            if (length > 0)
            {
                return Encoding.UTF8.GetString(readData(length, cache));
            }
            else return "";
        }
        private byte[] readData(int offset, List<byte> cache)
        {
            byte[] result = cache.Take(offset).ToArray();
            cache.RemoveRange(0, offset);
            return result;
        }
        private int readNextInt(List<byte> cache)
        {
            byte[] rawValue = readData(4, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToInt32(rawValue, 0);
        }
        private int readNextVarShort(List<byte> cache)
        {
            ushort low = readNextUShort(cache);
            byte high = 0;
            if ((low & 0x8000) != 0)
            {
                low &= 0x7FFF;
                high = readNextByte(cache);
            }
            return ((high & 0xFF) << 15) | low;
        }
        private ushort readNextUShort(List<byte> cache)
        {
            byte[] rawValue = readData(2, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToUInt16(rawValue, 0);
        }
        private short readNextShort(List<byte> cache)
        {
            byte[] rawValue = readData(2, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToInt16(rawValue, 0);
        }
        private bool readNextBool(List<byte> cache)
        {
            return readNextByte(cache) != 0x00;
        }
        public bool Update()
        {
            if (client.Client == null || !client.Connected) { return false; }
            try
            {
                while (client.Client.Available > 0)
                {
                    int packetID = 0;
                    List<byte> packetData = new List<byte>();
                    readNextPacket(ref packetID, packetData);
                    handlePacket(packetID, new List<byte>(packetData));
                }
            }
            catch (SocketException) { return false; }
            catch (NullReferenceException) { return false; }
            return true;
        }
        private void Updater()
        {
            try
            {
                do
                {
                    Thread.Sleep(500);
                }
                while (Update());
            }
            catch (System.IO.IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            handler.OnConnectionLost(BotUtils.DisconnectReason.ConnectionLost, "Connection Close.");
        }

        private void StartUpdating()
        {
            netRead = new Thread(new ThreadStart(Updater));
            netRead.Name = "ProtocolPacketHandler";
            netRead.Start();
        }


        public bool Login(string host, int port,string username)
        {
            byte[] protocol_version = getVarInt(protocolversion);
            byte[] server_adress_val = Encoding.UTF8.GetBytes(host + (forgeInfo != null ? "\0FML\0" : ""));
            byte[] server_adress_len = getVarInt(server_adress_val.Length);
            byte[] server_port = BitConverter.GetBytes((ushort)port); Array.Reverse(server_port);
            byte[] next_state = getVarInt(2);
            byte[] handshake_packet = concatBytes(protocol_version, server_adress_len, server_adress_val, server_port, next_state);

            SendPacket(0x00, handshake_packet);
            byte[] username_val = Encoding.UTF8.GetBytes(username);
            byte[] username_len = getVarInt(username_val.Length);
            byte[] login_packet = concatBytes(username_len, username_val);
            SendPacket(0x00, login_packet);
            int packetID = -1;
            List<byte> packetData = new List<byte>();
            while (true)
            {
                readNextPacket(ref packetID, packetData);
                if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(BotUtils.DisconnectReason.LoginRejected, readNextString(packetData));
                    return false;
                }
                else if (packetID == 0x01) //Encryption request
                {
                    ConsoleIO.AddMsgSeq(username + "This Server is in online mode.", "Connection");
                    return false;
                }
                else if (packetID == 0x02) //Login successful
                {
                    login_phase = false;
                    if (forgeInfo != null)
                    {
                        // Do the forge handshake.
                        if (!CompleteForgeHandshake())
                        {
                            return false;
                        }
                    }

                    StartUpdating();
                    return true; //No need to check session or start encryption
                }
                else handlePacket(packetID, packetData);
            }
        }
        private void SendPacket(PacketOutgoingType packetID, IEnumerable<byte> packetData)
        {
            SendPacket(getPacketOutgoingID(packetID, protocolversion), packetData);
        }
        private void SendPacket(int packetID, IEnumerable<byte> packetData)
        {
            byte[] the_packet = concatBytes(getVarInt(packetID), packetData.ToArray());
            if (compression_treshold > 0) //Compression enabled?
            {
                if (the_packet.Length >= compression_treshold) //Packet long enough for compressing?
                {
                    byte[] compressed_packet = ZlibUtils.Compress(the_packet);
                    the_packet = concatBytes(getVarInt(the_packet.Length), compressed_packet);
                }
                else
                {
                    byte[] uncompressed_length = getVarInt(0); //Not compressed (short packet)
                    the_packet = concatBytes(uncompressed_length, the_packet);
                }
            }
            try
            {
                client.Client.Send(concatBytes(getVarInt(the_packet.Length), the_packet));
            }
            catch
            {
                handler.OnConnectionLost(BotUtils.DisconnectReason.ConnectionLost, "PacketSendError.");
            }

        }
        private void SendForgeHandshakePacket(FMLHandshakeDiscriminator discriminator, byte[] data)
        {
            SendPluginChannelPacket("FML|HS", concatBytes(new byte[] { (byte)discriminator }, data));
        }
        public bool SendChatMessage(string message)
        {
            if (String.IsNullOrEmpty(message))
                return true;
            try
            {
                byte[] message_val = Encoding.UTF8.GetBytes(message);
                byte[] message_len = getVarInt(message_val.Length);
                byte[] message_packet = concatBytes(message_len, message_val);
                SendPacket(PacketOutgoingType.ChatMessage, message_packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }
        public bool SendRespawnPacket()
        {
            try
            {
                SendPacket(PacketOutgoingType.ClientStatus, new byte[] { 0 });
                return true;
            }
            catch (SocketException) { return false; }
        }
        public bool SendClientSettings(string language, byte viewDistance, byte difficulty, byte chatMode, bool chatColors, byte skinParts, byte mainHand)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(getString(language));
                fields.Add(viewDistance);
                fields.AddRange(protocolversion >= MC19Version
                    ? getVarInt(chatMode)
                    : new byte[] { chatMode });
                fields.Add(chatColors ? (byte)1 : (byte)0);
                if (protocolversion < MC18Version)
                {
                    fields.Add(difficulty);
                    fields.Add((byte)(skinParts & 0x1)); //show cape
                }
                else fields.Add(skinParts);
                if (protocolversion >= MC19Version)
                    fields.AddRange(getVarInt(mainHand));
                SendPacket(PacketOutgoingType.ClientSettings, fields);
            }
            catch (SocketException) { }
            return false;
        }
        public bool SendPluginChannelPacket(string channel, byte[] data)
        {
            try
            {
                if (protocolversion < MC18Version)
                {
                    byte[] length = BitConverter.GetBytes((short)data.Length);
                    Array.Reverse(length);

                    SendPacket(PacketOutgoingType.PluginMessage, concatBytes(getString(channel), length, data));
                }
                else
                {
                    SendPacket(PacketOutgoingType.PluginMessage, concatBytes(getString(channel), data));
                }

                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }

        public bool AutoTabComplete(string BehindCursor)
        {
            if (String.IsNullOrEmpty(BehindCursor))
                return false;
            byte[] transaction_id = getVarInt(autocomplete_transaction_id);
            byte[] assume_command = new byte[] { 0x00 };
            byte[] has_position = new byte[] { 0x00 };

            byte[] tabcomplete_packet = new byte[] { };
            if (protocolversion >= MC18Version)
            {
                if (protocolversion >= MC17w46aVersion)
                {
                    tabcomplete_packet = concatBytes(tabcomplete_packet, transaction_id);
                    tabcomplete_packet = concatBytes(tabcomplete_packet, getString(BehindCursor));
                }
                else
                {
                    tabcomplete_packet = concatBytes(tabcomplete_packet, getString(BehindCursor));

                    if (protocolversion >= MC19Version)
                    {
                        tabcomplete_packet = concatBytes(tabcomplete_packet, assume_command);
                    }

                    tabcomplete_packet = concatBytes(tabcomplete_packet, has_position);
                }
            }
            else
            {
                tabcomplete_packet = concatBytes(getString(BehindCursor));
            }
            SendPacket(PacketOutgoingType.TabComplete, tabcomplete_packet);
            return true;
        }
        public void Dispose()
        {
            try
            {
                if (netRead != null)
                    netRead.Abort();
                if(client!=null)
                    client.Close();
            }
            catch { }
        }
    }
}
