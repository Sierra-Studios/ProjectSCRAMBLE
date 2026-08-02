using System.ComponentModel;

using Exiled.API.Interfaces;

using UnityEngine;

namespace ProjectSCRAMBLE.Configs
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;
    }
}
