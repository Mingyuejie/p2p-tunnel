using client.service.events;
using client.service.p2pPlugins.plugins.request;
using client.service.serverPlugins.register;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using server.model;
using server.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace client.service.punchHolePlugins
{
    public class PunchHoleEventHandles
    {
        private Dictionary<PunchHoleTypes, IPunchHolePlugin[]> plugins = null;

        private readonly EventHandlers eventHandlers;
        private readonly RegisterState registerState;

        public PunchHoleEventHandles(EventHandlers eventHandlers, RegisterState registerState)
        {
            this.eventHandlers = eventHandlers;
            this.registerState = registerState;
        }

        private Socket TcpServer => registerState.TcpSocket;

        public void LoadPlugins()
        {
            plugins = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IPunchHolePlugin)))
                .Where(c=>c != null).Select(c => (IPunchHolePlugin)Program.serviceProvider.GetService(c)).GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public event EventHandler<OnPunchHoleTcpArg> OnPunchHoleHandler;
        public void OnPunchHole(OnPunchHoleTcpArg arg)
        {
            PunchHoleTypes type = (PunchHoleTypes)arg.Data.PunchType;

            if (plugins.ContainsKey(type))
            {
                IPunchHolePlugin[] plugin = plugins[type];
                if (plugin.Length > 0)
                {
                    for (int i = 0; i < plugin.Length; i++)
                    {
                        plugin[i].Excute(arg);
                    }
                }
            }

            OnPunchHoleHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 收到p2p的TCP消息
        /// </summary>
        public event EventHandler<OnPunchHoleTcpArg> OnPunchHoleTcpHandler;
        public void OnPunchHoleTcp(OnPunchHoleTcpArg arg)
        {
            PunchHoleTypes type = (PunchHoleTypes)arg.Data.PunchType;

            if (plugins.ContainsKey(type))
            {
                IPunchHolePlugin[] plugin = plugins[type];
                if (plugin.Length > 0)
                {
                    for (int i = 0; i < plugin.Length; i++)
                    {
                        plugin[i].Excute(arg);
                    }
                }
            }

            OnPunchHoleTcpHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 发送p2p的TCP消息
        /// </summary>
        public event EventHandler<SendPunchHoleTcpArg> OnSendTcpHandler;
        public void SendTcp(SendPunchHoleTcpArg arg)
        {
            eventHandlers.SendTcp(new SendTcpEventArg
            {
                Socket = arg.Socket,
                Data = new PunchHoleModel
                {
                    Data = arg.Data.ToBytes(),
                    PunchForwardType = arg.Data.PunchForwardType,
                    Id = registerState.RemoteInfo.ConnectId,
                    PunchStep = arg.Data.PunchStep,
                    PunchType = (short)arg.Data.PunchType,
                    ToId = arg.ToId
                }
            });

            OnSendTcpHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 发送p2p的UDP消息
        /// </summary>
        public event EventHandler<SendPunchHoleArg> OnSendHandler;
        public void Send(SendPunchHoleArg arg)
        {
            eventHandlers.Send(new SendEventArg
            {
                Address = arg.Address,
                Data = new PunchHoleModel
                {
                    Data = arg.Data.ToBytes(),
                    PunchForwardType = arg.Data.PunchForwardType,
                    Id = registerState.RemoteInfo.ConnectId,
                    PunchStep = arg.Data.PunchStep,
                    PunchType = (short)arg.Data.PunchType,
                    ToId = arg.ToId
                }
            });
            OnSendHandler?.Invoke(this, arg);
        }


        public event EventHandler<OnPunchHoleTcpArg> OnReverseHandler;
        public void OnReverse(OnPunchHoleTcpArg arg)
        {
            OnReverseHandler?.Invoke(this, arg);
        }
        public void SendReverse(long toid)
        {
            SendTcp(new SendPunchHoleTcpArg
            {
                Socket = TcpServer,
                ToId = toid,
                Data = new ReverseModel { FromId = registerState.RemoteInfo.ConnectId, }
            });
        }

    }

    public class SendPunchHoleArg
    {
        public IPEndPoint Address { get; set; }

        public long ToId { get; set; }

        public IPunchHoleMessageBase Data { get; set; }
    }

    public class SendPunchHoleTcpArg
    {
        public Socket Socket { get; set; }

        public long ToId { get; set; }

        public IPunchHoleMessageBase Data { get; set; } = default;
    }


    public class OnPunchHoleTcpArg
    {
        public PunchHoleModel Data { get; set; }
        public PluginExcuteModel Packet { get; set; }
    }


    [ProtoContract]
    public class ReverseModel : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.REVERSE;

        [ProtoMember(3, IsRequired = true)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4)]
        public short PunchStep { get; } = 0;
    }
}
