using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using JarlykMods.Raincoat.ItemDropper;
using MiniRpcLib;
using R2API;
using R2API.Utils;
using RoR2.UI;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable UnusedMember.Local

namespace JarlykMods.Raincoat
{
    [BepInPlugin(PluginGuid, "Raincoat", "1.0.2")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("dev.wildbook.libminirpc")]
    [R2APISubmoduleDependency(nameof(LanguageAPI))]
    public class RaincoatPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.raincoat";

        private const string ModRpcId = "JarlykMods.Raincoat";

        private readonly RecentItemDropper _recentItemDroppper;
        private readonly PingImprovements _pingImprovements;
        private readonly TeamImprovements _teamImprovements;
        private readonly AllyCardImprovements _allyCardImprovements;
        private readonly BossShopDropper _bossShopDropper;
        private readonly KeyCode? _dropItemKey;
        private readonly KeyCode? _starterPackKey;
        private bool _inChatBox;

        public RaincoatPlugin()
        {
            RaincoatConfig.Init(Config);
            RaincoatAssets.Init();

            var miniRpc = MiniRpc.CreateInstance(ModRpcId);

            if (RaincoatConfig.EnableRecentItemDropper.Value)
                _recentItemDroppper = new RecentItemDropper(miniRpc);

            if (RaincoatConfig.EnablePingImprovements.Value)
                _pingImprovements = new PingImprovements();

            if (RaincoatConfig.EnableTeamImprovements.Value)
                _teamImprovements = new TeamImprovements();

            if (RaincoatConfig.EnableAllyCardImprovements.Value)
                _allyCardImprovements = new AllyCardImprovements();

            if (RaincoatConfig.EnableBossShopDropping.Value)
                _bossShopDropper = new BossShopDropper();

            _dropItemKey = GetKey(RaincoatConfig.DropItemKey);
            if (_dropItemKey == null)
                Debug.LogError("Invalid key code specified for DropItemKey");
            _starterPackKey = GetKey(RaincoatConfig.StarterPackKey);
            if (_starterPackKey == null)
                Debug.LogError("Invalid key code specified for StarterPackKey");

            On.RoR2.UI.ChatBox.FocusInputField += OnFocusInputField;
            On.RoR2.UI.ChatBox.UnfocusInputField += OnUnfocusInputField;
        }

        private void OnUnfocusInputField(On.RoR2.UI.ChatBox.orig_UnfocusInputField orig, ChatBox self)
        {
            _inChatBox = false;
        }

        private void OnFocusInputField(On.RoR2.UI.ChatBox.orig_FocusInputField orig, ChatBox self)
        {
            _inChatBox = true;
        }

        private KeyCode? GetKey(ConfigEntry<string> param)
        {
            if (!Enum.TryParse(param.Value, out KeyCode result))
                return null;

            return result;
        }

        public void Update()
        {
            if (_inChatBox || !Stage.instance || Stage.instance.sceneDef.sceneType != SceneType.Stage)
                return;

            if (_recentItemDroppper != null && _dropItemKey != null && Input.GetKeyDown(_dropItemKey.Value))
                _recentItemDroppper.DropRecentItem();

            if (RaincoatConfig.EnableStarterPack.Value && NetworkServer.active && _starterPackKey != null && Input.GetKeyDown(_starterPackKey.Value))
                StarterPack.GrantStarterItemsToAll();
        }
    }
}