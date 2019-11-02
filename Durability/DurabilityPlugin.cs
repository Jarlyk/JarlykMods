using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using EntityStates;
using MiniRpcLib;
using MiniRpcLib.Action;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Durability
{
    [BepInPlugin(PluginGuid, "EquipmentDurability", "0.1.1")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    public sealed class DurabilityPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.durability";
        private const string ModRpcId = "JarlykMods.Durability";
        private const float FullDurability = 100;

        private readonly MiniRpcInstance _miniRpc;
        private readonly IRpcAction<UpdateDurabilityMessage> _cmdUpdateDurability;
        private float _nextDropDurability;
        

        public DurabilityPlugin()
        {
            DurabilityConfig.Init(Config);
            DurabilityAssets.Init();
            _miniRpc = MiniRpc.CreateInstance(ModRpcId);
            _cmdUpdateDurability = _miniRpc.RegisterAction(Target.Client, (Action<NetworkUser, UpdateDurabilityMessage>)OnUpdateDurability);

            On.RoR2.EquipmentSlot.ExecuteIfReady += EquipmentSlotOnExecuteIfReady;
            On.RoR2.GenericPickupController.GrantEquipment += GenericPickupControllerOnGrantEquipment;
            IL.RoR2.PickupDropletController.CreatePickupDroplet += PickupDropletControllerOnCreatePickupDroplet;
            IL.RoR2.PickupDropletController.OnCollisionEnter += PickupDropletControllerOnOnCollisionEnter;

            On.RoR2.UI.EquipmentIcon.Update += EquipmentIconOnUpdate;

            On.RoR2.Console.Awake += (orig, self) =>
            {
                CommandHelper.RegisterCommands(self);
                orig(self);
            };
        }

        private void Awake()
        {
            _nextDropDurability = FullDurability;
        }

        private void OnUpdateDurability(NetworkUser user, UpdateDurabilityMessage message)
        {
            var master = LocalUserManager.GetFirstLocalUser().cachedMasterObject;
            if (master != null)
            {
                var tracker = master.GetComponent<DurabilityTracker>();
                if (tracker == null)
                {
                    tracker = master.gameObject.AddComponent<DurabilityTracker>();
                }

                tracker.durability = message.durability;
                tracker.durabilityAlt = message.durabilityAlt;
            }
        }

        private void EquipmentIconOnUpdate(On.RoR2.UI.EquipmentIcon.orig_Update orig, EquipmentIcon self)
        {
            orig(self);

            var feedback = self.GetComponent<DurabilityFeedback>();
            if (feedback == null)
            {
                feedback = self.gameObject.AddComponent<DurabilityFeedback>();
            }

            var master = self.playerCharacterMasterController?.master;
            if (master != null)
            {
                var tracker = master.GetComponent<DurabilityTracker>();
                if (tracker == null)
                {
                    tracker = master.gameObject.AddComponent<DurabilityTracker>();
                    tracker.durability = FullDurability;
                    tracker.durabilityAlt = FullDurability;
                }

                bool useAlt = self.targetEquipmentSlot.activeEquipmentSlot == 0
                    ? self.displayAlternateEquipment
                    : !self.displayAlternateEquipment;
                feedback.percentDurability = useAlt ? tracker.durabilityAlt : tracker.durability;
                feedback.showBar = self.hasEquipment;
            }
        }

        private void GenericPickupControllerOnGrantEquipment(On.RoR2.GenericPickupController.orig_GrantEquipment orig, GenericPickupController self, CharacterBody body, Inventory inventory)
        {
            if (NetworkServer.active)
            {
                var tracker = body.master.GetComponent<DurabilityTracker>();
                if (tracker != null)
                {
                    _nextDropDurability = inventory.activeEquipmentSlot == 0 ? tracker.durability : tracker.durabilityAlt;
                }
                else
                {
                    _nextDropDurability = FullDurability;
                }
            }
            orig(self, body, inventory);
            if (NetworkServer.active)
            {
                float durability = FullDurability;
                var tracker = self.gameObject.GetComponent<DurabilityTracker>();
                if (tracker != null)
                {
                    durability = tracker.durability;
                    tracker.durability = _nextDropDurability;
                    _nextDropDurability = FullDurability;
                }

                var masterTracker = body.master.GetComponent<DurabilityTracker>();
                if (masterTracker == null)
                {
                    masterTracker = body.masterObject.AddComponent<DurabilityTracker>();
                }
                if (inventory.activeEquipmentSlot == 0)
                {
                    masterTracker.durability = durability;
                }
                else
                {
                    masterTracker.durabilityAlt = durability;
                }

                var networkUser = body.master?.playerCharacterMasterController?.networkUser;
                if (networkUser != null && !networkUser.isLocalPlayer)
                {
                    var message = new UpdateDurabilityMessage
                    {
                        durability = masterTracker.durability,
                        durabilityAlt = masterTracker.durabilityAlt
                    };
                    _cmdUpdateDurability.Invoke(message, networkUser);
                }
            }
        }

        private void PickupDropletControllerOnCreatePickupDroplet(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(i => i.MatchCall("UnityEngine.Networking.NetworkServer", "Spawn"));

            c.Emit(OpCodes.Dup);
            c.EmitDelegate<Action<GameObject>>(obj =>
            {
                if (NetworkServer.active)
                {
                    var controller = obj.GetComponent<PickupDropletController>();
                    if (controller != null && PickupCatalog.GetPickupDef(controller.pickupIndex).equipmentIndex != EquipmentIndex.None)
                    {
                        var tracker = obj.AddComponent<DurabilityTracker>();
                        tracker.durability = _nextDropDurability;
                        _nextDropDurability = FullDurability;
                    }
                }
            });
        }

        private void PickupDropletControllerOnOnCollisionEnter(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(i => i.MatchCall("UnityEngine.Networking.NetworkServer", "Spawn"));
            c.Emit(OpCodes.Dup);
            c.Index++;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<GameObject, PickupDropletController>>(TransferTracker);
        }

        private void TransferTracker(GameObject newObj, PickupDropletController controller)
        {
            if (newObj == null || controller == null || !NetworkServer.active)
                return;

            var tracker = controller.GetComponent<DurabilityTracker>();
            if (tracker != null)
            {
                var newTracker = newObj.AddComponent<DurabilityTracker>();
                newTracker.durability = tracker.durability;
            }
            else if (PickupCatalog.GetPickupDef(controller.pickupIndex).equipmentIndex != EquipmentIndex.None)
            {
                var newTracker = newObj.AddComponent<DurabilityTracker>();
                newTracker.durability = FullDurability;
            }
        }

        private bool EquipmentSlotOnExecuteIfReady(On.RoR2.EquipmentSlot.orig_ExecuteIfReady orig, EquipmentSlot self)
        {
            var origStock = self.stock;
            var executed = orig(self);

            if (executed && self.stock < origStock)
            {
                var tracker = self.characterBody.master.GetComponent<DurabilityTracker>();
                if (tracker == null)
                {
                    tracker = self.characterBody.masterObject.AddComponent<DurabilityTracker>();
                }

                var durability = self.activeEquipmentSlot == 0 ? tracker.durability : tracker.durabilityAlt;

                var inv = self.characterBody.inventory;
                var equipDef = inv.currentEquipmentState.equipmentDef;
                var lifetime = equipDef.isLunar
                    ? DurabilityConfig.LunarEquipLifetime.Value
                    : DurabilityConfig.RegEquipLifetime.Value;
                var decay = 100*equipDef.cooldown/lifetime;
                var fuelCells = inv.GetItemCount(ItemIndex.EquipmentMagazine);
                if (fuelCells > 0)
                    decay *= Mathf.Pow(0.85f, fuelCells);
                durability -= decay;

                if (durability <= 0)
                {
                    self.characterBody.inventory.SetEquipment(EquipmentState.empty, self.activeEquipmentSlot);
                    durability = FullDurability;
                }

                if (self.activeEquipmentSlot == 0)
                {
                    tracker.durability = durability;
                }
                else
                {
                    tracker.durabilityAlt = durability;
                }

                var networkUser = self.characterBody.master?.playerCharacterMasterController?.networkUser;
                if (networkUser != null && !networkUser.isLocalPlayer)
                {
                    var message = new UpdateDurabilityMessage
                    {
                        durability = tracker.durability,
                        durabilityAlt = tracker.durabilityAlt
                    };
                    _cmdUpdateDurability.Invoke(message, networkUser);
                }
            }
            
            return executed;
        }
    }
}
