using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace JarlykMods.Raincoat
{
    public static class RaincoatConfig
    {
        public static void Init(ConfigFile config)
        {
            EnableRecentItemDropper = config.Bind(
                "Settings",
                "EnableRecentItemDropper",
                true,
                "Whether to enable pressing a key to drop the recently picked up item");
            EnablePingImprovements = config.Bind(
                "Settings",
                "EnablePingImprovements",
                true,
                "Whether to enable various improvements to pinging");
            EnableTeamImprovements = config.Bind(
                "Settings",
                "EnableTeamImprovements",
                true,
                "Whether to enable various improvements to how allied teams are handled/displayed");
            EnableAllyCardImprovements = config.Bind(
                "Settings",
                "EnableAllyCardImprovements",
                true,
                "Whether to enable improved highlighting of ally card notifications");
            EnableBossShopDropping = config.Bind(
                "Settings",
                "EnableBossShopDropping",
                true,
                "Whether to enable dropping of shops after teleporter boss fights");
            EnableStarterPack = config.Bind(
                "Settings",
                "EnableStarterPack",
                true,
                "Whether to enable optional granting of a 'starter pack' of items, to speed up the early game");

            DropItemKey = config.Bind(
                "RecentItemDropper",
                "DropItemKey",
                KeyCode.G.ToString(),
                "Key code for dropping recently picked up item");

            StarterPackKey = config.Bind(
                "StarterPack",
                "StarterPackKey",
                KeyCode.F1.ToString(),
                "Key code for granting starter pack to all players in the game");

            BossDropUseArtifact = config.Bind(
                "BossShopDropping",
                "BossDropUseArtifact",
                true,
                "If enabled, whether to use an artifact to enable/disable the actual dropping on a per-run basis");

            BossDropRedsMinStage = config.Bind(
                "BossShopDropping",
                "BossDropRedsMinStage",
                6,
                "The first stage number at which red item shops can drop from bosses");

            BossDropRedsChancePerStage = config.Bind(
                "BossShopDropping",
                "BossDropRedsChancePerStage",
                0.02f,
                "The additive chance per stage, as a ratio;  0.02 would be a 2% chance");

            BossDropRedsMaxChance = config.Bind(
                "BossShopDropping",
                "BossDropRedsMaxChance",
                0.2f,
                "The maximum chance of a red shop dropping on any stage, as a ratio; 0.2 would be a 20% chance");
        }

        public static ConfigEntry<bool> EnableRecentItemDropper;

        public static ConfigEntry<bool> EnablePingImprovements;

        public static ConfigEntry<bool> EnableTeamImprovements;

        public static ConfigEntry<bool> EnableAllyCardImprovements;

        public static ConfigEntry<bool> EnableBossShopDropping;

        public static ConfigEntry<bool> EnableStarterPack;

        public static ConfigEntry<string> DropItemKey;

        public static ConfigEntry<string> StarterPackKey;

        public static ConfigEntry<bool> BossDropUseArtifact;
        
        public static ConfigEntry<int> BossDropRedsMinStage;

        public static ConfigEntry<float> BossDropRedsChancePerStage;

        public static ConfigEntry<float> BossDropRedsMaxChance;
    }
}
