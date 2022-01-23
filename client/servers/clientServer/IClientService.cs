using common.extends;
using Fleck;
using MessagePack;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace client.servers.clientServer
{
    public interface IClientService{}

    [ProtoContract, MessagePackObject]
    public class ClientServiceResponseInfo
    {
        [ProtoMember(1), Key(1)]
        public string Path { get; set; } = string.Empty;
        [ProtoMember(2), Key(2)]
        public long RequestId { get; set; } = 0;
        [ProtoMember(3), Key(3)]
        public int Code { get; set; } = 0;
        [ProtoMember(4), Key(4)]
        public object Content { get; set; } = string.Empty;
    }

    public class ClientServiceRequestInfo
    {
        public string Path { get; set; } = string.Empty;
        public long RequestId { get; set; } = 0;
        public int Code { get; set; } = 0;
        public string Content { get; set; } = string.Empty;
    }

    public class ClientServiceParamsInfo
    {
        public IWebSocketConnection Socket { get; set; }
        public long RequestId { get; set; } = 0;
        public string Content { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public ConcurrentDictionary<Guid, IWebSocketConnection> Websockets { get; set; }

        public int Code { get; private set; } = 0;
        public string ErrorMessage { get; private set; } = string.Empty;
        public void SetCode(int code, string errormsg = "")
        {
            Code = code;
            ErrorMessage = errormsg;
        }
        public void SetErrorMessage(string msg)
        {
            Code = -1;
            ErrorMessage = msg;
        }
    }
}
