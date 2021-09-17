using Fleck;
using System;
using System.Collections.Concurrent;

namespace client.service.clientService
{
    public interface IClientServicePlugin
    {

    }

    public class ClientServiceMessageResponseWrap
    {
        public string Path { get; set; } = string.Empty;
        public long RequestId { get; set; } = 0;
        public int Code { get; set; } = 0;
        public object Content { get; set; } = string.Empty;
    }

    public class ClientServiceMessageWrap
    {
        public string Path { get; set; } = string.Empty;
        public long RequestId { get; set; } = 0;
        public int Code { get; set; } = 0;
        public string Content { get; set; } = string.Empty;
    }

    public class ClientServicePluginExcuteWrap
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
            ErrorMessage = msg;
        }
    }
}
