using Fleck;
using System;
using System.Collections.Concurrent;

namespace client.service.clientService
{
    public interface IClientServicePlugin
    {

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
        public string Message { get; private set; } = string.Empty;
        public void SetResultCode(int code, string msg = "")
        {
            Code = code;
            Message = msg;
        }
        public void SetResultMessage(string msg)
        {
            Message = msg;
        }

        public Action<ClientServicePluginExcuteWrap, object> Callback { get; set; }

    }
}
