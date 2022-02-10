using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.model
{
    [MessagePackObject]
    public class CryptoKeyParamsInfo
    {
        public CryptoKeyParamsInfo() { }
    }

    [MessagePackObject]
    public class CryptoSetParamsInfo
    {
        public CryptoSetParamsInfo() { }

        [Key(1)]
        public string Password { get; set; } = string.Empty;
    }
}
