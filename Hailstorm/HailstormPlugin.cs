using System;
using System.Collections.Generic;
using System.Linq;
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
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    [BepInPlugin(PluginGuid, "Hailstorm", "1.2.2")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(EsoPlugin.PluginGuid)]
    [R2APISubmoduleDependency(nameof(EliteAPI))]
    [R2APISubmoduleDependency(nameof(BuffAPI))]
    [R2APISubmoduleDependency(nameof(ItemAPI))]
    [R2APISubmoduleDependency(nameof(CommandHelper))]
    [R2APISubmoduleDependency(nameof(ResourcesAPI))]
    [R2APISubmoduleDependency(nameof(LanguageAPI))]
    [R2APISubmoduleDependency(nameof(PrefabAPI))]
    [R2APISubmoduleDependency(nameof(LoadoutAPI))]
    [R2APISubmoduleDependency(nameof(SurvivorAPI))]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod)]
    public sealed class HailstormPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.hailstorm";

        private readonly DarkElitesManager _darkElites;
        private readonly BarrierElitesManager _barrierElites;
        private readonly StormElitesManager _stormElites;
        private static Mimics _mimics;
        private static Xoroshiro128Plus _rng;

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
            _mimics?.Awake();
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

        [ConCommand(commandName = "hs_testmimic", flags = ConVarFlags.ExecuteOnServer,
                    helpText = "Test Mimic")]
        private static void TestMimic(ConCommandArgs args)
        {
            var spawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            spawnCard.hullSize = HullClassification.Human;
            spawnCard.name = "Mimic";
            spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
            spawnCard.prefab = Mimics.MasterPrefab;

            var user = LocalUserManager.GetFirstLocalUser();
            var body = user.cachedBody;
            if (body?.master == null)
            {
                Debug.LogError("Cannot find local user body!");
                return;
            }

            var placement = new DirectorPlacementRule
            {
                spawnOnTarget = body.transform,
                maxDistance = 40,
                placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                preventOverhead = false
            };

            EsoLib.TrySpawnElite(spawnCard, null, placement, _rng);
        }
    }
}
