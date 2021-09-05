using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace client.service.p2pPlugins.plugins.request
{
    public interface IRequestExcuteMessage
    {
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum RequestTypes
    {
        REQUEST, RESULT
    }
}
