using System;
using System.Collections.Generic;
using MiniRpcLib;
using MiniRpcLib.Action;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Raincoat.ItemDropper
{
    public sealed class RecentItemDropper
    {
        private readonly Dictionary<Inventory, PickupRecord> _pickups = new Dictionary<Inventory, PickupRecord>();
        private readonly IRpcAction<DropRecentItemMessage> _cmdDropItem;

        public RecentItemDropper(MiniRpcInstance miniRpc)
        {
            _cmdDropItem = miniRpc.RegisterAction(Target.Server, (Action<NetworkUser, DropRecentItemMessage>)DoDropItem);

            On.RoR2.GenericPickupController.GrantItem += GenericPickupControllerGrantItem;
        }

        public void DropRecentItem()
        {
            if (!NetworkServer.active)
            {
                var message = new DropRecentItemMessage();
                _cmdDropItem.Invoke(message);
            }
            else
            {
                var player = PlayerCharacterMasterController.instances[0];
                var inventory = player.master.inventory;
                if (!_pickups.TryGetValue(inventory, out var pickup))
                    return;

                if (pickup.ItemIndex == ItemIndex.None || Time.time - pickup.PickupTime > 10)
                    return;

                // Server, execute command
                var characterBody = inventory.GetComponent<CharacterMaster>().GetBody();
                var charTransform = characterBody.transform;
                var pickupIndex = new PickupIndex(pickup.ItemIndex);

                DropItem(charTransform, inventory, pickupIndex);
            }
        }

        private void GenericPickupControllerGrantItem(On.RoR2.GenericPickupController.orig_GrantItem orig, RoR2.GenericPickupController self, CharacterBody body, Inventory inventory)
        {
            orig(self, body, inventory);
            _pickups[inventory] = new PickupRecord(Time.time, self.pickupIndex.itemIndex);
        }

        private void DoDropItem(NetworkUser user, DropRecentItemMessage message)
        {
            var master = user.master;
            if (master == null)
                return;

            var body = master.GetBody();
            if (body == null)
            {
                Debug.LogError("Cannot find network user's body!");
                return;
            }

            var inventory = master.inventory;
            var charTransform = body.transform;
            if (!_pickups.TryGetValue(inventory, out var pickup))
            {
                Debug.LogError("Received drop item request prior to any pickup");
                return;
            }

            if (pickup.ItemIndex == ItemIndex.None || Time.time - pickup.PickupTime > 10)
            {
                Debug.LogError("Received drop item request with no valid recent pickup");
                return;
            }

            Debug.Log($"Dropping item \'{pickup.ItemIndex}\' for player \'{master.name}\'");
            var pickupIndex = new PickupIndex(pickup.ItemIndex);
            DropItem(charTransform, inventory, pickupIndex);
        }
        
        private bool DropItem(Transform charTransform, Inventory inventory, PickupIndex pickupIndex)
        {
            _pickups[inventory] = new PickupRecord(Time.time, ItemIndex.None);

            //if (!inventory.hasAuthority)
            //    return false;

            if (pickupIndex.equipmentIndex != EquipmentIndex.None)
            {
                if (inventory.GetEquipmentIndex() != pickupIndex.equipmentIndex)
                {
                    return false;
                }

                inventory.SetEquipmentIndex(EquipmentIndex.None);
            }
            else
            {
                if (inventory.GetItemCount(pickupIndex.itemIndex) <= 0)
                {
                    return false;
                }

                inventory.RemoveItem(pickupIndex.itemIndex, 1);
            }

            PickupDropletController.CreatePickupDroplet(pickupIndex,
                                                        charTransform.position, Vector3.up * 20f + charTransform.forward * 10f);
            return true;
        }

    }
}