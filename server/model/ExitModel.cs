﻿using ProtoBuf;
using server.model;

namespace server.models
{
    [ProtoContract]
    public class ExitModel
    {
        public ExitModel() { }
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

    }
}
