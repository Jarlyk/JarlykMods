using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Schema;
using BepInEx;
using BepInEx.Configuration;
using EntityStates;
using ItemLib;
using RoR2;
using UnityEngine;
using MiniRpcLib;
using MiniRpcLib.Action;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.CharacterAI;
using RoR2.Projectile;
using UnityEngine.Networking;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

// ReSharper disable UnusedMember.Local

namespace TestModJarlyk
{
    [BepInPlugin(PluginGuid, "Umbrella", "0.1.0")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(ItemLib.ItemLibPlugin.ModGuid)]
    public class UmbrellaPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.umbrella";

        private readonly EquipmentIndex _idxBulletTimer;
        private float _bulletTimerStartTime;

        public UmbrellaPlugin()
        {
            //TODO: Filter out allied projectiles

            _idxBulletTimer = (EquipmentIndex)ItemLib.ItemLib.GetEquipmentId(EquipNames.BulletTimer);
            On.RoR2.EquipmentSlot.PerformEquipmentAction += EquipmentSlotOnPerformEquipmentAction;
            IL.RoR2.Projectile.ProjectileManager.FireProjectileServer += ProjectileManagerOnFireProjectileServer;
            _bulletTimerStartTime = float.NaN;
        }

        private void ProjectileManagerOnFireProjectileServer(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, x => x.MatchCall("RoR2.Projectile.ProjectileManager","InitializeProjectile"));
            cursor.Emit(OpCodes.Ldloc_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Action<GameObject, FireProjectileInfo>>(AfterInitializeProjectile);
        }

        private bool EquipmentSlotOnPerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentIndex index)
        {
            if (index == _idxBulletTimer)
            {
                _bulletTimerStartTime = Time.time;
                return true;
            }

            return orig(self, index);
        }

        [Item(ItemAttribute.ItemType.Equipment)]
        public static CustomEquipment EquipmentBulletTimer()
        {
            //var bundle = AssetBundle.LoadFromFile(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/umbrellaassets");
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JarlykMods.Umbrella.umbrellaassets");
            var bundle = AssetBundle.LoadFromStream(stream);

            var prefab = bundle.LoadAsset<GameObject>("Assets/Import/bullet_timer/bullet_timer.prefab");
            var icon = bundle.LoadAsset<UnityEngine.Object>("Assets/Import/bullet_timer/bullet_timer_icon.png");

            var equipDef = new EquipmentDef
            {
                cooldown = 40f,
                pickupModelPath = "",
                pickupIconPath = "",
                nameToken = EquipNames.BulletTimer,
                descriptionToken = "Bullet Timer",
                canDrop = true,
                enigmaCompatible = true
            };

            return new CustomEquipment(equipDef, prefab, icon);
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
                var pickupIndex = new PickupIndex(_idxBulletTimer);
                PickupDropletController.CreatePickupDroplet(pickupIndex,
                                                            charTransform.position, Vector3.up * 20f + charTransform.forward * 10f);
            }
        }

        private void AfterInitializeProjectile(GameObject controllerObj, FireProjectileInfo info)
        {
            if (float.IsNaN(_bulletTimerStartTime) || (Time.time - _bulletTimerStartTime) > 8f)
                return;

            //Allied projectiles are not slowed
            var teamFilter = controllerObj.GetComponent<TeamFilter>();
            if (teamFilter?.teamIndex == TeamIndex.Player)
                return;

            var simple = controllerObj.GetComponent<ProjectileSimple>();
            if (simple != null)
            {
                simple.velocity *= 0.1f;
                simple.lifetime *= 10f;
            }
        }

        public static class EquipNames
        {
            public const string BulletTimer = "BulletTimer";
        }
    }
}