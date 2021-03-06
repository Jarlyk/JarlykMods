﻿using System;
using System.Collections.Generic;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace JarlykMods.Raincoat
{
    public sealed class BossShopDropper
    {
        private ArtifactDef _artifactDef;

        private const string ArtifactNameToken = "ARTIFACT_MERCH_NAME";
        private const string ArtifactDescToken = "ARTIFACT_MERCH_DESC";

        public BossShopDropper()
        {
            On.RoR2.BossGroup.DropRewards += BossGroupDropRewards;

            if (RaincoatConfig.BossDropUseArtifact.Value)
            {
                ArtifactCatalog.getAdditionalEntries += (list) =>
                {
                    _artifactDef = ScriptableObject.CreateInstance<ArtifactDef>();
                    _artifactDef.nameToken = ArtifactNameToken;
                    _artifactDef.descriptionToken = ArtifactDescToken;
                    _artifactDef.smallIconDeselectedSprite = RaincoatAssets.ArtifactMerchInactiveIcon;
                    _artifactDef.smallIconSelectedSprite = RaincoatAssets.ArtifactMerchActiveIcon;
                    list.Add(_artifactDef);
                };

                LanguageAPI.Add(ArtifactNameToken, "Artifact of Merch");
                LanguageAPI.Add(ArtifactDescToken, "When bosses die, they drop shops instead of items");

                Debug.Log("Raincoat: Added Artifact of Merch");
            }
        }

        private void BossGroupDropRewards(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup self)
        {
            //If we're using an artifact and it's not enabled, just use the default handler
            if (RaincoatConfig.BossDropUseArtifact.Value && !RunArtifactManager.instance.IsArtifactEnabled(_artifactDef))
            {
                orig(self);
                return;
            }

            //If no players, nothing to do
            int participatingPlayerCount = ItemDropAPI.BossDropParticipatingPlayerCount ?? Run.instance.participatingPlayerCount;
            if (participatingPlayerCount == 0 || !self.dropPosition)
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
                placementRule.maxDistance = 50f;
                placementRule.minDistance = 5f;
                placementRule.placementMode = DirectorPlacementRule.PlacementMode.Approximate;
                placementRule.position = self.dropPosition.position;
                placementRule.spawnOnTarget = self.dropPosition;

                var spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);

                GameObject spawnedObj;
                var oldBaseCost = controller.baseCost;
                var oldSkipSpawn = spawnCard.skipSpawnWhenSacrificeArtifactEnabled;
                controller.baseCost = 0;
                spawnCard.skipSpawnWhenSacrificeArtifactEnabled = false;
                try
                { 
                    spawnedObj = DirectorCore.instance.TrySpawnObject(spawnRequest);
                }
                finally
                {
                    controller.baseCost = oldBaseCost;
                    spawnCard.skipSpawnWhenSacrificeArtifactEnabled = oldSkipSpawn;
                }

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
