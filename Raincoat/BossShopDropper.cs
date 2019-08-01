using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace JarlykMods.Raincoat
{
    public sealed class BossShopDropper
    {
        public BossShopDropper()
        {
            On.RoR2.BossGroup.DropRewards += BossGroupDropRewards;
        }

        private void BossGroupDropRewards(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup self)
        {
            //NOTE: We're overwriting the behavior entirely here, so no need to call back to orig

            //If no players, nothing to do
            int participatingPlayerCount = Run.instance.participatingPlayerCount;
            if (participatingPlayerCount == 0)
                return;

            //If no teleporter, nothing to do
            var teleporterInteraction = TeleporterInteraction.instance;
            if (teleporterInteraction == null)
                return;

            //More items for more players and for more mountain shrines
            int itemCount = participatingPlayerCount*(1 + teleporterInteraction.shrineBonusStacks);

            for (int i = 0; i < itemCount; i++)
            {
                var rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUlong);

                //Create spawn card for a free green-item shop
                var spawnCard = Resources.Load<InteractableSpawnCard>("SpawnCards/InteractableSpawnCard/iscTripleShopLarge");
                var controller = spawnCard.prefab.GetComponent<MultiShopController>();
                controller.baseCost = 0;

                //Slowly increasing chance of red items, capping at 20%
                var redChance = Math.Min(0.20f, 0.02f * Run.instance.stageClearCount - 0.10f);
                controller.itemTier = rng.nextNormalizedFloat < redChance ? ItemTier.Tier3 : ItemTier.Tier2;

                //Determine where to place the shop (randomly relative to the teleporter)
                var placementRule = new DirectorPlacementRule();
                placementRule.maxDistance = 60f;
                placementRule.minDistance = 10f;
                placementRule.placementMode = DirectorPlacementRule.PlacementMode.Approximate;
                placementRule.position = teleporterInteraction.transform.position;
                placementRule.spawnOnTarget = teleporterInteraction.transform;

                //Try to spawn shop
                var spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);
                var spawnedObj = DirectorCore.instance.TrySpawnObject(spawnRequest);
                if (spawnedObj != null)
                {
                    //Replace first terminal with special boss item, if applicable
                    var bossDrops = self.GetFieldValue<List<PickupIndex>>("bossDrops");
                    if (bossDrops?.Count > 0 && rng.nextNormalizedFloat <= self.bossDropChance)
                    {
                        controller = spawnedObj.GetComponent<MultiShopController>();
                        var terminal = controller.GetFieldValue<GameObject[]>("terminalGameObjects")[0];
                        var behavior = terminal.GetComponent<ShopTerminalBehavior>();
                        behavior.SetPickupIndex(rng.NextElementUniform(bossDrops));
                    }
                }
            }
        }
    }
}
