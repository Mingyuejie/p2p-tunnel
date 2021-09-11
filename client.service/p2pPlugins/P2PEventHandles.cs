using client.service.events;
using client.service.p2pPlugins.plugins.request;
using client.service.serverPlugins.register;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
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
        private Dictionary<P2PDataTypes, IP2PPlugin[]> plugins = null;

        private readonly EventHandlers eventHandlers;
        private readonly RegisterState registerState;

        public P2PEventHandles(EventHandlers eventHandlers, RegisterState registerState)
        {
            this.eventHandlers = eventHandlers;
            this.registerState = registerState;
        }

        public void LoadPlugins()
        {
            plugins = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IP2PPlugin)))
                .Select(c => (IP2PPlugin)Program.serviceProvider.GetService(c)).GroupBy(c => c.Type)
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
            eventHandlers.SendTcp(new SendTcpEventArg
            {
                Socket = arg.Socket,
                Data = new P2PModel
                {
                    Data = arg.Data.ToBytes(),
                    DataType = (byte)arg.Data.Type,
                    FormId = registerState.RemoteInfo.ConnectId
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
            eventHandlers.Send(new SendEventArg
            {
                Address = arg.Address,
                Data = new P2PModel
                {
                    Data = arg.Data.ToBytes(),
                    DataType = (byte)arg.Data.Type,
                    FormId = registerState.RemoteInfo.ConnectId
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
