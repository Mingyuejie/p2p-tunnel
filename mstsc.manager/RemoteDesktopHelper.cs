//using RDPCOMAPILib;
using common;
using common.extends;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using MessageBox = HandyControl.Controls.MessageBox;

namespace mstsc.manager
{
    public class RemoteDesktopHelper
    {
        private static readonly Lazy<RemoteDesktopHelper> lazy = new Lazy<RemoteDesktopHelper>(() => new RemoteDesktopHelper());
        public static RemoteDesktopHelper Instance
        {
            get => lazy.Value;
            private set { }

        }

        public RemoteDesktopViewModel viewModel = null;
        private readonly string configFileName = "config_remove_desktop.json";
        public event EventHandler<RemoteDesktopInfo> OnOpen;

        public RemoteDesktopHelper()
        {
            viewModel = new RemoteDesktopViewModel();
            ReadConfig();
        }


        public void Add(RemoteDesktopInfo model)
        {
            viewModel.Desktops.Add(model);
            SaveConfig();
        }
        public void Del(int index)
        {
            try
            {
                viewModel.Desktops.RemoveAt(index);
                SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Error(ex.Message);
            }
        }

        public void Open(RemoteDesktopInfo model)
        {
            OnOpen?.Invoke(this, model);
        }

        private void ReadConfig()
        {
            if (File.Exists(configFileName))
            {
                ConfigFileModel config = File.ReadAllText(configFileName).DeJson<ConfigFileModel>();
                if (config != null)
                {
                    viewModel.Desktops = new ObservableCollection<RemoteDesktopInfo>(config.Desktops);
                }
            }
        }

        private void SaveConfig()
        {
            File.WriteAllText(configFileName, new ConfigFileModel
            {
                Desktops = viewModel.Desktops.ToList()
            }.ToJson());
        }

        public void Disponse()
        {
            Instance = null;
            viewModel.Desktops.Clear();
            viewModel.Cleanup();
            viewModel = null;
        }
    }

    public class ConfigFileModel
    {
        public ConfigFileModel() { }

        public List<RemoteDesktopInfo> Desktops { get; set; } = new List<RemoteDesktopInfo>();
    }
}
