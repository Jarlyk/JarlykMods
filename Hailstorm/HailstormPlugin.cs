using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using EliteSpawningOverhaul;
using ItemLib;
using RoR2;
using UnityEngine;

namespace JarlykMods.Hailstorm
{
    [BepInPlugin(PluginGuid, "Hailstorm", "0.1.0")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(EsoPlugin.PluginGuid)]
    [BepInDependency(ItemLibPlugin.ModGuid)]
    public sealed class HailstormPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.hailstorm";

        private readonly DarkElitesManager _darkElites;
        private readonly BarrierElitesManager _barrierElites;
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

            if (HailstormConfig.EnableMimics.Value)
                _mimics = new Mimics();

            _rng = new Xoroshiro128Plus((ulong) DateTime.Now.Ticks);
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

            if (Input.GetKeyDown(KeyCode.F5) && _barrierElites != null)
            {
                var user = LocalUserManager.GetFirstLocalUser();
                var body = user.cachedBody;
                if (body?.master == null)
                {
                    Debug.LogError("Cannot find local user body!");
                    return;
                }

                var beetle = Resources.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscBeetle");
                var placement = new DirectorPlacementRule
                {
                    spawnOnTarget = body.transform,
                    maxDistance = 40,
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    preventOverhead = false
                };

                EsoLib.SpawnElite(beetle, _barrierElites.Card, placement, _rng);
            }
        }

        [Item(ItemAttribute.ItemType.Elite)]
        public static CustomElite BuildDarkElite()
        {
            //NOTE: We always need to build it, as we can't actually load the Config yet
            //This will reserve the slot, but it will still effectively be disabled if we never make it eligible for spawning with ESO
            return DarkElitesManager.Build();
        }

        [Item(ItemAttribute.ItemType.Elite)]
        public static CustomElite BuildBarrierElite()
        {
            return BarrierElitesManager.Build();
        }
    }
}
