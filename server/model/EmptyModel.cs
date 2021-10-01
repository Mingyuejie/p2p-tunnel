﻿using MessagePack;
using ProtoBuf;
using server.model;

namespace server.models
{
    [ProtoContract, MessagePackObject]
    public class EmptyModel
    {
        public EmptyModel() { }
        [ProtoMember(1), Key(1)]
        public byte Id { get; } = 0;

    }
}
