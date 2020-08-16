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
            RegEquipLifetime = config.Bind(
                "Settings",
                "RegEquipLifetime",
                1200f,
                "Target lifetime of regular equipment, if using constantly, in seconds");

            LunarEquipLifetime = config.Bind(
                "Settings",
                "LunarEquipLifetime",
                2400f,
                "Target lifetime of lunar equipment, if using constantly, in seconds");
        }

        public static ConfigEntry<float> RegEquipLifetime;

        public static ConfigEntry<float> LunarEquipLifetime;
    }
}
