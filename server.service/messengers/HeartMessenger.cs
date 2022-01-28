using common;

namespace server.service.messengers
{
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
