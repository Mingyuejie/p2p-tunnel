using server.model;
using System;

namespace server.packet
{
    public interface IPacket
    {
        public byte[] ToArray();

        public byte[] Chunk { get; set; }

    }
}
