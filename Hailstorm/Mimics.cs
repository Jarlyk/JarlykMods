using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace JarlykMods.Hailstorm
{
    public sealed class Mimics
    {
        private readonly List<ChestMimicBehavior> _chestMimics = new List<ChestMimicBehavior>();
        private Xoroshiro128Plus _rng;

        public Mimics()
        {
            IL.RoR2.SceneDirector.PopulateScene += SceneDirectorOnPopulateScene;
            On.RoR2.DeathRewards.OnKilledServer += DeathRewardsOnOnKilledServer;
        }

        private void SceneDirectorOnPopulateScene(ILContext il)
        {
            var cursor = new ILCursor(il);

            //The first TrySpawnObject is for interactables
            cursor.GotoNext(MoveType.After, x => x.MatchCallvirt("RoR2.DirectorCore", "TrySpawnObject"));
            cursor.Index += 1;
            cursor.Emit(OpCodes.Ldloc_S, (byte)5);
            cursor.EmitDelegate<Action<GameObject>>(OnSpawnInteractable);
        }

        private void OnSpawnInteractable(GameObject gameObj)
        {
            if (gameObj == null)
                return;

            var chest = gameObj.GetComponent<ChestBehavior>();
            if (chest != null)
            {
                if (_rng == null)
                {
                    _rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
                }

                if (_rng.nextNormalizedFloat < HailstormConfig.MimicChance.Value)
                {
                    Debug.Log("Mimic added");
                    var mimic = ChestMimicBehavior.Build(gameObj);
                    _chestMimics.Add(mimic);
                }
            }
        }

        private void DeathRewardsOnOnKilledServer(On.RoR2.DeathRewards.orig_OnKilledServer orig, DeathRewards self, DamageReport damagereport)
        {
            orig(self, damagereport);

            foreach (var mimic in _chestMimics.ToList())
            {
                if (mimic.BoundReward == self)
                {
                    PickupDropletController.CreatePickupDroplet(new PickupIndex(mimic.BoundItem), self.transform.position, 5f*Vector3.up);
                    _chestMimics.Remove(mimic);
                }
            }
        }
    }
}
