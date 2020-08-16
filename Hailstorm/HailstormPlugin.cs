using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx;
using EliteSpawningOverhaul;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    [BepInPlugin(PluginGuid, "Hailstorm", "0.4.1")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(EsoPlugin.PluginGuid)]
    [R2APISubmoduleDependency(nameof(EliteAPI))]
    [R2APISubmoduleDependency(nameof(BuffAPI))]
    [R2APISubmoduleDependency(nameof(ItemAPI))]
    [R2APISubmoduleDependency(nameof(CommandHelper))]
    [R2APISubmoduleDependency(nameof(ResourcesAPI))]
    public sealed class HailstormPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.hailstorm";

        private readonly DarkElitesManager _darkElites;
        private readonly BarrierElitesManager _barrierElites;
        private readonly StormElitesManager _stormElites;
        private readonly Mimics _mimics;
        private readonly Xoroshiro128Plus _rng;

        public HailstormPlugin()
        {
            HailstormConfig.Init(Config);
            HailstormAssets.Init();

            if (HailstormConfig.EnableDarkElites.Value)
                _darkElites = new DarkElitesManager();

            if (HailstormConfig.EnableBarrierElites.Value)
                _barrierElites = new BarrierElitesManager();

            if (HailstormConfig.EnableStormElites.Value)
                _stormElites = new StormElitesManager();

            if (HailstormConfig.EnableMimics.Value)
                _mimics = new Mimics();

            _rng = new Xoroshiro128Plus((ulong) DateTime.Now.Ticks);

            CommandHelper.AddToConsoleWhenReady();
        }

        private void Awake()
        {
            _darkElites?.Awake();
            _barrierElites?.Awake();
        }

        public void Start()
        {
            _barrierElites?.Start();
        }

        private void Update()
        {
            _darkElites?.Update();
            _barrierElites?.Update();
        }
    }
}
