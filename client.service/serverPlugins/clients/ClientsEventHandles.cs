using server.model;
using System;

namespace client.service.serverPlugins.clients
{
    public class ClientsEventHandles
    {
        private static readonly Lazy<ClientsEventHandles> lazy = new Lazy<ClientsEventHandles>(() => new ClientsEventHandles());
        public static ClientsEventHandles Instance => lazy.Value;

        private ClientsEventHandles()
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
        public PluginExcuteModel Packet { get; set; }
        public ClientsModel Data { get; set; }
    }

}
