using client.service.events;
using common.extends;
using server.model;
using server.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace client.service.p2pPlugins
{
    public class P2PEventHandles
    {
        private static readonly Lazy<P2PEventHandles> lazy = new(() => new P2PEventHandles());
        public static P2PEventHandles Instance => lazy.Value;
        private readonly Dictionary<P2PDataTypes, IP2PPlugin[]> plugins = null;

        private P2PEventHandles()
        {
            plugins = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IP2PPlugin)))
                .Select(c => (IP2PPlugin)Activator.CreateInstance(c)).GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }

        /// <summary>
        /// 收到P2P的UDP消息
        /// </summary>
        public event EventHandler<OnP2PTcpArg> OnP2PHandler;
        public void OnP2P(OnP2PTcpArg arg)
        {
            P2PDataTypes type = (P2PDataTypes)arg.Data.DataType;

            if (plugins.ContainsKey(type))
            {
                IP2PPlugin[] plugin = plugins[type];
                if (plugin.Length > 0)
                {
                    for (int i = 0; i < plugin.Length; i++)
                    {
                        plugin[i].Excute(arg);
                    }
                }
            }

            OnP2PHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 收到p2p的TCP消息
        /// </summary>
        public event EventHandler<OnP2PTcpArg> OnP2PTcpHandler;
        public void OnP2PTcp(OnP2PTcpArg arg)
        {
            P2PDataTypes type = (P2PDataTypes)arg.Data.DataType;

            if (plugins.ContainsKey(type))
            {
                IP2PPlugin[] plugin = plugins[type];
                if (plugin.Length > 0)
                {
                    for (int i = 0; i < plugin.Length; i++)
                    {
                        plugin[i].Excute(arg);
                    }
                }
            }

            OnP2PTcpHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 发送p2p的TCP消息
        /// </summary>
        public event EventHandler<SendP2PTcpArg> OnSendTcpHandler;
        public void SendTcp(SendP2PTcpArg arg)
        {
            EventHandlers.Instance.SendTcp(new SendTcpEventArg
            {
                Socket = arg.Socket,
                Data = new P2PModel
                {
                    Data = arg.Data.ToBytes(),
                    DataType = (byte)arg.Data.Type,
                    FormId = EventHandlers.Instance.ConnectId
                }
            });

            OnSendTcpHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 发送p2p的UDP消息
        /// </summary>
        public event EventHandler<SendP2PArg> OnSendHandler;
        public void Send(SendP2PArg arg)
        {
            EventHandlers.Instance.Send(new SendEventArg
            {
                Address = arg.Address,
                Data = new P2PModel
                {
                    Data = arg.Data.ToBytes(),
                    DataType = (byte)arg.Data.Type,
                    FormId = EventHandlers.Instance.ConnectId
                }
            });
            OnSendHandler?.Invoke(this, arg);
        }

    }

    public class SendP2PArg
    {
        public IPEndPoint Address { get; set; }

        public IP2PMessageBase Data { get; set; }
    }

    public class SendP2PTcpArg
    {
        public Socket Socket { get; set; }

        public IP2PMessageBase Data { get; set; }
    }


    public class OnP2PTcpArg
    {
        public P2PModel Data { get; set; }
        public PluginExcuteModel Packet { get; set; }
    }
}
