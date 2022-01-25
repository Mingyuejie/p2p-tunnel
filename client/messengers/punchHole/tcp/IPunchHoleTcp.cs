using common;
using MessagePack;
using server;
using server.model;
using System;
using System.Threading.Tasks;

namespace client.messengers.punchHole.tcp
{
    public interface IPunchHoleTcp
    {
        public SimpleSubPushHandler<ConnectTcpParams> OnSendHandler { get; }
        public Task<ConnectTcpResultModel> Send(ConnectTcpParams param);

        public SimpleSubPushHandler<OnStep1EventArg> OnStep1Handler { get; }
        public Task OnStep1(OnStep1EventArg e);

        public SimpleSubPushHandler<OnStep2EventArg> OnStep2Handler { get; }
        public Task OnStep2(OnStep2EventArg e);

        public SimpleSubPushHandler<OnStep2RetryEventArg> OnStep2RetryHandler { get; }
        public Task OnStep2Retry(OnStep2RetryEventArg e);

        public SimpleSubPushHandler<ulong> OnSendStep2FailHandler { get; }
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
        public PunchHoleParamsInfo RawData { get; set; }
        public PunchHoleNotifyInfo Data { get; set; }
    }
    public class OnStep2EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleParamsInfo RawData { get; set; }
        public PunchHoleNotifyInfo Data { get; set; }
    }

    public class OnStep2RetryEventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleParamsInfo RawData { get; set; }
        public PunchHoleNotifyInfo Data { get; set; }
    }
    public class SendStep2FailEventArg : EventArgs
    {
        public ulong ToId { get; set; }
        public ulong Id { get; set; }
        public string TunnelName { get; set; }
    }
    public class OnStep2FailEventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleParamsInfo RawData { get; set; }
        public Step2FailModel Data { get; set; }
    }
    public class OnStep2StopEventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleParamsInfo RawData { get; set; }
        public Step2StopModel Data { get; set; }
    }

    public class OnStep3EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleParamsInfo RawData { get; set; }
        public Step3Model Data { get; set; }
    }
    public class OnStep4EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleParamsInfo RawData { get; set; }
        public Step4Model Data { get; set; }
    }

    [MessagePackObject]
    public class Step1Model : IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_1;
    }

    [MessagePackObject]
    public class Step2Model : IPunchHoleStepInfo
    {

        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_2;
    }

    [MessagePackObject]
    public class Step2FailModel : IPunchHoleStepInfo
    {

        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_FAIL;
    }
    [MessagePackObject]
    public class Step2TryModel : IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_TRY;
    }

    [MessagePackObject]
    public class Step2StopModel : IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_STOP;
    }

    [MessagePackObject]
    public class Step3Model : IPunchHoleStepInfo
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [Key(1)]
        public ulong FromId { get; set; } = 0;

        [Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(4)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_3;
    }

    [MessagePackObject]
    public class Step4Model : IPunchHoleStepInfo
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [Key(1)]
        public ulong FromId { get; set; } = 0;

        [Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(4)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_4;
    }

    [MessagePackObject]
    public class StepPacketModel : IPunchHoleStepInfo
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [Key(1)]
        public ulong FromId { get; set; } = 0;

        [Key(2)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(4)]
        public byte PunchStep { get; } = (byte)PunchHoleTcpNutssBSteps.STEP_PACKET;

        [Key(5)]
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
        public string TunnelName { get; set; } = string.Empty;
        public int TryTimes { get; set; } = 5;
    }

    public class ConnectTcpCache
    {
        public int TryTimes { get; set; } = 5;
        public bool Canceled { get; set; } = false;
        public TaskCompletionSource<ConnectTcpResultModel> Tcs { get; set; }
        public string TunnelName { get; set; }

        //public Action<OnStep4EventArg> Callback { get; set; } = null;
        //public Action<ConnectFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectTcpFailModel
    {
        public ConnectTcpFailType Type { get; set; } = ConnectTcpFailType.ERROR;
        public string Msg { get; set; } = string.Empty;
    }
    public class ConnectTcpResultModel
    {
        public bool State { get; set; }
        public object Result { get; set; }
    }

    [Flags]
    public enum ConnectTcpFailType
    {
        ERROR, TIMEOUT, CANCEL
    }
}
