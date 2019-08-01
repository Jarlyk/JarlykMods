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
            EnableRecentItemDropper = config.Wrap(
                "Settings",
                "EnableRecentItemDropper",
                "Whether to enable pressing a key to drop the recently picked up item",
                true);
            EnablePingImprovements = config.Wrap(
                "Settings",
                "EnablePingImprovements",
                "Whether to enable various improvements to pinging",
                true);
            EnableTeamImprovements = config.Wrap(
                "Settings",
                "EnableTeamImprovements",
                "Whether to enable various improvements to how allied teams are handled/displayed",
                true);
            EnableAllyCardImprovements = config.Wrap(
                "Settings",
                "EnableAllyCardImprovements",
                "Whether to enable improved highlighting of ally card notifications",
                true);
            EnableBossShopDropping = config.Wrap(
                "Settings",
                "EnableBossShopDropping",
                "Whether to enable dropping of shops after teleporter boss fights",
                true);
            EnableStarterPack = config.Wrap(
                "Settings",
                "EnableStarterPack",
                "Whether to enable optional granting of a 'starter pack' of items, to speed up the early game",
                true);

            DropItemKey = config.Wrap(
                "RecentItemDropper",
                "DropItemKey",
                "Key code for dropping recently picked up item",
                KeyCode.G.ToString());

            StarterPackKey = config.Wrap(
                "StarterPack",
                "StarterPackKey",
                "Key code for granting starter pack to all players in the game",
                KeyCode.F1.ToString());
        }

        public static ConfigWrapper<bool> EnableRecentItemDropper;

        public static ConfigWrapper<bool> EnablePingImprovements;

        public static ConfigWrapper<bool> EnableTeamImprovements;

        public static ConfigWrapper<bool> EnableAllyCardImprovements;

        public static ConfigWrapper<bool> EnableBossShopDropping;

        public static ConfigWrapper<bool> EnableStarterPack;

        public static ConfigWrapper<string> DropItemKey;

        public static ConfigWrapper<string> StarterPackKey;
    }
}
