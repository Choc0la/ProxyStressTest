using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zlib;

namespace MinecraftBots.Protocol.Client.Handler
{
    public static class ZlibUtils
    {
        /// <summary>
        /// Compress a byte array into another bytes array using Zlib compression
        /// </summary>
        /// <param name="to_compress">Data to compress</param>
        /// <returns>Compressed data as a byte array</returns>
        public static byte[] Compress(byte[] to_compress)
        {
            byte[] data;
            using (System.IO.MemoryStream memstream = new System.IO.MemoryStream())
            {
                using (ZlibStream stream = new ZlibStream(memstream, Ionic.Zlib.CompressionMode.Compress))
                {
                    stream.Write(to_compress, 0, to_compress.Length);
                }
                data = memstream.ToArray();
            }
            return data;
        }

        /// <summary>
        /// Decompress a byte array into another byte array of the specified size
        /// </summary>
        /// <param name="to_decompress">Data to decompress</param>
        /// <param name="size_uncompressed">Size of the data once decompressed</param>
        /// <returns>Decompressed data as a byte array</returns>
        public static byte[] Decompress(byte[] to_decompress, int size_uncompressed)
        {
            ZlibStream stream = new ZlibStream(new System.IO.MemoryStream(to_decompress, false), Ionic.Zlib.CompressionMode.Decompress);
            byte[] packetData_decompressed = new byte[size_uncompressed];
            stream.Read(packetData_decompressed, 0, size_uncompressed);
            stream.Close();
            return packetData_decompressed;
        }

        public static string GZIPdecompress(byte[] to_decompress)
        {
            Stream stream = new MemoryStream(to_decompress);
            MemoryStream outBuffer = new MemoryStream();
            try
            {
                System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
                byte[] buf = new byte[512];
                while (true)
                {
                    int bytesRead = gzip.Read(buf, 0, buf.Length);
                    if (bytesRead <= 0)
                        break;
                    else
                        outBuffer.Write(buf, 0, bytesRead);
                }
                gzip.Close();
            }
            catch { }
            return Encoding.UTF8.GetString(outBuffer.ToArray());
        }

        /// <summary>
        /// Decompress a byte array into another byte array of a potentially unlimited size (!)
        /// </summary>
        /// <param name="to_decompress">Data to decompress</param>
        /// <returns>Decompressed data as byte array</returns>
        public static byte[] Decompress(byte[] to_decompress)
        {
            ZlibStream stream = new ZlibStream(new System.IO.MemoryStream(to_decompress, false), Ionic.Zlib.CompressionMode.Decompress);
            byte[] buffer = new byte[16 * 1024];
            using (System.IO.MemoryStream decompressedBuffer = new System.IO.MemoryStream())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    decompressedBuffer.Write(buffer, 0, read);
                return decompressedBuffer.ToArray();
            }
        }
    }
}
