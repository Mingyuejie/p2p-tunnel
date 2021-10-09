using common.extends;
using Fleck;
using MessagePack;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace client.servers.clientServer
{
    public interface IClientServicePlugin
    {
    }

    public interface IClientServiceSettingPlugin
    {
        string Name { get; }
        string Author { get; }
        string Desc { get; }
        bool Enable { get; }
        bool SwitchEnable(bool enable);
        object LoadSetting();
        string SaveSetting(string jsonStr);
    }

    public class SettingModelBase
    {
        protected static T FromFile<T>(string fileName)
        {
            if (File.Exists(fileName))
            {
                return File.ReadAllText(fileName).DeJson<T>();
            }
            return default;
        }

        protected void ToFile<T>(T obj, string fileName)
        {
            File.WriteAllText(fileName, obj.ToJson(), System.Text.Encoding.UTF8);
        }
    }

    [ProtoContract, MessagePackObject]
    public class ClientServiceMessageResponseWrap
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
