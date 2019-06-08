using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace MinecraftBots.Protocol.Server
{
    public class ProtocolHandler
    {
        TcpClient c;
        public ProtocolHandler(TcpClient tcp)
        {
            this.c = tcp;
        }
        public static bool MinecraftServiceLookup(ref string domain, ref ushort port)
        {
            if (!String.IsNullOrEmpty(domain) && domain.Any(c => char.IsLetter(c)))
            {
                try
                {
                    Console.WriteLine("Resolving {0}...", domain);
                    Heijden.DNS.Response response = new Heijden.DNS.Resolver().Query("_minecraft._tcp." + domain, Heijden.DNS.QType.SRV);
                    Heijden.DNS.RecordSRV[] srvRecords = response.RecordsSRV;
                    if (srvRecords != null && srvRecords.Any())
                    {
                        //Order SRV records by priority and weight, then randomly
                        Heijden.DNS.RecordSRV result = srvRecords
                            .OrderBy(record => record.PRIORITY)
                            .ThenByDescending(record => record.WEIGHT)
                            .ThenBy(record => Guid.NewGuid())
                            .First();
                        string target = result.TARGET.Trim('.');
                        domain = target;
                        port = result.PORT;
                        Console.WriteLine("检测到SRV记录地址：" + domain + ":" + port);
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("未能成功解析..." + e.Message);
                }
            }
            return false;
        }

        public void Receive(byte[] buffer, int start, int offset, SocketFlags f)
        {
            int read = 0;
            while (read < offset)
            {
                read += c.Client.Receive(buffer, start + read, offset - read, f);
            }
        }

        public string readNextString()
        {
            ushort length = (ushort)readNextShort();
            if (length > 0)
            {
                byte[] cache = new byte[length * 2];
                Receive(cache, 0, length * 2, SocketFlags.None);
                string result = Encoding.BigEndianUnicode.GetString(cache);
                return result;
            }
            else return "";
        }

        /// <summary>
        /// Build an integer for sending over the network
        /// </summary>
        /// <param name="paramInt">Integer to encode</param>
        /// <returns>Byte array for this integer</returns>
        public static byte[] getVarInt(int paramInt)
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

        /// <summary>
        /// Easily append several byte arrays
        /// </summary>
        /// <param name="bytes">Bytes to append</param>
        /// <returns>Array containing all the data</returns>
        public static byte[] concatBytes(params byte[][] bytes)
        {
            List<byte> result = new List<byte>();
            foreach (byte[] array in bytes)
                result.AddRange(array);
            return result.ToArray();
        }

        /// <summary>
        /// Read some data from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="offset">Amount of bytes to read</param>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The data read from the cache as an array</returns>
        private static byte[] readData(int offset, List<byte> cache)
        {
            byte[] result = cache.Take(offset).ToArray();
            cache.RemoveRange(0, offset);
            return result;
        }

        /// <summary>
        /// Read a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The string</returns>
        public static string readNextString(List<byte> cache)
        {
            int length = readNextVarInt(cache);
            if (length > 0)
            {
                return Encoding.UTF8.GetString(readData(length, cache));
            }
            else return "";
        }

        public byte[] readNextByteArray()
        {
            short len = readNextShort();
            byte[] data = new byte[len];
            Receive(data, 0, len, SocketFlags.None);
            return data;
        }

        public short readNextShort()
        {
            byte[] tmp = new byte[2];
            Receive(tmp, 0, 2, SocketFlags.None);
            Array.Reverse(tmp);
            return BitConverter.ToInt16(tmp, 0);
        }

        public int readNextInt()
        {
            byte[] tmp = new byte[4];
            Receive(tmp, 0, 4, SocketFlags.None);
            Array.Reverse(tmp);
            return BitConverter.ToInt32(tmp, 0);
        }

        public byte readNextByte()
        {
            byte[] result = new byte[1];
            Receive(result, 0, 1, SocketFlags.None);
            return result[0];
        }

        /// <summary>
        /// Read a single byte from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The byte that was read</returns>
        public static byte readNextByte(List<byte> cache)
        {
            byte result = cache[0];
            cache.RemoveAt(0);
            return result;
        }

        /// <summary>
        /// Read an integer from the network
        /// </summary>
        /// <returns>The integer</returns>
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
                if (j > 5) throw new OverflowException("VarInt too big");
                if ((k & 0x80) != 128) break;
            }
            return i;
        }

        /// <summary>
        /// Read some data directly from the network
        /// </summary>
        /// <param name="offset">Amount of bytes to read</param>
        /// <returns>The data read from the network as an array</returns>
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

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The integer</returns>
        public static int readNextVarInt(List<byte> cache)
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

        /// <summary>
        /// Convert a human-readable Minecraft version number to network protocol version number
        /// </summary>
        /// <param name="MCVersion">The Minecraft version number</param>
        /// <returns>The protocol version number or 0 if could not determine protocol version: error, unknown, not supported</returns>
        public static int MCVer2ProtocolVersion(string MCVersion)
        {
            if (MCVersion.Contains('.'))
            {
                switch (MCVersion.Split(' ')[0].Trim())
                {
                    case "1.4.6":
                    case "1.4.7":
                        return 51;
                    case "1.5.1":
                        return 60;
                    case "1.5.2":
                        return 61;
                    case "1.6":
                    case "1.6.0":
                        return 72;
                    case "1.6.1":
                    case "1.6.2":
                    case "1.6.3":
                    case "1.6.4":
                        return 73;
                    case "1.7.2":
                    case "1.7.3":
                    case "1.7.4":
                    case "1.7.5":
                        return 4;
                    case "1.7.6":
                    case "1.7.7":
                    case "1.7.8":
                    case "1.7.9":
                    case "1.7.10":
                        return 5;
                    case "1.8":
                    case "1.8.0":
                    case "1.8.1":
                    case "1.8.2":
                    case "1.8.3":
                    case "1.8.4":
                    case "1.8.5":
                    case "1.8.6":
                    case "1.8.7":
                    case "1.8.8":
                    case "1.8.9":
                        return 47;
                    case "1.9":
                    case "1.9.0":
                        return 107;
                    case "1.9.1":
                        return 108;
                    case "1.9.2":
                        return 109;
                    case "1.9.3":
                    case "1.9.4":
                        return 110;
                    case "1.10":
                    case "1.10.0":
                    case "1.10.1":
                    case "1.10.2":
                        return 210;
                    case "1.11":
                    case "1.11.0":
                        return 315;
                    case "1.11.1":
                    case "1.11.2":
                        return 316;
                    case "1.12":
                    case "1.12.0":
                        return 335;
                    case "1.12.1":
                        return 338;
                    case "1.12.2":
                        return 340;
                    case "1.13":
                        return 393;
                    case "1.13.1":
                        return 401;
                    default:
                        return 0;
                }
            }
            else
            {
                try
                {
                    return Int32.Parse(MCVersion);
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static string getGameVersion(int protover)
        {
            switch (protover)
            {
                case 51:
                    return "1.4.6";
                case 60:
                    return "1.5.1";
                case 61:
                    return "1.5.2";
                case 72:
                    return "1.6";
                case 73:
                    return "1.6.2";
                case 4:
                    return "1.7.2";
                case 5:
                    return "1.7.10";
                case 47:
                    return "1.8";
                case 107:
                    return "1.9";
                case 108:
                    return "1.9.1";
                case 109:
                    return "1.9.2";
                case 110:
                    return "1.9.4";
                case 210:
                    return "1.10";
                case 315:
                    return "1.11";
                case 316:
                    return "1.11.2";
                case 335:
                    return "1.12";
                case 338:
                    return "1.12.1";
                case 340:
                    return "1.12.2";
                case 393:
                    return "1.13";
                case 401:
                    return "1.13.1";
                default:
                    return "";
            }
        }
    }
}
