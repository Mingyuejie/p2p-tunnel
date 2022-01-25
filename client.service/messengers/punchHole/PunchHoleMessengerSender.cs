using client.messengers.punchHole;
using client.messengers.register;
using common;
using common.extends;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using server;
using server.model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace client.service.messengers.punchHole
{
    public class PunchHoleMessengerSender
    {
        private Dictionary<PunchHoleTypes, IPunchHole> plugins = new Dictionary<PunchHoleTypes, IPunchHole>();

        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerState;
        private readonly ServiceProvider serviceProvider;

        public PunchHoleMessengerSender(MessengerSender messengerSender, RegisterStateInfo registerState, ServiceProvider serviceProvider)
        {
            this.messengerSender = messengerSender;
            this.registerState = registerState;
            this.serviceProvider = serviceProvider;
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IPunchHole)))
            {
                IPunchHole obj = (IPunchHole)serviceProvider.GetService(item);
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
                IPunchHole plugin = plugins[type];
                plugin?.Execute(arg);
            }
        }

        public async Task Send<T>(SendPunchHoleArg<T> arg)
        {
            IPunchHoleStepInfo msg = ((IPunchHoleStepInfo)arg.Data);
            await messengerSender.SendOnly(new MessageRequestParamsInfo<PunchHoleParamsInfo>
            {
                Connection = arg.Connection,
                Path = "punchhole/Execute",
                Data = new PunchHoleParamsInfo
                {
                    Data = arg.Data.ToBytes(),
                    PunchForwardType = msg.PunchForwardType,
                    FromId = 0,
                    PunchStep = msg.PunchStep,
                    PunchType = (byte)msg.PunchType,
                    ToId = arg.ToId,
                    TunnelName = arg.TunnelName
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
        public string TunnelName { get; set; }

        public T Data { get; set; }
    }

    [MessagePackObject]
    public class ReverseModel : IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.REVERSE;

        [Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(3)]
        public byte PunchStep { get; } = 0;
    }
}
