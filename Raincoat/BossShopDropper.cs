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
            int participatingPlayerCount = R2API.ItemDropAPI.BossDropParticipatingPlayerCount ?? Run.instance.participatingPlayerCount;
            if (participatingPlayerCount == 0)
                return;

            //More items for more players and for more mountain shrines
            int itemCount = (1 + self.bonusRewardCount);
            if (self.scaleRewardsByPlayerCount)
                itemCount *= participatingPlayerCount;

            for (int i = 0; i < itemCount; i++)
            {
                var rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUlong);

                //Create spawn card for a free green-item shop
                var spawnCard = Resources.Load<InteractableSpawnCard>("SpawnCards/InteractableSpawnCard/iscTripleShopLarge");
                var controller = spawnCard.prefab.GetComponent<MultiShopController>();

                //Slowly increasing chance of red items, capping at 20%
                var maxChance = RaincoatConfig.BossDropRedsMaxChance.Value;
                var chancePerStage = RaincoatConfig.BossDropRedsChancePerStage.Value;
                var minStage = RaincoatConfig.BossDropRedsMinStage.Value;
                var redChance = Mathf.Min(maxChance, chancePerStage*(Run.instance.stageClearCount - minStage - 1));
                controller.itemTier = rng.nextNormalizedFloat < redChance || self.forceTier3Reward ? ItemTier.Tier3 : ItemTier.Tier2;

                //Determine where to place the shop (randomly relative to the drop position)
                var placementRule = new DirectorPlacementRule();
                placementRule.maxDistance = 60f;
                placementRule.minDistance = 10f;
                placementRule.placementMode = DirectorPlacementRule.PlacementMode.Approximate;
                placementRule.position = self.dropPosition.position;
                placementRule.spawnOnTarget = self.dropPosition;

                var spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);

                var oldBaseCost = controller.baseCost;
                controller.baseCost = 0;
                var spawnedObj = DirectorCore.instance.TrySpawnObject(spawnRequest);
                controller.baseCost = oldBaseCost;
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
