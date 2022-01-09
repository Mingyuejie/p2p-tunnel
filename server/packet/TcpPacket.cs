using common;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace server.packet
{
    /// <summary>
    /// 自定义TCP数据包
    /// </summary>
    public class TcpPacket : IPacket
    {
        public byte[] Chunk { get; set; }

        public TcpPacket(byte[] chunk)
        {
            Chunk = chunk;
        }

        /// <summary>
        /// 包转 byte[]
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            return ToArray(Chunk);
        }

        public static byte[] ToArray(byte[] chunk)
        {
            byte[] lengthArray = BitConverter.GetBytes(chunk.Length);

            byte[] result = new byte[lengthArray.Length + chunk.Length];

            int distIndex = 0;
            Array.Copy(lengthArray, 0, result, distIndex, lengthArray.Length);
            distIndex += lengthArray.Length;

            Array.Copy(chunk, 0, result, distIndex, chunk.Length);

            return result;
        }

        /// <summary>
        /// byte[]  转为包结构
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        public static List<TcpPacket> FromArray(List<byte> buffer)
        {
            List<TcpPacket> result = new List<TcpPacket>();
            do
            {
                int packageLen = BitConverter.ToInt32(buffer.GetRange(0, 4).ToArray());
                if (packageLen > buffer.Count - 4)
                {
                    break;
                }

                byte[] rev = buffer.GetRange(4, packageLen).ToArray();
                buffer.RemoveRange(0, packageLen + 4);
                result.Add(new TcpPacket(rev));
            } while (buffer.Count > 4);

            return result;
        }
    }
}
