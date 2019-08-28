﻿using System;
using System.Collections.Generic;
using System.Text;
using EliteSpawningOverhaul;
using ItemLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    public sealed class StormElitesManager
    {
        public const string EliteName = "Storm";
        public const string BuffName = "Affix_Storm";
        public const string EquipName = "Storm Bringer";

        public StormElitesManager()
        {
            EliteIndex = (EliteIndex) ItemLib.ItemLib.GetEliteId(EliteName);
            BuffIndex = (BuffIndex) ItemLib.ItemLib.GetBuffId(BuffName);
            EquipIndex = (EquipmentIndex) ItemLib.ItemLib.GetEquipmentId(EquipName);

            //Storm elites are immune to twisters
            TwisterProjectileController.ImmunityBuff = BuffIndex;
            TornadoLauncher.StormBuff = BuffIndex;

            //Storm elites are tier 2 elites, on the same order as malachites
            //They're a bit less tanky than malachites, but even more dangerous in terms of damage
            var card = new EliteAffixCard
            {
                spawnWeight = 1.0f,
                costMultiplier = 30.0f,
                damageBoostCoeff = 6.0f,
                healthBoostCoeff = 20.0f,
                eliteType = EliteIndex,
                isAvailable = () => Run.instance.loopClearCount > 0,
                onSpawned = OnSpawned
            };

            //Register the card for spawning if ESO is enabled
            EsoLib.Cards.Add(card);
            Card = card;
        }

        public EliteIndex EliteIndex { get; }

        public BuffIndex BuffIndex { get; }

        public EquipmentIndex EquipIndex { get; }

        public EliteAffixCard Card { get; }

        private static void OnSpawned(CharacterMaster master)
        {
            master.inventory.GiveItem(ItemIndex.ChainLightning);
            var bodyObj = master.GetBodyObject();
            var decor = UnityEngine.Object.Instantiate(HailstormAssets.TwisterVisualPrefab, bodyObj.transform);
            decor.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            var launcher = bodyObj.AddComponent<TornadoLauncher>();
            launcher.enabled = true;
        }

        public static CustomElite Build()
        {
            HailstormAssets.Init();

            var eliteDef = new EliteDef
            {
                modifierToken = EliteName,
                color = new Color32(162, 179, 241, 255)
            };
            var equipDef = new EquipmentDef
            {
                cooldown = 10f,
                pickupModelPath = "",
                pickupIconPath = "",
                nameToken = EquipName,
                pickupToken = "Storm Bringer",
                descriptionToken = "Storm Bringer",
                canDrop = false,
                enigmaCompatible = false
            };
            var buffDef = new BuffDef
            {
                buffColor = eliteDef.color,
                canStack = false
            };

            var equip = new CustomEquipment(equipDef, null, null, new ItemDisplayRule[0]);
            var buff = new CustomBuff(BuffName, buffDef, null);
            var elite = new CustomElite(EliteName, eliteDef, equip, buff, 2);
            return elite;
        }
    }
}
