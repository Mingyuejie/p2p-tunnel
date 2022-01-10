using MessagePack;
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
        static MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
        public static byte[] ToBytes<T>(this T obj)
        {
            if (obj is byte[] bytes)
            {
                return bytes;
            }
            return MessagePackSerializer.Serialize(obj);
        }
        public static byte[] ToBytesWithCompression<T>(this T obj)
        {
            return MessagePackSerializer.Serialize(obj, lz4Options);
        }
        public static T DeBytes<T>(this byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data);
        }
        public static T DeBytes<T>(this Memory<byte> data)
        {
            return MessagePackSerializer.Deserialize<T>(data);
        }
        public static T DeBytes<T>(this ReadOnlyMemory<byte> data)
        {
            return MessagePackSerializer.Deserialize<T>(data);
        }

        public static T DeBytesWithCompression<T>(this byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data, lz4Options);
        }
        public static T DeBytesWithCompression<T>(this Memory<byte> data)
        {
            return MessagePackSerializer.Deserialize<T>(data, lz4Options);
        }
        public static T DeBytesWithCompression<T>(this ReadOnlyMemory<byte> data)
        {
            return MessagePackSerializer.Deserialize<T>(data, lz4Options);
        }

        private static byte[] optionsBytes = Encoding.ASCII.GetBytes("OPTIONS");
        public static bool IsOptionsMethod(this byte[] lines)
        {
            return lines.IsMethod(optionsBytes);
        }
        public static bool IsMethod(this byte[] lines, byte[] method)
        {
            return lines.Length > method.Length && Enumerable.SequenceEqual(lines.Take(method.Length), method);
        }
        private static byte[] chunkedHeaderBytes = Encoding.ASCII.GetBytes("Transfer-Encoding: chunked");
        public static bool IsChunked(this byte[] lines)
        {
            return HeaderEqual(lines, chunkedHeaderBytes);
        }
        public static bool IsChunkedEnd(this byte[] lines)
        {
            return lines.Length >= 4 && lines[^1] == 10 && lines[^2] == 13 && lines[^3] == 10 && lines[^4] == 13;
        }
        private static byte[] keepaliveHeaderBytes = Encoding.ASCII.GetBytes("Connection: keep-alive");
        public static bool IsKeepAline(this byte[] lines)
        {
            return HeaderEqual(lines, keepaliveHeaderBytes);
        }
        private static byte[] contentLengthHeaderBytes = Encoding.ASCII.GetBytes("Content-Length:");
        public static bool IsContentLength(this byte[] lines)
        {
            return HeaderEqual(lines, contentLengthHeaderBytes);
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
            zipStream.Close();//不先关闭会有 解压结果为0的bug
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
