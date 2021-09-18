using server.model;
using System;

namespace client.service.plugins.serverPlugins.clients
{
    public class ClientsEventHandles
    {
        public ClientsEventHandles()
        {

        }

        /// <summary>
        /// 服务器发来的客户端列表数据
        /// </summary>
        public event EventHandler<OnServerSendClientsEventArg> OnServerSendClientsHandler;
        /// <summary>
        /// 服务器发来的客户端列表数据
        /// </summary>
        /// <param name="arg"></param>
        public void OnServerSendClients(OnServerSendClientsEventArg arg)
        {
            OnServerSendClientsHandler?.Invoke(this, arg);
        }
    }

    public class OnServerSendClientsEventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public ClientsModel Data { get; set; }
    }

}
