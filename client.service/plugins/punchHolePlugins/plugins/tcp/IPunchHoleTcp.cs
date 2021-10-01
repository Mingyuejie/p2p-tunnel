using common;
using MessagePack;
using ProtoBuf;
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
        public void Send(ConnectTcpParams param);

        public SimplePushSubHandler<OnStep1EventArg> OnStep1Handler { get; }
        public void OnStep1(OnStep1EventArg e);

        public SimplePushSubHandler<OnStep2EventArg> OnStep2Handler { get; }
        public void OnStep2(OnStep2EventArg e);

        public void SendStep2Retry(long toid);

        public SimplePushSubHandler<OnStep2RetryEventArg> OnStep2RetryHandler { get; }
        public void OnStep2Retry(OnStep2RetryEventArg e);

        public void SendStep2Fail(OnSendStep2FailEventArg arg);

        public SimplePushSubHandler<OnStep2FailEventArg> OnStep2FailHandler { get; }
        public void OnStep2Fail(OnStep2FailEventArg arg);

        public void SendStep2Stop(long toid);
        public void OnStep2Stop(Step2StopModel e);

        public void SendStep3(SendStep3EventArg arg);
        public SimplePushSubHandler<OnStep3EventArg> OnStep3Handler { get; }
        public void OnStep3(OnStep3EventArg arg);

        public void SendStep4(SendStep4EventArg arg);
        public SimplePushSubHandler<OnStep4EventArg> OnStep4Handler { get; }
        public void OnStep4(OnStep4EventArg arg);

        public void SendStepPacket(SendStepPacketEventArg arg);
        public SimplePushSubHandler<OnStepPacketEventArg> OnStepPacketHandler { get; }
        public void OnStepPacket(OnStepPacketEventArg arg);
    }


    public class SendEventArg : EventArgs
    {
        public string Name { get; set; }
        public long Id { get; set; }
    }

    public class OnStep1EventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public PunchHoleNotifyModel Data { get; set; }
    }
    public class OnStep2EventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public PunchHoleNotifyModel Data { get; set; }
    }

    public class OnStep2RetryEventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public PunchHoleNotifyModel Data { get; set; }
    }
    public class OnSendStep2FailEventArg : EventArgs
    {
        public long ToId { get; set; }
        public long Id { get; set; }
    }
    public class OnStep2FailEventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public Step2FailModel Data { get; set; }
    }
    public class SendStep3EventArg : EventArgs
    {
        public Socket Socket { get; set; }
        public long ToId { get; set; }
    }
    public class OnStep3EventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public Step3Model Data { get; set; }
    }
    public class SendStep4EventArg : EventArgs
    {
        public Socket Socket { get; set; }
        public long ToId { get; set; }
    }
    public class OnStep4EventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public Step4Model Data { get; set; }
    }


    public class SendStepPacketEventArg : EventArgs
    {
        public Socket Socket { get; set; }
        public long ToId { get; set; }
        public long FromId { get; set; }
    }
    public class OnStepPacketEventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public StepPacketModel Data { get; set; }
    }


    [ProtoContract, MessagePackObject]
    public class Step1Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1),Key(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(4), Key(4)]
        public short PunchStep { get; } = (short)PunchHoleTcpNutssBSteps.STEP_1;
    }

    [ProtoContract, MessagePackObject]
    public class Step2Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1), Key(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(4), Key(4)]
        public short PunchStep { get; } = (short)PunchHoleTcpNutssBSteps.STEP_2;
    }

    [ProtoContract, MessagePackObject]
    public class Step2FailModel : IPunchHoleMessageBase
    {
        [ProtoMember(1), Key(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4), Key(4)]
        public short PunchStep { get; } = (short)PunchHoleTcpNutssBSteps.STEP_2_FAIL;
    }
    [ProtoContract, MessagePackObject]
    public class Step2TryModel : IPunchHoleMessageBase
    {
        [ProtoMember(1),Key(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(4), Key(4)]
        public short PunchStep { get; } = (short)PunchHoleTcpNutssBSteps.STEP_2_TRY;
    }

    [ProtoContract, MessagePackObject]
    public class Step2StopModel : IPunchHoleMessageBase
    {
        [ProtoMember(1),Key(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4), Key(4)]
        public short PunchStep { get; } = (short)PunchHoleTcpNutssBSteps.STEP_2_STOP;
    }

    [ProtoContract, MessagePackObject]
    public class Step3Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1),Key(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4), Key(4)]
        public short PunchStep { get; } = (short)PunchHoleTcpNutssBSteps.STEP_3;
    }

    [ProtoContract, MessagePackObject]
    public class Step4Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1),Key(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4), Key(4)]
        public short PunchStep { get; } = (short)PunchHoleTcpNutssBSteps.STEP_4;
    }

    [ProtoContract, MessagePackObject]
    public class StepPacketModel : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1),Key(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4), Key(4)]
        public short PunchStep { get; } = (short)PunchHoleTcpNutssBSteps.STEP_PACKET;

        [ProtoMember(5), Key(5)]
        public byte Live { get; } = 1;
    }

    public enum PunchHoleTcpNutssBSteps : short
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
        public long Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public int Timeout { get; set; } = 15 * 1000;
        public Action<OnStep4EventArg> Callback { get; set; } = null;
        public Action<ConnectFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectTcpCache
    {
        public long Time { get; set; } = 0;
        public int TryTimes { get; set; } = 10;
        public int Timeout { get; set; } = 15 * 1000;
        public Action<OnStep4EventArg> Callback { get; set; } = null;
        public Action<ConnectFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectFailModel
    {
        public ConnectFailType Type { get; set; } = ConnectFailType.ERROR;
        public string Msg { get; set; } = string.Empty;
    }

    public enum ConnectFailType
    {
        ERROR, TIMEOUT
    }
}
