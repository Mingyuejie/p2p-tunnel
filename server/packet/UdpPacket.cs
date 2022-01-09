using common;
using common.extends;
using server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace server.packet
{

    /// <summary>
    /// 自定义UDP数据包
    /// </summary>
    public class UdpPacket : IPacket
    {
        private static readonly ConcurrentDictionary<long, ConcurrentDictionary<ulong, PacketCacheModel>> packetChunkCache = new();
        private static NumberSpace udpSequenceNumberSpace = new NumberSpace(0);
        private static int chunkLength = 1400;

        public ulong Sequence { get; set; }
        public int Total { get; set; }
        public int Index { get; set; }
        public byte[] Chunk { get; set; }


        private UdpPacket()
        {

        }
        public UdpPacket(ulong sequence, int total, int index, byte[] chunk) : this()
        {
            Sequence = sequence;
            Total = total;
            Index = index;
            Chunk = chunk;
        }

        public byte[] ToArray()
        {
            byte[] sequenceArray = BitConverter.GetBytes(Sequence);
            byte[] indexArray = BitConverter.GetBytes(Index);
            byte[] totalArray = BitConverter.GetBytes(Total);

            byte[] dist = new byte[Chunk.Length + indexArray.Length + sequenceArray.Length + totalArray.Length];

            int distIndex = 0;


            Array.Copy(sequenceArray, 0, dist, distIndex, sequenceArray.Length);
            distIndex += sequenceArray.Length;

            Array.Copy(indexArray, 0, dist, distIndex, indexArray.Length);
            distIndex += indexArray.Length;

            Array.Copy(totalArray, 0, dist, distIndex, totalArray.Length);
            distIndex += totalArray.Length;

            Array.Copy(Chunk, 0, dist, distIndex, Chunk.Length);

            return dist;
        }


        public static UdpPacket FromArray(IPEndPoint ip, byte[] array)
        {
            return FromArray(ip.ToInt64(), array);
        }

        public static UdpPacket FromArray(long ip, byte[] array)
        {
            int skipIndex = 0;

            ulong sequence = BitConverter.ToUInt64(array.Skip(skipIndex).Take(8).ToArray());
            skipIndex += 8;

            int index = BitConverter.ToInt32(array.Skip(skipIndex).Take(4).ToArray());
            skipIndex += 4;

            int total = BitConverter.ToInt32(array.Skip(skipIndex).Take(4).ToArray());
            skipIndex += 4;

            byte[] chunk = array.Skip(skipIndex).ToArray();
            if (total == 1)
            {
                return new UdpPacket(sequence, total, index, chunk);
            }

            //ip 分类
            packetChunkCache.TryGetValue(ip, out ConcurrentDictionary<ulong, PacketCacheModel> ipPackets);
            if (ipPackets == null)
            {
                ipPackets = new ConcurrentDictionary<ulong, PacketCacheModel>();
            }
            //ip下的序号
            ipPackets.TryGetValue(sequence, out PacketCacheModel packets);
            if (packets == null)
            {
                packets = new PacketCacheModel();
            }
            packets.Time = Helper.GetTimeStamp();
            packets.Buffers.Add(new PacketCacheBufferModel { Index = index, Buffers = chunk });

            if (packets.Buffers.Count == total)
            {
                ipPackets.Remove(sequence, out _);
                if (ipPackets.Values.Count <= 0)
                {
                    packetChunkCache.TryRemove(ip, out _);
                }

                IOrderedEnumerable<PacketCacheBufferModel> ceches = packets.Buffers.OrderBy(c => c.Index);
                int totalPageSize = ceches.Sum(c => c.Buffers.Length);
                byte[] dist = new byte[totalPageSize];

                int distIndex = 0;
                foreach (PacketCacheBufferModel item in ceches)
                {
                    Buffer.BlockCopy(item.Buffers, 0, dist, distIndex, item.Buffers.Length);
                    distIndex += item.Buffers.Length;
                }
                packets.Buffers.Clear();
                return new UdpPacket(sequence, total, index, dist);
            }
            else
            {
                ipPackets.AddOrUpdate(sequence, packets, (k, v) => packets);

                long time = Helper.GetTimeStamp();
                foreach (KeyValuePair<long, ConcurrentDictionary<ulong, PacketCacheModel>> item in packetChunkCache)
                {
                    foreach (KeyValuePair<ulong, PacketCacheModel> item1 in item.Value)
                    {
                        if (time - item1.Value.Time > 100)
                        {
                            item.Value.TryRemove(item1.Key, out _);
                        }
                    }

                    if (item.Value.Count == 0)
                    {
                        packetChunkCache.TryRemove(item.Key, out _);
                    }
                }
            }

            return null;
        }

        public static IEnumerable<UdpPacket> Split(byte[] datagram)
        {
            if (datagram == null)
            {
                throw new ArgumentNullException("datagram");
            }

            List<UdpPacket> packets = new();
            ulong sequence = udpSequenceNumberSpace.Get();

            int chunks = datagram.Length / chunkLength;
            int remainder = datagram.Length % chunkLength;
            int total = chunks;
            if (remainder > 0)
            {
                total++;
            }

            for (int i = 1; i <= chunks; i++)
            {
                byte[] chunk = new byte[chunkLength];
                Buffer.BlockCopy(datagram, (i - 1) * chunkLength, chunk, 0, chunkLength);
                packets.Add(new UdpPacket(sequence, total, i, chunk));
            }
            if (remainder > 0)
            {
                int length = datagram.Length - (chunkLength * chunks);
                byte[] chunk = new byte[length];
                Buffer.BlockCopy(datagram, chunkLength * chunks, chunk, 0, length);
                packets.Add(new UdpPacket(sequence, total, total, chunk));
            }

            return packets;
        }
    }

    public class PacketCacheModel
    {
        public long Time { get; set; } = Helper.GetTimeStamp();
        public List<PacketCacheBufferModel> Buffers { get; set; } = new List<PacketCacheBufferModel>();
    }
    public class PacketCacheBufferModel
    {
        public int Index { get; set; } = 0;
        public byte[] Buffers { get; set; } = Array.Empty<byte>();
    }
}
