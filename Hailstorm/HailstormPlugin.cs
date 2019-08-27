using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using EliteSpawningOverhaul;
using ItemLib;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            if (Input.GetKeyDown(KeyCode.F5) && _barrierElites != null && _darkElites != null)
            {
                var user = LocalUserManager.GetFirstLocalUser();
                var body = user.cachedBody;
                if (body?.master == null)
                {
                    Debug.LogError("Cannot find local user body!");
                    return;
                }

                var beetle = Resources.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscBeetle");
                //var wisp = Resources.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscLesserWisp");
                var placement = new DirectorPlacementRule
                {
                    spawnOnTarget = body.transform,
                    maxDistance = 40,
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    preventOverhead = false
                };

                for (int i = 0; i < 5; i++)
                {
                    EsoLib.TrySpawnElite(beetle, _stormElites.Card, placement, _rng);
                }
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                var user = LocalUserManager.GetFirstLocalUser();
                var body = user.cachedBody;
                if (body?.master == null)
                {
                    Debug.LogError("Cannot find local user body!");
                    return;
                }

                var spawnPos = body.aimOriginTransform.TransformPoint(0, 0, 2);
                var twister = Instantiate(HailstormAssets.TwisterPrefab);
                twister.transform.position = spawnPos;
                twister.transform.localScale = new Vector3(15.0f, 35.0f, 15.0f);
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

        [Item(ItemAttribute.ItemType.Elite)]
        public static CustomElite BuildStormElite()
        {
            return StormElitesManager.Build();
        }
    }
}
