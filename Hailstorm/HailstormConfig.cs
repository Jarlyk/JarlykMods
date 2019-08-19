using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace JarlykMods.Hailstorm
{
    public static class HailstormConfig
    {
        public static void Init(ConfigFile config)
        {
            EnableMimics = config.Wrap(
                "Settings",
                "EnableMimics",
                "Whether to enable replacement of chests with Mimics",
                true);

            EnableDarkElites = config.Wrap(
                "Settings",
                "EnableDarkElites",
                "Whether to enable Dark elites; if you stare into them, your world will go dark",
                true);

            EnableBarrierElites = config.Wrap(
                "Settings",
                "EnableBarrierElites",
                "Whether to enable Barrier elites; it will be much harder to kill their friends while they're still alive",
                true);

            EnableStormElites = config.Wrap(
                "Settings",
                "EnableStormElites",
                "Whether to enable Storm elites; don't underestimate their damage and try not to get caught by their wind or chain lightning",
                true);

            MimicChance = config.Wrap(
                "Mimics",
                "MimicChance",
                "Ratio of chests that will be replaced by Mimics, from 0 to 1",
                0.1f);
        }

        public static ConfigWrapper<bool> EnableMimics;

        public static ConfigWrapper<bool> EnableDarkElites;

        public static ConfigWrapper<bool> EnableBarrierElites;

        public static ConfigWrapper<bool> EnableStormElites;

        public static ConfigWrapper<float> MimicChance;
    }
}
