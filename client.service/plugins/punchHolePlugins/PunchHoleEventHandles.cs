using client.plugins.serverPlugins;
using client.plugins.serverPlugins.register;
using common;
using common.extends;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace client.service.plugins.punchHolePlugins
{
    public class PunchHoleEventHandles
    {
        private Dictionary<PunchHoleTypes, IPunchHolePlugin> plugins = null;

        private readonly IServerRequest serverRequest;
        private readonly RegisterState registerState;
        private readonly ServiceProvider serviceProvider;

        public PunchHoleEventHandles(IServerRequest serverRequest, RegisterState registerState, ServiceProvider serviceProvider)
        {
            this.serverRequest = serverRequest;
            this.registerState = registerState;
            this.serviceProvider = serviceProvider;
        }

        private Socket TcpServer => registerState.TcpSocket;

        public void LoadPlugins()
        {
            LoadPlugins(AppDomain.CurrentDomain.GetAssemblies());
        }
        public void LoadPlugins(Assembly[] assemblys)
        {
            if (plugins == null)
            {
                plugins = new Dictionary<PunchHoleTypes, IPunchHolePlugin>();
            }

            var types = assemblys
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IPunchHolePlugin)));
            foreach (var item in types)
            {
                IPunchHolePlugin obj = (IPunchHolePlugin)serviceProvider.GetService(item);
                if (!plugins.ContainsKey(obj.Type))
                {
                    plugins.Add(obj.Type, obj);
                }
            }
        }

        public void OnPunchHole(OnPunchHoleTcpArg arg)
        {
            PunchHoleTypes type = (PunchHoleTypes)arg.Data.PunchType;

            if (plugins.ContainsKey(type))
            {
                IPunchHolePlugin plugin = plugins[type];
                plugin?.Excute(arg);
            }
        }
        public void OnPunchHoleTcp(OnPunchHoleTcpArg arg)
        {
            PunchHoleTypes type = (PunchHoleTypes)arg.Data.PunchType;

            if (plugins.ContainsKey(type))
            {
                IPunchHolePlugin plugin = plugins[type];
                plugin?.Excute(arg);
            }
        }

        public void SendTcp<T>(SendPunchHoleTcpArg<T> arg)
        {
            var msg = ((IPunchHoleMessageBase)arg.Data);
            serverRequest.SendOnlyTcp(new SendTcpEventArg<PunchHoleModel>
            {
                Socket = arg.Socket,
                Path = "punchhole/excute",
                Data = new PunchHoleModel
                {
                    Data = arg.Data.ToBytes(),
                    PunchForwardType = msg.PunchForwardType,
                    Id = registerState.RemoteInfo.ConnectId,
                    PunchStep = msg.PunchStep,
                    PunchType = (short)msg.PunchType,
                    ToId = arg.ToId
                }
            });
        }
        public void Send<T>(SendPunchHoleArg<T> arg)
        {
            var msg = ((IPunchHoleMessageBase)arg.Data);
            serverRequest.SendOnly(new SendEventArg<PunchHoleModel>
            {
                Address = arg.Address,
                Path = "punchhole/excute",
                Data = new PunchHoleModel
                {
                    Data = arg.Data.ToBytes(),
                    PunchForwardType = msg.PunchForwardType,
                    Id = registerState.RemoteInfo.ConnectId,
                    PunchStep = msg.PunchStep,
                    PunchType = (short)msg.PunchType,
                    ToId = arg.ToId
                }
            });
        }

        public SimplePushSubHandler<OnPunchHoleTcpArg> OnReverse { get; } = new SimplePushSubHandler<OnPunchHoleTcpArg>();
        public void SendReverse(long toid)
        {
            SendTcp(new SendPunchHoleTcpArg<ReverseModel>
            {
                Socket = TcpServer,
                ToId = toid,
                Data = new ReverseModel { FromId = registerState.RemoteInfo.ConnectId, }
            });
        }

    }

    public class SendPunchHoleArg<T>
    {
        public IPEndPoint Address { get; set; }

        public long ToId { get; set; }

        public T Data { get; set; }
    }

    public class SendPunchHoleTcpArg<T>
    {
        public Socket Socket { get; set; }

        public long ToId { get; set; }

        public T Data { get; set; } = default;
    }


    public class OnPunchHoleTcpArg
    {
        public PunchHoleModel Data { get; set; }
        public PluginParamWrap Packet { get; set; }
    }


    [ProtoContract, MessagePackObject]
    public class ReverseModel : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1),Key(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.REVERSE;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4), Key(4)]
        public short PunchStep { get; } = 0;
    }
}
