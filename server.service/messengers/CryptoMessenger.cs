using common;
using common.extends;
using server.model;

namespace server.service.messengers
{
    public class CryptoMessenger : IMessenger
    {
        private readonly IAsymmetricCrypto asymmetricCrypto;
        private readonly ICryptoFactory cryptoFactory;
        public CryptoMessenger(IAsymmetricCrypto asymmetricCrypto, ICryptoFactory cryptoFactory)
        {
            this.asymmetricCrypto = asymmetricCrypto;
            this.cryptoFactory = cryptoFactory;
        }

        public string Key(IConnection connection)
        {
            return asymmetricCrypto.Key.PublicKey;
        }

        public bool Set(IConnection connection)
        {
            CryptoSetParamsInfo model = connection.ReceiveRequestWrap.Memory.DeBytes<CryptoSetParamsInfo>();
            connection.EncodeEnable(cryptoFactory.CreateSymmetric(model.Password));
            return true;
        }
    }
}
