using common.extends;
using server.model;
using server.plugin;

namespace client.service.serverPlugins.connectClient
{

    public class ConnectClientReversePlugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.SERVER_P2P_REVERSE;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            ConnectClientReverseModel data = model.Packet.Chunk.DeBytes<ConnectClientReverseModel>();
            ConnectClientEventHandles.Instance.OnConnectClientReverse(new OnConnectClientReverseEventArg
            {
                Data = data,
                Packet = model,
            });
        }
    }

    public class ConnectClientStep1Plugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.P2P_STEP_1;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            ConnectClientStep1Model data = model.Packet.Chunk.DeBytes<ConnectClientStep1Model>();
            if (serverType == ServerType.UDP)
            {
                ConnectClientEventHandles.Instance.OnConnectClientStep1(new OnConnectClientStep1EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
            else if (serverType == ServerType.TCP)
            {
                ConnectClientEventHandles.Instance.OnTcpConnectClientStep1(new OnConnectClientStep1EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
        }
    }


    public class ConnectClientStep2Plugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.P2P_STEP_2;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            ConnectClientStep2Model data = model.Packet.Chunk.DeBytes<ConnectClientStep2Model>();

            if (serverType == ServerType.UDP)
            {
                ConnectClientEventHandles.Instance.OnConnectClientStep2(new OnConnectClientStep2EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
            else if (serverType == ServerType.TCP)
            {
                ConnectClientEventHandles.Instance.OnTcpConnectClientStep2(new OnConnectClientStep2EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
        }
    }

    public class ConnectClientStep2RetryPlugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.SERVER_P2P_STEP_2_RETRY;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            if (serverType == ServerType.TCP)
            {
                ConnectClientStep2RetryModel data = model.Packet.Chunk.DeBytes<ConnectClientStep2RetryModel>();
                ConnectClientEventHandles.Instance.OnTcpConnectClientStep2Retry(new OnConnectClientStep2RetryEventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
        }
    }

    public class ConnectClientStep2FailPlugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.SERVER_P2P_STEP_2_FAIL;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            ConnectClientStep2FailModel data = model.Packet.Chunk.DeBytes<ConnectClientStep2FailModel>();

            if (serverType == ServerType.UDP)
            {
                //EventHandlers.Instance.OnConnectClientStep2FailMessage(new OnConnectClientStep2FailEventArg
                //{
                //    Data = data,
                //    Packet = model,
                //});
            }
            else if (serverType == ServerType.TCP)
            {
                ConnectClientEventHandles.Instance.OnTcpConnectClientStep2Fail(new OnTcpConnectClientStep2FailEventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
        }
    }

    public class ConnectClientStep3Plugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.P2P_STEP_3;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            ConnectClientStep3Model data = model.Packet.Chunk.DeBytes<ConnectClientStep3Model>();

            if (serverType == ServerType.UDP)
            {
                ConnectClientEventHandles.Instance.OnConnectClientStep3(new OnConnectClientStep3EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
            else if (serverType == ServerType.TCP)
            {
                ConnectClientEventHandles.Instance.OnTcpConnectClientStep3(new OnConnectClientStep3EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
        }
    }

    public class ConnectClientStep4Plugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.P2P_STEP_4;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            ConnectClientStep4Model data = model.Packet.Chunk.DeBytes<ConnectClientStep4Model>();

            if (serverType == ServerType.UDP)
            {
                ConnectClientEventHandles.Instance.OnConnectClientStep4(new OnConnectClientStep4EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
            else if (serverType == ServerType.TCP)
            {
                ConnectClientEventHandles.Instance.OnTcpConnectClientStep4(new OnConnectClientStep4EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
        }
    }


    public class ConnectClientStep2StopPlugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.SERVER_P2P_STEP_2_STOP;

        public void Excute(PluginExcuteModel data, ServerType serverType)
        {
            ConnectClientStep2StopModel model = data.Packet.Chunk.DeBytes<ConnectClientStep2StopModel>();

            ConnectClientEventHandles.Instance.OnTcpConnectClientStep2StopMessage(model);
        }
    }
}
