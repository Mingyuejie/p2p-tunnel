using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace common.extends
{
    public static class ByteArrayExtends
    {
        public static Byte[] ToBytes<T>(this T obj)
        {
            using var memory = new MemoryStream();
            Serializer.Serialize(memory, obj);
            return memory.ToArray();
        }
        public static T DeBytes<T>(this byte[] data)
        {
            using var memory = new MemoryStream(data);
            return Serializer.Deserialize<T>(memory);
        }


        private static byte[] chunkedHeaderBytes = Encoding.ASCII.GetBytes("Transfer-Encoding: chunked");
        /// <summary>
        /// 判断是否分块传输
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static bool IsChunked(this byte[] lines)
        {
            return HeaderEqual(lines, chunkedHeaderBytes);
        }
        /// <summary>
        /// \r\n\r\n结束 13 10 13 10
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static bool IsChunkedEnd(this byte[] lines)
        {
            return lines.Length >= 4 && lines[^1] == 10 && lines[^2] == 13 && lines[^3] == 10 && lines[^4] == 13;
        }

        private static byte[] keepaliveHeaderBytes = Encoding.ASCII.GetBytes("Connection: keep-alive");
        /// <summary>
        /// 是否是keepalive
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static bool IsKeepAline(this byte[] lines)
        {
            return HeaderEqual(lines, keepaliveHeaderBytes);
        }

        public static bool HeaderEqual(this byte[] lines, byte[] headerBytes)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == 10 && lines[i - 1] == 13 && lines.Length - i >= headerBytes.Length)
                {
                    if (Enumerable.SequenceEqual(lines.Skip(i + 1).Take(headerBytes.Length), headerBytes))
                    {
                        return true;
                    }
                }

                //头结束
                if (lines[i] == 10 && lines[i - 1] == 13 && lines[i - 2] == 10 && lines[i - 3] == 13)
                {
                    return false;
                }
            }
            return false;
        }


        public static byte[] GZip(this byte[] bytes)
        {
            using MemoryStream compressStream = new MemoryStream();
            using var zipStream = new GZipStream(compressStream, CompressionMode.Compress);
            zipStream.Write(bytes, 0, bytes.Length);
            zipStream.Close();
            return compressStream.ToArray();
        }

        public static byte[] UnGZip(this byte[] bytes)
        {
            using var compressStream = new MemoryStream(bytes);
            using var zipStream = new GZipStream(compressStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            zipStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }
    }
}
