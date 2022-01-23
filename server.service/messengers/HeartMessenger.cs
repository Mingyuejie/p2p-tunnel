namespace server.service.messengers
{
    public class HeartMessenger : IMessenger
    {
        public HeartMessenger()
        {
        }

        public bool Execute(IConnection connection)
        {
            return true;
        }
    }
}
