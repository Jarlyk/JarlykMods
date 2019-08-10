using System.Linq;
using System.Reflection;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    public sealed class ChestMimicBehavior : MonoBehaviour
    {
        //For comparison, shrine of combat is 100
        public const float BaseMonsterCredit = 120;

        private Xoroshiro128Plus _rng;

        private float monsterCredit => BaseMonsterCredit*Run.instance.difficultyCoefficient;

        public DeathRewards BoundReward { get; private set; }

        public ItemIndex BoundItem { get; private set; }

        private void Start()
        {
            _rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
        }

        private void OnTriggerEnter(Collider collider)
        {
            //Only run on the host and when enabled
            if (!NetworkServer.active || !enabled)
                return;

            //Check if we've collided with a player character
            var body = collider.gameObject.GetComponent<CharacterBody>();
            if (body != null && body.isPlayerControlled)
            {
                //We've done our job after this and can go to sleep
                enabled = false;

                //Assuming this is a chest, grab its behavior
                var chest = gameObject.GetComponent<ChestBehavior>();
                if (chest == null)
                    return;

                //Check what item was supposed to drop from this chest
                var chestDrop = chest.GetFieldValue<PickupIndex>("dropPickup");
                if (chestDrop.itemIndex == ItemIndex.None)
                    return;

                //We're not really a chest, so remove it, noting where we were located
                var position = transform.position;
                Destroy(gameObject);

                //Play the shrine effect to highlight that something momentous is happening
                EffectManager instance = EffectManager.instance;
                GameObject effectPrefab = Resources.Load<GameObject>("Prefabs/Effects/ShrineUseEffect");
                EffectData effectData = new EffectData();
                effectData.origin = position;
                effectData.rotation = Quaternion.identity;
                effectData.scale = 1f;
                effectData.color = new Color32(255, 0, 0, 255);
                instance.SpawnEffect(effectPrefab, effectData, true);

                var monsterSelection = ClassicStageInfo.instance.monsterSelection;
                var weightedSelection = new WeightedSelection<DirectorCard>(8);
                float eliteCostMultiplier = CombatDirector.highestEliteCostMultiplier;
                for (int index = 0; index < monsterSelection.Count; ++index)
                {
                    DirectorCard directorCard = monsterSelection.choices[index].value;
                    float num = (float)((double)directorCard.cost * (double)CombatDirector.maximumNumberToSpawnBeforeSkipping * ((directorCard.spawnCard as CharacterSpawnCard).noElites ? 1.0 : (double)eliteCostMultiplier));
                    if (directorCard.CardIsValid() && (double)directorCard.cost <= (double)this.monsterCredit && (double)num / 2.0 > (double)this.monsterCredit)
                        weightedSelection.AddChoice(directorCard, monsterSelection.choices[index].weight);
                }

                if (weightedSelection.Count != 0)
                {
                    var chosenDirectorCard = weightedSelection.Evaluate(_rng.nextNormalizedFloat);
                    var placement = new DirectorPlacementRule
                    {
                        placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                        preventOverhead = chosenDirectorCard.preventOverhead,
                        minDistance = 0,
                        maxDistance = 50,
                        spawnOnTarget = transform
                    };
                    var spawnRequest = new DirectorSpawnRequest(chosenDirectorCard.spawnCard, placement, _rng)
                    {
                        teamIndexOverride = TeamIndex.Monster,
                        ignoreTeamMemberLimit = true
                    };
                    var spawned = DirectorCore.instance.TrySpawnObject(spawnRequest);
                    if (spawned == null)
                    {
                        Debug.LogWarning("Failed to spawn monster for Mimic!");

                        //This ideally shouldn't happen, but for now we'll at least drop the item
                        PickupDropletController.CreatePickupDroplet(new PickupIndex(chestDrop.itemIndex), position, 10f*Vector3.up);
                        return;
                    }

                    //Elites are boosted based on hard-coded parameters in CombatDirector
                    var tiers = (CombatDirector.EliteTierDef[])typeof(CombatDirector).GetField("eliteTiers", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

                    var eliteTypes = tiers.Where(t => t.isAvailable())
                                      .SelectMany(t => t.eliteTypes)
                                      .Where(e => e != EliteIndex.None && e != EliteIndex.Gold)
                                      .ToList();
                    var eliteType = _rng.NextElementUniform(eliteTypes);
                    var tier = tiers.First(t => t.eliteTypes.Contains(eliteType));
                    var healthBoost = tier.healthBoostCoefficient;
                    var damageBoost = tier.damageBoostCoefficient;

                    //Configure as the chosen elite
                    var spawnedMaster = spawned.GetComponent<CharacterMaster>();
                    spawnedMaster.inventory.GiveItem(ItemIndex.BoostHp, Mathf.RoundToInt((float)((healthBoost - 1.0) * 10.0)));
                    spawnedMaster.inventory.GiveItem(ItemIndex.BoostDamage, Mathf.RoundToInt((float)((damageBoost - 1.0) * 10.0)));
                    var eliteDef = EliteCatalog.GetEliteDef(eliteType);
                    if (eliteDef != null)
                        spawnedMaster.inventory.SetEquipmentIndex(eliteDef.eliteEquipmentIndex);

                    //This monster carries the item that would've been in the chest and only gives xp, no gold
                    spawnedMaster.inventory.GiveItem(chestDrop.itemIndex);
                    BoundReward = spawnedMaster.GetBody().GetComponent<DeathRewards>();
                    BoundReward.expReward = (uint)(chosenDirectorCard.cost*0.2*Run.instance.compensatedDifficultyCoefficient);
                    BoundItem = chestDrop.itemIndex;

                    //Bypass spawning animation by skipping out of the spawning state
                    foreach (var stateMachine in body.GetComponents<EntityStateMachine>())
                        stateMachine.initialStateType = stateMachine.mainStateType;
                }
            }
        }

        public static ChestMimicBehavior Build(GameObject owner)
        {
            var result = owner.AddComponent<ChestMimicBehavior>();
            var collider = result.gameObject.AddComponent<SphereCollider>();
            collider.radius = 15;
            collider.isTrigger = true;
            return result;
        }
    }
}
