using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using ItemLib;
using RoR2;
using UnityEngine;

namespace JarlykMods.Hailstorm
{
    [BepInPlugin(PluginGuid, "Hailstorm", "0.1.0")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(ItemLibPlugin.ModGuid)]
    public sealed class HailstormPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.hailstorm";

        private readonly DarkElitesManager _darkElites;
        private readonly BarrierElitesManager _barrierElites;
        private readonly Mimics _mimics;

        public HailstormPlugin()
        {
            HailstormConfig.Init(Config);
            HailstormAssets.Init();

            if (HailstormConfig.EnableDarkElites.Value)
                _darkElites = new DarkElitesManager();

            if (HailstormConfig.EnableBarrierElites.Value)
                _barrierElites = new BarrierElitesManager();

            if (HailstormConfig.EnableMimics.Value)
                _mimics = new Mimics();
        }

        private void Awake()
        {
            _darkElites?.Awake();
            _barrierElites?.Awake();
        }

        private void Update()
        {
            _darkElites?.Update();
            _barrierElites?.Update();
        }

        [Item(ItemAttribute.ItemType.Elite)]
        public static CustomElite BuildDarkElite()
        {
            HailstormAssets.Init();

            var eliteDef = new EliteDef
            {
                modifierToken = DarkElitesManager.EliteName,
                color = new Color32(0, 0, 0, 255),
            };
            var equipDef = new EquipmentDef
            {
                cooldown = 10f,
                pickupModelPath = "",
                pickupIconPath = "",
                nameToken = DarkElitesManager.EquipName,
                pickupToken = "Darkness",
                descriptionToken = "Night-bringer",
                canDrop = false,
                enigmaCompatible = false
            };
            var buffDef = new BuffDef
            {
                buffColor = new Color32(255, 255, 255, 255),
                canStack = false
            };

            var equip = new CustomEquipment(equipDef, null, null, new ItemDisplayRule[0]);
            var buff = new CustomBuff(DarkElitesManager.BuffName, buffDef, HailstormAssets.IconDarkElite);
            var elite = new CustomElite(DarkElitesManager.EliteName, eliteDef, equip, buff, 1);
            return elite;
        }

        [Item(ItemAttribute.ItemType.Elite)]
        public static CustomElite BuildBarrierElite()
        {
            HailstormAssets.Init();

            var eliteDef = new EliteDef
            {
                modifierToken = BarrierElitesManager.EliteName,
                color = new Color32(162, 179, 241, 255)
            };
            var equipDef = new EquipmentDef
            {
                cooldown = 10f,
                pickupModelPath = "",
                pickupIconPath = "",
                nameToken = BarrierElitesManager.EquipName,
                pickupToken = "Shield-Bearer",
                descriptionToken = "Shield-Bearer",
                canDrop = false,
                enigmaCompatible = false
            };
            var buffDef = new BuffDef
            {
                buffColor = eliteDef.color,
                canStack = false
            };

            var equip = new CustomEquipment(equipDef, null, null, new ItemDisplayRule[0]);
            var buff = new CustomBuff(BarrierElitesManager.BuffName, buffDef, HailstormAssets.IconBarrierElite);
            var elite = new CustomElite(BarrierElitesManager.EliteName, eliteDef, equip, buff, 1);
            return elite;
        }
    }
}
