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

            MimicChance = config.Wrap(
                "Mimics",
                "MimicChance",
                "Ratio of chests that will be replaced by Mimics, from 0 to 1",
                0.1f);
        }

        public static ConfigWrapper<bool> EnableMimics;

        public static ConfigWrapper<float> MimicChance;
    }
}
