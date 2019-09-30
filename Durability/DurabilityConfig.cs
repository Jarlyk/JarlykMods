using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;

namespace JarlykMods.Durability
{
    public static class DurabilityConfig
    {
        public static void Init(ConfigFile config)
        {
            RegEquipLifetime = config.Wrap(
                "Settings",
                "RegEquipLifetime",
                "Target lifetime of regular equipment, if using constantly, in seconds",
                360f);

            LunarEquipLifetime = config.Wrap(
                "Settings",
                "LunarEquipLifetime",
                "Target lifetime of lunar equipment, if using constantly, in seconds",
                720f);
        }

        public static ConfigWrapper<float> RegEquipLifetime;

        public static ConfigWrapper<float> LunarEquipLifetime;
    }
}
