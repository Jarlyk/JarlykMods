using System.Linq;
using System.Reflection;
using EliteSpawningOverhaul;
using R2API.Utils;
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
                    var noElites = ((CharacterSpawnCard) directorCard.spawnCard).noElites;
                    float highestCost = (float) (directorCard.cost*(noElites ? 1.0 : eliteCostMultiplier));
                    if (directorCard.CardIsValid() && directorCard.cost <= monsterCredit && highestCost/2.0 > monsterCredit)
                        weightedSelection.AddChoice(directorCard, monsterSelection.choices[index].weight);
                }

                if (weightedSelection.Count != 0)
                {
                    var chosenDirectorCard = weightedSelection.Evaluate(_rng.nextNormalizedFloat);

                    var eliteAffix = EsoLib.ChooseEliteAffix(chosenDirectorCard, monsterCredit, _rng);

                    var placement = new DirectorPlacementRule
                    {
                        placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                        preventOverhead = chosenDirectorCard.preventOverhead,
                        minDistance = 0,
                        maxDistance = 50,
                        spawnOnTarget = transform
                    };

                    var spawned = EsoLib.TrySpawnElite((CharacterSpawnCard)chosenDirectorCard.spawnCard, eliteAffix, placement, _rng);
                    if (spawned == null)
                    {
                        Debug.LogWarning("Failed to spawn monster for Mimic!");

                        //This ideally shouldn't happen, but for now we'll at least drop the item
                        PickupDropletController.CreatePickupDroplet(new PickupIndex(chestDrop.itemIndex), position, 10f*Vector3.up);
                        return;
                    }

                    //This monster carries the item that would've been in the chest and only gives xp, no gold
                    var spawnedMaster = spawned.GetComponent<CharacterMaster>();
                    spawnedMaster.inventory.GiveItem(chestDrop.itemIndex);
                    BoundReward = spawnedMaster.GetBody().GetComponent<DeathRewards>();
                    BoundReward.expReward = (uint)(chosenDirectorCard.cost*0.2*Run.instance.compensatedDifficultyCoefficient);
                    BoundItem = chestDrop.itemIndex;
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
