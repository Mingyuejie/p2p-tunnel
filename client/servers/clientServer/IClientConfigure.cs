using common.extends;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.servers.clientServer
{
    public interface IClientConfigure
    {
        string Name { get; }
        string Author { get; }
        string Desc { get; }
        bool Enable { get; }
        Task<bool> SwitchEnable(bool enable);
        Task<object> Load();
        Task<string> Save(string jsonStr);
    }

    public class ClientConfigureInfoBase
    {
        protected static async Task<T> FromFile<T>(string fileName)
        {
            if (File.Exists(fileName))
            {
                return (await File.ReadAllTextAsync(fileName)).DeJson<T>();
            }
            return default;
        }

        protected async Task ToFile<T>(T obj, string fileName)
        {
            await File.WriteAllTextAsync(fileName, obj.ToJson(), System.Text.Encoding.UTF8);
        }
    }
}
