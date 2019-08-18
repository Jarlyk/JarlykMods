using BepInEx;
using ItemLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable UnusedMember.Local

namespace JarlykMods.Umbrella
{
    [BepInPlugin(PluginGuid, "Umbrella", "0.1.0")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(ItemLib.ItemLibPlugin.ModGuid)]
    public class UmbrellaPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.umbrella";

        private readonly BulletTimer _bulletTimer;
        private readonly JestersDice _jestersDice;

        public UmbrellaPlugin()
        {
            _bulletTimer = new BulletTimer();
            _jestersDice = new JestersDice();

            On.RoR2.EquipmentSlot.PerformEquipmentAction += EquipmentSlotOnPerformEquipmentAction;
        }

        private void Awake()
        {
            _jestersDice?.Awake();
        }

        private bool EquipmentSlotOnPerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentIndex index)
        {
            if (index == _bulletTimer.EquipIndex)
            {
                _bulletTimer.PerformAction();
                return true;
            }

            if (index == _jestersDice.EquipIndex)
            {
                _jestersDice.PerformAction(this);
                return true;
            }

            return orig(self, index);
        }

        [Item(ItemAttribute.ItemType.Equipment)]
        public static CustomEquipment EquipmentBulletTimer()
        {
            return BulletTimer.Build();
        }

        [Item(ItemAttribute.ItemType.Equipment)]
        public static CustomEquipment EquipmentJestersDice()
        {
            return JestersDice.Build();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3) && NetworkServer.active)
            {
                var user = LocalUserManager.GetFirstLocalUser();
                var body = user.cachedBody;
                if (body?.master == null)
                {
                    Debug.LogError("Cannot find local user body!");
                    return;
                }

                var charTransform = body.transform;
                var pickupIndex = new PickupIndex(_jestersDice.EquipIndex);
                PickupDropletController.CreatePickupDroplet(pickupIndex,
                                                            charTransform.position, Vector3.up * 20f + charTransform.forward * 10f);
            }
        }
    }
}