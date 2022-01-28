using common;
using server;

namespace client.service.messengers.heart
{
    /// <summary>
    /// 心跳包
    /// </summary>
    public class HeartMessenger : IMessenger
    {
        public HeartMessenger()
        {
        }

        public bool Execute(IConnection connection)
        {
            //Logger.Instance.Debug($"收到{connection.ConnectId}的心跳");
            return true;
        }
    }
}
