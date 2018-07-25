using System;
using System.Collections.Generic;
using System.Text;
using GammaLibrary;
using Number = System.Numerics.BigInteger;

namespace DoudizhuSharp
{
    [Configuration("cygames")]
    public class Config : Configuration<Config>
    {
        static Config()
        {
            Update();
            Save();
        }
        public Dictionary<string, PlayerConfig> PlayerConfigs { get; set; } = new Dictionary<string, PlayerConfig>();


        public PlayerConfig GetPlayerConfig(string id)
        {
            if (!PlayerConfigs.ContainsKey(id)) {
                var pc = new PlayerConfig();
                PlayerConfigs[id] = pc;
            }
            
            return PlayerConfigs[id];
        }
    }

    public class PlayerConfig
    {
        public Number Point { get; set; } = 10000;
        public bool Admin { get; set; } = false;
    }
}
