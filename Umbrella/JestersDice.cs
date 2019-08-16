using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemLib;
using RoR2;
using UnityEngine;

namespace JarlykMods.Umbrella
{
    public sealed class JestersDice
    {
        public JestersDice()
        {
            EquipIndex = (EquipmentIndex)ItemLib.ItemLib.GetEquipmentId(EquipNames.JestersDice);
        }

        public EquipmentIndex EquipIndex { get; }

        public void PerformAction()
        {
            var user = LocalUserManager.GetFirstLocalUser();
            var body = user.cachedBody;
            if (body?.master == null)
            {
                Debug.LogError("Jester's Dice: Cannot find local user body!");
                return;
            }

            //TODO: Adjust radius?
            var colliders = Physics.OverlapSphere(body.transform.position, 15, (int)LayerIndex.defaultLayer.mask);
            var droplets = colliders.Select(c => c.GetComponent<PickupDropletController>())
                                        .Where(pdc => pdc != null).ToList();
            foreach (var droplet in droplets)
            {
                //TODO: Reroll droplet
                //TODO: Reroll item in inventory of same tier (or other tier if none of same tier available)
            }

            if (droplets.Count == 0)
            {
                //TODO: Even if no item was rerolled on the ground, still reroll a random item in inventory
                //Will avoid rerolling Gesture
                //This is primarily so that if using with Gesture, will create a 'shuffle' run
            }
        }

        public static CustomEquipment Build()
        {
            UmbrellaAssets.Init();

            var equipDef = new EquipmentDef
            {
                cooldown = 30f,
                pickupModelPath = "",
                pickupIconPath = "",
                nameToken = EquipNames.JestersDice,
                descriptionToken = "Jester's Dice",
                canDrop = true,
                enigmaCompatible = true
            };

            //TODO
            GameObject prefab = null;
            UnityEngine.Object icon = null;
            var rule = new ItemDisplayRule
            {
                ruleType = ItemDisplayRuleType.ParentedPrefab,
                followerPrefab = prefab,
                childName = "Chest",
                localScale = new Vector3(0.15f, 0.15f, 0.15f),
                localAngles = new Vector3(0f, 180f, 0f),
                localPos = new Vector3(-0.35f, -0.1f, 0f)
            };

            return new CustomEquipment(equipDef, prefab, icon, new[] { rule });
        }

    }
}
