using client.servers.clientServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ftp.client
{
    public class FtpClientPushMsg : IClientPushMsg
    {
        private readonly FtpClient ftpClient;
        public FtpClientPushMsg(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }

        public object Info()
        {
            return new
            {
                Uploads = ftpClient.GetUploads(),
                Downloads = ftpClient.GetDownloads(),
            };
        }
    }
}
