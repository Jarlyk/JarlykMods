using System;
using System.Linq;
using BepInEx;
using RoR2;
using TMPro;
using UnityEngine;

namespace EliteSpawningOverhaul
{
    [BepInPlugin(PluginGuid, "EliteSpawningOverhaul", "0.2.0")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    public sealed class EsoPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.eso";

        public EsoPlugin()
        {
            EsoLib.Init();
            R2API.Utils.CommandHelper.AddToConsoleWhenReady();
        }

        [ConCommand(commandName = "eso_spawn", flags = ConVarFlags.ExecuteOnServer, 
            helpText = "Spawn a monster, possibly with a custom elite type.  Usage: eso_spawn SpawnCard [EliteModifierToken]")]
        private static void Spawn(ConCommandArgs args)
        {
            var spawnCardStr = args.userArgs[0];
            var eliteStr = args.userArgs.Count > 1 ? args.userArgs[1] : "";

            var spawnCard = Resources.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/" + spawnCardStr);
            if (spawnCard == null)
            {
                Debug.LogWarning($"Could not locate character spawn card asset with name {spawnCardStr}; should be 'cscBeetle' for spawning a beetle, for instance");
                return;
            }

            EliteAffixCard affixCard = null;
            if (!string.IsNullOrEmpty(eliteStr))
            {
                affixCard = EsoLib.Cards.FirstOrDefault(c => EliteCatalog
                                                             .GetEliteDef(c.eliteType).modifierToken.ToLower()
                                                             .Contains(eliteStr.ToLower()));
            }

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

            var rng = new Xoroshiro128Plus((ulong) DateTime.Now.Ticks);
            if (EsoLib.TrySpawnElite(spawnCard, affixCard, placement, rng) == null)
            {
                Debug.LogWarning("Failed to spawn elite; try again somewhere less crowded");
            }
        }
    }
}
