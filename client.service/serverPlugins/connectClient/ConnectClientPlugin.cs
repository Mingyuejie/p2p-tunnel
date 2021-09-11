using common.extends;
using server.model;
using server.plugin;

namespace client.service.serverPlugins.connectClient
{

    public class ConnectClientReversePlugin : IPlugin
    {
        private readonly ConnectClientEventHandles connectClientEventHandles;
        public ConnectClientReversePlugin(ConnectClientEventHandles connectClientEventHandles)
        {
            this.connectClientEventHandles = connectClientEventHandles;
        }

        public MessageTypes MsgType => MessageTypes.SERVER_P2P_REVERSE;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            ConnectClientReverseModel data = model.Packet.Chunk.DeBytes<ConnectClientReverseModel>();
            connectClientEventHandles.OnConnectClientReverse(new OnConnectClientReverseEventArg
            {
                Data = data,
                Packet = model,
            });
        }
    }

    public class ConnectClientStep1Plugin : IPlugin
    {
        private readonly ConnectClientEventHandles connectClientEventHandles;
        public ConnectClientStep1Plugin(ConnectClientEventHandles connectClientEventHandles)
        {
            this.connectClientEventHandles = connectClientEventHandles;
        }

        public MessageTypes MsgType => MessageTypes.P2P_STEP_1;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            ConnectClientStep1Model data = model.Packet.Chunk.DeBytes<ConnectClientStep1Model>();
            if (serverType == ServerType.UDP)
            {
                connectClientEventHandles.OnConnectClientStep1(new OnConnectClientStep1EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
            else if (serverType == ServerType.TCP)
            {
                connectClientEventHandles.OnTcpConnectClientStep1(new OnConnectClientStep1EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
        }
    }


    public class ConnectClientStep2Plugin : IPlugin
    {
        private readonly ConnectClientEventHandles connectClientEventHandles;
        public ConnectClientStep2Plugin(ConnectClientEventHandles connectClientEventHandles)
        {
            this.connectClientEventHandles = connectClientEventHandles;
        }

        public MessageTypes MsgType => MessageTypes.P2P_STEP_2;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            ConnectClientStep2Model data = model.Packet.Chunk.DeBytes<ConnectClientStep2Model>();

            if (serverType == ServerType.UDP)
            {
                connectClientEventHandles.OnConnectClientStep2(new OnConnectClientStep2EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
            else if (serverType == ServerType.TCP)
            {
                connectClientEventHandles.OnTcpConnectClientStep2(new OnConnectClientStep2EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
        }
    }

    public class ConnectClientStep2RetryPlugin : IPlugin
    {
        private readonly ConnectClientEventHandles connectClientEventHandles;
        public ConnectClientStep2RetryPlugin(ConnectClientEventHandles connectClientEventHandles)
        {
            this.connectClientEventHandles = connectClientEventHandles;
        }

        public MessageTypes MsgType => MessageTypes.SERVER_P2P_STEP_2_RETRY;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            if (serverType == ServerType.TCP)
            {
                ConnectClientStep2RetryModel data = model.Packet.Chunk.DeBytes<ConnectClientStep2RetryModel>();
                connectClientEventHandles.OnTcpConnectClientStep2Retry(new OnConnectClientStep2RetryEventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
        }
    }

    public class ConnectClientStep2FailPlugin : IPlugin
    {
        private readonly ConnectClientEventHandles connectClientEventHandles;
        public ConnectClientStep2FailPlugin(ConnectClientEventHandles connectClientEventHandles)
        {
            this.connectClientEventHandles = connectClientEventHandles;
        }

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
                connectClientEventHandles.OnTcpConnectClientStep2Fail(new OnTcpConnectClientStep2FailEventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
        }
    }

    public class ConnectClientStep3Plugin : IPlugin
    {
        private readonly ConnectClientEventHandles connectClientEventHandles;
        public ConnectClientStep3Plugin(ConnectClientEventHandles connectClientEventHandles)
        {
            this.connectClientEventHandles = connectClientEventHandles;
        }

        public MessageTypes MsgType => MessageTypes.P2P_STEP_3;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            ConnectClientStep3Model data = model.Packet.Chunk.DeBytes<ConnectClientStep3Model>();

            if (serverType == ServerType.UDP)
            {
                connectClientEventHandles.OnConnectClientStep3(new OnConnectClientStep3EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
            else if (serverType == ServerType.TCP)
            {
                connectClientEventHandles.OnTcpConnectClientStep3(new OnConnectClientStep3EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
        }
    }

    public class ConnectClientStep4Plugin : IPlugin
    {
        private readonly ConnectClientEventHandles connectClientEventHandles;
        public ConnectClientStep4Plugin(ConnectClientEventHandles connectClientEventHandles)
        {
            this.connectClientEventHandles = connectClientEventHandles;
        }

        public MessageTypes MsgType => MessageTypes.P2P_STEP_4;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            ConnectClientStep4Model data = model.Packet.Chunk.DeBytes<ConnectClientStep4Model>();

            if (serverType == ServerType.UDP)
            {
                connectClientEventHandles.OnConnectClientStep4(new OnConnectClientStep4EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
            else if (serverType == ServerType.TCP)
            {
                connectClientEventHandles.OnTcpConnectClientStep4(new OnConnectClientStep4EventArg
                {
                    Data = data,
                    Packet = model,
                });
            }
        }
    }


    public class ConnectClientStep2StopPlugin : IPlugin
    {
        private readonly ConnectClientEventHandles connectClientEventHandles;
        public ConnectClientStep2StopPlugin(ConnectClientEventHandles connectClientEventHandles)
        {
            this.connectClientEventHandles = connectClientEventHandles;
        }

        public MessageTypes MsgType => MessageTypes.SERVER_P2P_STEP_2_STOP;

        public void Excute(PluginExcuteModel data, ServerType serverType)
        {
            ConnectClientStep2StopModel model = data.Packet.Chunk.DeBytes<ConnectClientStep2StopModel>();

            connectClientEventHandles.OnTcpConnectClientStep2StopMessage(model);
        }
    }
}
