using Fleck;
using MessagePack;
using System;
using System.Collections.Concurrent;

namespace client.servers.clientServer
{
    public interface IClientService { }

    [MessagePackObject]
    public class ClientServiceResponseInfo
    {
        [Key(1)]
        public string Path { get; set; } = string.Empty;
        [Key(2)]
        public long RequestId { get; set; } = 0;
        [Key(3)]
        public int Code { get; set; } = 0;
        [Key(4)]
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
        public long RequestId { get; set; } = 0;
        public string Content { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

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
