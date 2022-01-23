using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using common.extends;
using server.model;

namespace client.service.messengers.punchHole.tcp.nutssb
{
    public class PunchHoleTcpNutssB : IPunchHole
    {
        private readonly IPunchHoleTcp punchHoleTcp;
        public PunchHoleTcpNutssB(IPunchHoleTcp punchHoleTcp)
        {
            this.punchHoleTcp = punchHoleTcp;
        }

        public PunchHoleTypes Type => PunchHoleTypes.TCP_NUTSSB;

        public void Execute(OnPunchHoleArg arg)
        {
            if (arg.Connection.ServerType != ServerType.TCP) return;

            PunchHoleTcpNutssBSteps step = (PunchHoleTcpNutssBSteps)arg.Data.PunchStep;
            switch (step)
            {
                case PunchHoleTcpNutssBSteps.STEP_1:
                    Step1(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2:
                    Step2(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_TRY:
                    Step2Try(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_FAIL:
                    Step2Fail(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_STOP:
                    Step2Stop(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_3:
                    Step3(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_4:
                    Step4(arg);
                    break;
                default:
                    break;
            }
        }

        private void Step1(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep1(new OnStep1EventArg
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyInfo>()
            });
        }
        private void Step2(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep2(new OnStep2EventArg
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyInfo>()
            });
        }
        private void Step2Try(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep2Retry(new OnStep2RetryEventArg
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyInfo>()
            });
        }
        private void Step2Fail(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep2Fail(new OnStep2FailEventArg
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<Step2FailModel>()
            });
        }
        private void Step2Stop(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep2Stop(new OnStep2StopEventArg
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<Step2StopModel>()
            });
        }

        private void Step3(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep3(new OnStep3EventArg
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<Step3Model>()
            });
        }
        private void Step4(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep4(new OnStep4EventArg
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<Step4Model>()
            });
        }
    }


}
