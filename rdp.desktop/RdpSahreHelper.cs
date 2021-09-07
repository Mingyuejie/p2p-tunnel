using common;
using common.extends;
using RDPCOMAPILib;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace rdp.desktop
{
    public class RdpSahreHelper
    {
        private static readonly Lazy<RdpSahreHelper> lazy = new Lazy<RdpSahreHelper>(() => new RdpSahreHelper());
        public static RdpSahreHelper Instance => lazy.Value;

        private RDPSession rdpSession;
        public string ConnectString { get; private set; } = string.Empty;
        private readonly string configFileName = "config_rdpshare.json";

        public RdpShareSettingViewModel ViewModel { get; private set; }

        public event EventHandler<bool> OnConnectdChange;

        private bool connecting = false;

        private RdpSahreHelper()
        {
            ViewModel = new RdpShareSettingViewModel();
            ReadConfig();
        }

        public void Start()
        {
            if (ViewModel.Connected)
            {
                Stop();
            }
            else
            {
                if (connecting)
                {
                    return;
                }

                _ = Task.Run(() =>
                  {
                      try
                      {
                          rdpSession = new RDPSession();
                          rdpSession.Open();
                          rdpSession.OnAttendeeConnected += (e) =>
                          {
                              IRDPSRAPIAttendee att = e as IRDPSRAPIAttendee;
                              att.ControlLevel = CTRL_LEVEL.CTRL_LEVEL_INTERACTIVE;
                          };

                          IRDPSRAPIInvitation invitation = rdpSession.Invitations.CreateInvitation(null, "default", ViewModel.Password, ViewModel.AttendeeLimit);

                          ConnectString = invitation.ConnectionString;
                          ViewModel.ServerConnectString = invitation.ConnectionString;

                          ViewModel.Connected = true;
                          OnConnectdChange?.Invoke(this, true);
                          SaveConfig();
                      }
                      catch (Exception ex)
                      {
                          Console.WriteLine("开启远程桌面失败:" + ex);
                          connecting = false;
                      }
                  });
            }
        }

        public void Stop()
        {
            if (rdpSession != null)
            {
                rdpSession.Close();
            }
            connecting = false;
            ViewModel.Connected = false;
            OnConnectdChange?.Invoke(this, false);
            ConnectString = string.Empty;
        }

        public void ConnectClient(string connectString)
        {
            if (rdpSession != null)
            {
                try
                {
                    rdpSession.ConnectToClient(connectString);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("未开启服务");
            }
        }

        private void ReadConfig()
        {
            if (File.Exists(configFileName))
            {
                ConfigFileModel config = File.ReadAllText(configFileName).DeJson<ConfigFileModel>();
                if (config != null)
                {
                    ViewModel.AttendeeLimit = config.AttendeeLimit;
                    ViewModel.Password = config.Password;
                }
            }
        }

        private void SaveConfig()
        {
            File.WriteAllText(configFileName, new ConfigFileModel
            {
                AttendeeLimit = ViewModel.AttendeeLimit,
                Password = ViewModel.Password,
            }.ToJson());
        }


    }

    public class ConfigFileModel
    {
        public ConfigFileModel() { }
        public ushort AttendeeLimit { get; set; }
        public string Password { get; set; }
    }
}
