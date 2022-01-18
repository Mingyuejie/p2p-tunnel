using common;
using MessagePack;
using ProtoBuf;
using server;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace client.service.plugins.punchHolePlugins.plugins.tcp
{
    public interface IPunchHoleTcp
    {
        public Task<ConnectResultModel> Send(ConnectTcpParams param);

        public SimpleSubPushHandler<OnStep1EventArg> OnStep1Handler { get; }
        public Task OnStep1(OnStep1EventArg e);

        public SimpleSubPushHandler<OnStep2EventArg> OnStep2Handler { get; }
        public Task OnStep2(OnStep2EventArg e);

        public SimpleSubPushHandler<OnStep2RetryEventArg> OnStep2RetryHandler { get; }
        public Task OnStep2Retry(OnStep2RetryEventArg e);

        public SimpleSubPushHandler<OnStep2FailEventArg> OnStep2FailHandler { get; }
        public Task OnStep2Fail(OnStep2FailEventArg arg);

        public Task SendStep2Stop(ulong toid);
        public Task OnStep2Stop(OnStep2StopEventArg e);

        public SimpleSubPushHandler<OnStep3EventArg> OnStep3Handler { get; }
        public Task OnStep3(OnStep3EventArg arg);

        public SimpleSubPushHandler<OnStep4EventArg> OnStep4Handler { get; }
        public Task OnStep4(OnStep4EventArg arg);
    }


    public class OnStep1EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleModel RawData { get; set; }
        public PunchHoleNotifyModel Data { get; set; }
    }
    public class OnStep2EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleModel RawData { get; set; }
        public PunchHoleNotifyModel Data { get; set; }
    }

    public class OnStep2RetryEventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleModel RawData { get; set; }
        public PunchHoleNotifyModel Data { get; set; }
    }
    public class SendStep2FailEventArg : EventArgs
    {
        public ulong ToId { get; set; }
        public ulong Id { get; set; }
    }
    public class OnStep2FailEventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleModel RawData { get; set; }
        public Step2FailModel Data { get; set; }
    }
    public class OnStep2StopEventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleModel RawData { get; set; }
        public Step2StopModel Data { get; set; }
    }

    public class OnStep3EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleModel RawData { get; set; }
        public Step3Model Data { get; set; }
    }
    public class OnStep4EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleModel RawData { get; set; }
        public Step4Model Data { get; set; }
    }

    [ProtoContract, MessagePackObject]
    public class Step1Model : IPunchHoleMessageBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(3), Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_1;
    }

    [ProtoContract, MessagePackObject]
    public class Step2Model : IPunchHoleMessageBase
    {

        [ProtoMember(1, IsRequired = true), Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(3), Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_2;
    }

    [ProtoContract, MessagePackObject]
    public class Step2FailModel : IPunchHoleMessageBase
    {

        [ProtoMember(1, IsRequired = true), Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(3), Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_FAIL;
    }
    [ProtoContract, MessagePackObject]
    public class Step2TryModel : IPunchHoleMessageBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(3), Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_TRY;
    }

    [ProtoContract, MessagePackObject]
    public class Step2StopModel : IPunchHoleMessageBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(3), Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_STOP;
    }

    [ProtoContract, MessagePackObject]
    public class Step3Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1),Key(1)]
        public ulong FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4), Key(4)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_3;
    }

    [ProtoContract, MessagePackObject]
    public class Step4Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1),Key(1)]
        public ulong FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4), Key(4)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_4;
    }

    [ProtoContract, MessagePackObject]
    public class StepPacketModel : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1),Key(1)]
        public ulong FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4), Key(4)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_PACKET;

        [ProtoMember(5), Key(5)]
        public byte Live { get; } = 1;
    }

    public enum PunchHoleTcpNutssBSteps : byte
    {
        STEP_1 = 1,
        STEP_2 = 2,
        STEP_2_TRY = 3,
        STEP_2_FAIL = 4,
        STEP_2_STOP = 5,
        STEP_3 = 6,
        STEP_4 = 7,
        STEP_PACKET = 8,
    }


    public class ConnectTcpParams
    {
        public ulong Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public int TryTimes { get; set; } = 5;
        //public Action<OnStep4EventArg> Callback { get; set; } = null;
       // public Action<ConnectFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectTcpCache
    {
        public int TryTimes { get; set; } = 5;
        public bool Canceled { get; set; } = false;
        public TaskCompletionSource<ConnectResultModel> Tcs { get; set; }

        //public Action<OnStep4EventArg> Callback { get; set; } = null;
        //public Action<ConnectFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectFailModel
    {
        public ConnectFailType Type { get; set; } = ConnectFailType.ERROR;
        public string Msg { get; set; } = string.Empty;
    }
    public class ConnectResultModel
    {
        public bool State { get; set; }
        public object Result { get; set; }
    }


    public enum ConnectFailType
    {
        ERROR, TIMEOUT,CANCEL
    }
}
