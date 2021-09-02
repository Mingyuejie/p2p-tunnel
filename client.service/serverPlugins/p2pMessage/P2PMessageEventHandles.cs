using client.service.events;
using common.extends;
using server.model;
using server.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace client.service.serverPlugins.p2pMessage
{
    public class P2PMessageEventHandles
    {
        private static readonly Lazy<P2PMessageEventHandles> lazy = new(() => new P2PMessageEventHandles());
        public static P2PMessageEventHandles Instance => lazy.Value;
        private readonly Dictionary<P2PDataMessageTypes, IP2PMessagePlugin[]> plugins = null;

        private P2PMessageEventHandles()
        {
            plugins = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IP2PMessagePlugin)))
                .Select(c => (IP2PMessagePlugin)Activator.CreateInstance(c)).GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public event EventHandler<OnP2PTcpMessageArg> OnMessageHandler;
        public void OnMessage(OnP2PTcpMessageArg arg)
        {
            P2PDataMessageTypes type = (P2PDataMessageTypes)arg.Data.DataType;

            if (plugins.ContainsKey(type))
            {
                IP2PMessagePlugin[] plugin = plugins[type];
                if (plugin.Length > 0)
                {
                    for (int i = 0; i < plugin.Length; i++)
                    {
                        plugin[i].Excute(arg);
                    }
                }
            }

            OnMessageHandler?.Invoke(this, arg);
        }

        public event EventHandler<OnP2PTcpMessageArg> OnTcpMessageHandler;
        public void OnTcpMessage(OnP2PTcpMessageArg arg)
        {
            P2PDataMessageTypes type = (P2PDataMessageTypes)arg.Data.DataType;

            if (plugins.ContainsKey(type))
            {
                IP2PMessagePlugin[] plugin = plugins[type];
                if (plugin.Length > 0)
                {
                    for (int i = 0; i < plugin.Length; i++)
                    {
                        plugin[i].Excute(arg);
                    }
                }
            }

            OnTcpMessageHandler?.Invoke(this, arg);
        }

        public event EventHandler<SendP2PTcpMessageArg> OnSendTcpMessageHandler;
        public void SendTcpMessage(SendP2PTcpMessageArg arg)
        {
            EventHandlers.Instance.SendTcpMessage(new SendTcpMessageEventArg
            {
                Socket = arg.Socket,
                Data = new MessageP2PModel
                {
                    Data = arg.Data.ProtobufSerialize(),
                    DataType = (byte)arg.Data.Type,
                    FormId = EventHandlers.Instance.ConnectId
                }
            });

            OnSendTcpMessageHandler?.Invoke(this, arg);
        }

        public event EventHandler<SendP2PMessageArg> OnSendMessageHandler;
        public void SendMessage(SendP2PMessageArg arg)
        {
            EventHandlers.Instance.SendMessage(new SendMessageEventArg
            {
                Address = arg.Address,
                Data = new MessageP2PModel
                {
                    Data = arg.Data.ProtobufSerialize(),
                    DataType = (byte)arg.Data.Type,
                    FormId = EventHandlers.Instance.ConnectId
                }
            });
            OnSendMessageHandler?.Invoke(this, arg);
        }

    }

    public class SendP2PMessageArg
    {
        public IPEndPoint Address { get; set; }

        public IP2PMessageBase Data { get; set; }
    }

    public class SendP2PTcpMessageArg
    {
        public Socket Socket { get; set; }

        public IP2PMessageBase Data { get; set; }
    }


    public class OnP2PTcpMessageArg
    {
        public MessageP2PModel Data { get; set; }
        public PluginExcuteModel Packet { get; set; }
    }
}
