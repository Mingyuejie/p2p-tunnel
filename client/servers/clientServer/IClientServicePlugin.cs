using Fleck;
using ProtoBuf;
using System;
using System.Collections.Concurrent;

namespace client.servers.clientServer
{
    public interface IClientServicePlugin
    {
        //string Name { get; }
        //bool Enabled { get; set; }

        //void Enable(ClientServicePluginExcuteWrap arg);
    }

    public interface IClientServiceSettingPlugin
    {
        string Name { get; }
        string Author { get; }
        object LoadSetting();
        void SaveSetting(string jsonStr);
    }

    [ProtoContract]
    public class ClientServiceMessageResponseWrap
    {
        [ProtoMember(1)]
        public string Path { get; set; } = string.Empty;
        [ProtoMember(2)]
        public long RequestId { get; set; } = 0;
        [ProtoMember(3)]
        public int Code { get; set; } = 0;
        [ProtoMember(4)]
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
