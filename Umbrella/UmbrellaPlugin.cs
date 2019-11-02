using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable UnusedMember.Local

namespace JarlykMods.Umbrella
{
    [BepInPlugin(PluginGuid, "Umbrella", "0.2.2")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    public class UmbrellaPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.umbrella";

        private readonly BulletTimer _bulletTimer;
        private readonly JestersDice _jestersDice;

        public UmbrellaPlugin()
        {
            UmbrellaAssets.Init();
            _bulletTimer = new BulletTimer();
            _jestersDice = new JestersDice();

            On.RoR2.EquipmentSlot.PerformEquipmentAction += EquipmentSlotOnPerformEquipmentAction;

            On.RoR2.Console.Awake += (orig, self) =>
            {
                CommandHelper.RegisterCommands(self);
                orig(self);
            };
        }

        private void Awake()
        {
            _jestersDice?.Awake();
        }

        private bool EquipmentSlotOnPerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentIndex index)
        {
            if (index == BulletTimer.EquipIndex)
            {
                _bulletTimer.PerformAction(self.characterBody);
                return true;
            }

            if (index == JestersDice.EquipIndex)
            {
                _jestersDice.PerformAction(this, self.characterBody);
                return true;
            }

            return orig(self, index);
        }

        [ConCommand(commandName = "umb_spawn_equip", flags = ConVarFlags.ExecuteOnServer, helpText="Spawn Umbrella Equipment")]
        private static void SpawnEquip(ConCommandArgs args)
        {
            if (args.Count < 1)
                return;

            if (args[0].ToLower().StartsWith("b"))
                SpawnEquip(BulletTimer.EquipIndex);
            else if (args[0].ToLower().StartsWith("j"))
                SpawnEquip(JestersDice.EquipIndex);
        }

        private static void SpawnEquip(EquipmentIndex equip)
        {
            if (NetworkServer.active)
            {
                var user = LocalUserManager.GetFirstLocalUser();
                var body = user.cachedBody;
                if (body?.master == null)
                {
                    Debug.LogError("Cannot find local user body!");
                    return;
                }

                var charTransform = body.transform;
                var pickupIndex = PickupCatalog.FindPickupIndex(equip);
                PickupDropletController.CreatePickupDroplet(pickupIndex,
                                                            charTransform.position,
                                                            Vector3.up * 20f + charTransform.forward * 10f);
            }
        }
    }
}