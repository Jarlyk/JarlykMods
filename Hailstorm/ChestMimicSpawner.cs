﻿using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using EliteSpawningOverhaul;
using JarlykMods.Hailstorm.MimicStates;
using KinematicCharacterController;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace JarlykMods.Hailstorm
{
    public sealed class ChestMimicSpawner : MonoBehaviour
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
                var dropItem = PickupCatalog.GetPickupDef(chestDrop).itemIndex;
                if (dropItem == ItemIndex.None)
                    return;

                if (gameObject.name.StartsWith("Chest1"))
                {
                    SpawnNewMimic(collider, chestDrop);
                }
                else
                {
                    //SpawnLegacyMimic(dropItem);
                }
            }
        }

        private void SpawnLegacyMimic(ItemIndex dropItem)
        {
            //We're not really a chest, so remove it, noting where we were located
            var position = gameObject.transform.position;
            Destroy(gameObject, Time.fixedDeltaTime);

            //We also want to remove the node where we spawned from the occupied nodes list
            //This allows the mimic monster to spawn in the same location
            var node = SceneInfo.instance.groundNodes.FindClosestNode(position, HullClassification.Human);
            RemoveNode(DirectorCore.instance, node);

            //Play the shrine effect to highlight that something momentous is happening
            GameObject effectPrefab = Resources.Load<GameObject>("Prefabs/Effects/ShrineUseEffect");
            EffectData effectData = new EffectData();
            effectData.origin = position;
            effectData.rotation = Quaternion.identity;
            effectData.scale = 1f;
            effectData.color = new Color32(255, 0, 0, 255);
            EffectManager.SpawnEffect(effectPrefab, effectData, true);

            var weightedSelection = Util.CreateReasonableDirectorCardSpawnList(monsterCredit, 6, 1);
            if (weightedSelection.Count != 0)
            {
                var choices = weightedSelection.choices;
                for (int i = 0; i < choices.Length; i++)
                {
                    //Worms are banned from being mimics
                    var c = choices[i];
                    if (c.value.spawnCard.name == "cscMagmaWorm" ||
                        c.value.spawnCard.name == "cscElectricWorm")
                    {
                        weightedSelection.ModifyChoiceWeight(i, 0);
                    }
                }

                var chosenDirectorCard = weightedSelection.Evaluate(_rng.nextNormalizedFloat);

                var eliteAffix = EsoLib.ChooseEliteAffix(chosenDirectorCard, monsterCredit, _rng);

                var placement = new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Direct,
                    preventOverhead = chosenDirectorCard.preventOverhead,
                    minDistance = 0,
                    maxDistance = 50,
                    spawnOnTarget = transform
                };
                placement.spawnOnTarget.Translate(0, 0.5f, 0);

                var spawned = EsoLib.TrySpawnElite((CharacterSpawnCard) chosenDirectorCard.spawnCard, eliteAffix, placement,
                                                   _rng);
                if (spawned == null)
                {
                    Debug.LogWarning("Failed to spawn monster for Mimic due to insufficient spawn area!");

                    //This ideally shouldn't happen, but for now we'll at least drop the item
                    PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(dropItem), position,
                                                                10f*Vector3.up);
                    return;
                }

                //This monster carries the item that would've been in the chest and only gives xp, no gold
                var spawnedMaster = spawned.GetComponent<CharacterMaster>();
                spawnedMaster.inventory.GiveItem(dropItem);
                BoundReward = spawnedMaster.GetBody().GetComponent<DeathRewards>();
                BoundReward.expReward = (uint) (chosenDirectorCard.cost*0.2*Run.instance.compensatedDifficultyCoefficient);
                BoundItem = dropItem;
            }
            else
            {
                Debug.LogWarning("Failed to spawn monster for Mimic due to unable to find a monster type!");

                //This ideally shouldn't happen, but for now we'll at least drop the item
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(dropItem), position, 10f*Vector3.up);
                return;
            }
        }

        private void SpawnNewMimic(Collider collider, PickupIndex chestDrop)
        {
            //Get biases for the splat map from the current chest
            var origModel = GetComponent<ModelLocator>().modelTransform.gameObject;
            var renderer = origModel.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!renderer)
                Debug.Log("Couldn't find renderer");

            var propBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propBlock);
            var biasR = propBlock.GetFloat("_RedChannelBias");
            var biasG = propBlock.GetFloat("_GreenChannelBias");
            var biasB = propBlock.GetFloat("_BlueChannelBias");

            //We're not really a chest, so remove it, noting where we were located
            var origTransform = gameObject.transform;
            Destroy(gameObject, Time.fixedDeltaTime);

            //Create mimic in its place
            var spawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            spawnCard.prefab = Mimics.MasterPrefab;
            spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
            spawnCard.occupyPosition = false;
            spawnCard.name = "Mimic";
            //var spawnReq = new DirectorSpawnRequest(spawnCard, null, null);
            //var spawnResult = spawnCard.DoSpawn(origTransform.position + origTransform.rotation*new Vector3(0, 1.4f, 0), origTransform.rotation, spawnReq);
            //var mimic = spawnResult.spawnedInstance;

            var placement = new DirectorPlacementRule();
            placement.spawnOnTarget = origTransform;
            placement.placementMode = DirectorPlacementRule.PlacementMode.Direct;

            var mimicMaster = EsoLib.TrySpawnElite(spawnCard, null, placement, _rng);
            var message = new ConfigureMimicMessage
            {
                mimicMasterId = mimicMaster.netId,
                initialRotation = placement.spawnOnTarget.rotation,
                target = collider.gameObject,
                splatBiasR = biasR,
                splatBiasG = biasG,
                splatBiasB = biasB,
                item = chestDrop
            };
            message.Send(NetworkDestination.Clients);
            ConfigureMimic(message, mimicMaster.GetBody(), mimicMaster);

            //NOTE: We don't need to call this explicitly on the server, as the server also gets the OnReceived callback for the message
            //ConfigureMimic(message);
        }

        public static void ConfigureMimic(ConfigureMimicMessage message)
        {
            HailstormPlugin.Instance.StartCoroutine(ConfigureMimicAsync(message));
        }

        private static IEnumerator ConfigureMimicAsync(ConfigureMimicMessage message)
        {
            CharacterMaster mimicMaster = null;
            CharacterBody mimic = null;

            yield return new WaitUntil(() =>
            {
                mimicMaster = ClientScene.FindLocalObject(message.mimicMasterId)?.GetComponent<CharacterMaster>();
                mimic = mimicMaster?.GetBody();
                return mimicMaster && mimic;
            });

            ConfigureMimic(message, mimic, mimicMaster);
        }

        private static void ConfigureMimic(ConfigureMimicMessage message, CharacterBody mimic, CharacterMaster mimicMaster)
        {
            Debug.Log("Configuring mimic");

            var context = mimic.gameObject.AddComponent<MimicContext>();
            context.initialRotation = message.initialRotation;
            context.target = message.target;

            var mimicModel = mimic.GetComponent<ModelLocator>().modelTransform.gameObject;
            var mimicRenderer = mimicModel.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!mimicRenderer)
                Debug.Log("Couldn't find mimic renderer");

            var propBlock = new MaterialPropertyBlock();
            mimicRenderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat("_RedChannelBias", message.splatBiasR);
            propBlock.SetFloat("_GreenChannelBias", message.splatBiasG);
            propBlock.SetFloat("_BlueChannelBias", message.splatBiasB);
            mimicRenderer.SetPropertyBlock(propBlock);

            //Initially the mimic can't set its direction; we need this to allow the surprise attack to adjust into orientation
            var dir = mimic.GetComponent<CharacterDirection>();
            dir.enabled = false;

            var motor = mimic.GetComponent<CharacterMotor>();
            motor.enabled = false;
            mimic.GetComponent<Rigidbody>().detectCollisions = false;

            var kinMotor = mimic.GetComponent<KinematicCharacterMotor>();
            kinMotor.enabled = false;

            var ai = mimicMaster.GetComponent<BaseAI>();
            ai.customTarget.gameObject = message.target;

            if (NetworkServer.active)
            {
                //Find the location where the item will be located
                var heldItemRoot = mimicModel.transform.Find("Armature/chestArmature/Base/HeldItem");
                if (!heldItemRoot)
                    Debug.Log("Failed to locate held item root for Mimic");
                
                //Create pickup container to hold the object it will contain
                var pickupObj = Instantiate(Mimics.MimicPickupPrefab, heldItemRoot.transform.position, heldItemRoot.transform.rotation);

                //var pickup = mimicModel.GetComponentInChildren<GenericPickupController>();
                var pickup = pickupObj.GetComponent<GenericPickupController>();
                pickup.NetworkpickupIndex = message.item;
                
                pickupObj.transform.SetParent(heldItemRoot);

                NetworkServer.Spawn(pickupObj);
                
                var normMessage = new NormalizeTransformMessage
                {
                    offset = new Vector3(0, -1.3f, 0),
                    normalizedScale = 1.0f,
                    netId = pickupObj.GetComponent<NetworkIdentity>().netId
                };
                normMessage.Send(NetworkDestination.Clients);

                HailstormPlugin.Instance.StartCoroutine(ConfigurePickupAsync(normMessage, pickupObj));
            }
        }

        private static IEnumerator ConfigurePickupAsync(NormalizeTransformMessage message, GameObject pickupObj)
        {
            //Delay is necessary because trying to set the scale on the same frame doesn't work for some reason, probably related to NetworkSpawn sync
            yield return new WaitForFixedUpdate();
            message.Configure(pickupObj);
        }

        private static void RemoveNode(DirectorCore directorCore, NodeGraph.NodeIndex node)
        {
            var arrField = typeof(DirectorCore).GetField("occupiedNodes", BindingFlags.Instance | BindingFlags.NonPublic);
            var nodeRefType = typeof(DirectorCore).GetNestedType("NodeReference", BindingFlags.NonPublic);
            var nodeIndexField = nodeRefType.GetField("nodeIndex", BindingFlags.Instance | BindingFlags.Public);

            var arr = (Array)arrField.GetValue(directorCore);
            int removalIndex = -1;
            for (int i = 0; i < arr.Length; i++)
            {
                var nodeRef = arr.GetValue(i);
                var occupiedNode = (NodeGraph.NodeIndex)nodeIndexField.GetValue(nodeRef);
                if (occupiedNode.nodeIndex == node.nodeIndex)
                {
                    removalIndex = i;
                    break;
                }
            }

            if (removalIndex > -1)
            {
                var newArr = Array.CreateInstance(nodeRefType, arr.Length - 1);
                if (removalIndex > 0)
                    Array.Copy(arr, 0, newArr, 0, removalIndex);
                if (removalIndex < arr.Length - 1)
                    Array.Copy(arr, removalIndex+1, newArr, removalIndex, newArr.Length - removalIndex);

                arrField.SetValue(directorCore, newArr);
            }
        }

        public static ChestMimicSpawner Build(GameObject owner)
        {
            var result = owner.AddComponent<ChestMimicSpawner>();
            var collider = result.gameObject.AddComponent<SphereCollider>();
            collider.radius = 15;
            collider.isTrigger = true;
            return result;
        }
    }
}
