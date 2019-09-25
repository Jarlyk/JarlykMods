using System;
using System.Collections.Generic;
using System.Text;
using ItemLib;
using RoR2;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.Projectile;

namespace JarlykMods.Umbrella
{
    public sealed class BulletTimer
    {
        private float _startTime;

        public BulletTimer()
        {
            EquipIndex = (EquipmentIndex)ItemLib.ItemLib.GetEquipmentId(EquipNames.BulletTimer);
            IL.RoR2.Projectile.ProjectileManager.FireProjectileServer += ProjectileManagerOnFireProjectileServer;
            _startTime = float.NaN;

        }

        public EquipmentIndex EquipIndex { get; }

        public void PerformAction(CharacterBody body)
        {
            _startTime = Time.time;
            AkSoundEngine.PostEvent(SoundEvents.PlayBulletTimer, body.gameObject);
        }

        private void ProjectileManagerOnFireProjectileServer(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, x => x.MatchCall("RoR2.Projectile.ProjectileManager", "InitializeProjectile"));
            cursor.Emit(OpCodes.Ldloc_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Action<GameObject, FireProjectileInfo>>(AfterInitializeProjectile);
        }

        private void AfterInitializeProjectile(GameObject controllerObj, FireProjectileInfo info)
        {
            if (float.IsNaN(_startTime) || (Time.time - _startTime) > 8f)
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

        public static CustomEquipment Build()
        {
            UmbrellaAssets.Init();

            var equipDef = new EquipmentDef
            {
                cooldown = 40f,
                pickupModelPath = "",
                pickupIconPath = "",
                nameToken = EquipNames.BulletTimer,
                descriptionToken = "Bullet Timer",
                pickupToken = "Bullet Timer",
                canDrop = true,
                enigmaCompatible = true
            };

            var rule = new ItemDisplayRule
            {
                ruleType = ItemDisplayRuleType.ParentedPrefab,
                followerPrefab = UmbrellaAssets.BulletTimerPrefab,
                childName = "Chest",
                localScale = new Vector3(0.15f, 0.15f, 0.15f),
                localAngles = new Vector3(0f, 180f, 0f),
                localPos = new Vector3(-0.35f, -0.1f, 0f)
            };

            return new CustomEquipment(equipDef, UmbrellaAssets.BulletTimerPrefab, UmbrellaAssets.BulletTimerIcon, new[] { rule });
        }
    }
}
