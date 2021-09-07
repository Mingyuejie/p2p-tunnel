using common.cache;
using common.extends;
using server.model;
using server.plugin;
using server.service.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.service.plugins
{
    public class ResetPlugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.SERVER_RESET;

        public void Excute(PluginExcuteModel data, ServerType serverType)
        {
            ResetModel model = data.Packet.Chunk.DeBytes<ResetModel>();

            //A已注册
            RegisterCacheModel source = ClientRegisterCache.Instance.Get(model.Id);
            if (source != null)
            {
                //B已注册
                RegisterCacheModel target = ClientRegisterCache.Instance.Get(model.ToId);
                if (target != null)
                {
                    //是否在同一个组
                    if (source.GroupId != target.GroupId)
                    {
                        return;
                    }

                    if (serverType == ServerType.UDP)
                    {
                        UDPServer.Instance.Send(new RecvQueueModel<IModelBase>
                        {
                            Address = target.Address,
                            TcpCoket = null,
                            Data = model
                        });
                    }
                    else if (serverType == ServerType.TCP)
                    {
                        TCPServer.Instance.Send(new RecvQueueModel<IModelBase>
                        {
                            Address = target.Address,
                            TcpCoket = target.TcpSocket,
                            Data = model
                        });
                    }
                }
            }
        }
    }
}
