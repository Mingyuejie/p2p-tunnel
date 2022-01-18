using client.plugins.serverPlugins;
using client.plugins.serverPlugins.register;
using common;
using common.extends;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using server;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

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

            IEnumerable<Type> types = assemblys
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IPunchHolePlugin)));
            foreach (Type item in types)
            {
                IPunchHolePlugin obj = (IPunchHolePlugin)serviceProvider.GetService(item);
                if (!plugins.ContainsKey(obj.Type))
                {
                    plugins.Add(obj.Type, obj);
                }
            }
        }

        public void OnPunchHole(OnPunchHoleArg arg)
        {
            PunchHoleTypes type = (PunchHoleTypes)arg.Data.PunchType;

            if (plugins.ContainsKey(type))
            {
                IPunchHolePlugin plugin = plugins[type];
                plugin?.Execute(arg);
            }
        }

        public async Task Send<T>(SendPunchHoleArg<T> arg)
        {
            IPunchHoleMessageBase msg = ((IPunchHoleMessageBase)arg.Data);
            await serverRequest.SendOnly(new SendArg<PunchHoleModel>
            {
                Connection = arg.Connection,
                Path = "punchhole/Execute",
                Data = new PunchHoleModel
                {
                    Data = arg.Data.ToBytes(),
                    PunchForwardType = msg.PunchForwardType,
                    FromId = 0,
                    PunchStep = msg.PunchStep,
                    PunchType = (byte)msg.PunchType,
                    ToId = arg.ToId
                }
            });
        }

        public SimpleSubPushHandler<OnPunchHoleArg> OnReverse { get; } = new SimpleSubPushHandler<OnPunchHoleArg>();
        public async Task SendReverse(ulong toid)
        {
            await Send(new SendPunchHoleArg<ReverseModel>
            {
                Connection = registerState.TcpConnection,
                ToId = toid,
                Data = new ReverseModel { }
            });
        }

    }

    public class SendPunchHoleArg<T>
    {
        public IConnection Connection { get; set; }

        public ulong ToId { get; set; }

        public T Data { get; set; }
    }

    public class OnPunchHoleArg
    {
        public PunchHoleModel Data { get; set; }
        public IConnection Connection { get; set; }
    }

    [ProtoContract, MessagePackObject]
    public class ReverseModel : IPunchHoleMessageBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.REVERSE;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(3), Key(3)]
        public byte PunchStep { get; } = 0;
    }
}
