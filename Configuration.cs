using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace KayDeath
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool AutoOpenOnDeath { get; set; } = true;

        [NonSerialized]
        private IDalamudPluginInterface? _pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this._pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this._pluginInterface?.SavePluginConfig(this);
        }
    }
}
