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

        public HailstormPlugin()
        {
            HailstormAssets.Init();

            const bool darkElitesEnabled = true;
            if (darkElitesEnabled)
            {
                _darkElites = new DarkElitesManager();
            }
        }

        private void Awake()
        {
            _darkElites?.Awake();
        }

        private void Update()
        {
            _darkElites?.Update();
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
                descriptionToken = "",
                canDrop = false,
                enigmaCompatible = false
            };
            var buffDef = new BuffDef
            {
                buffColor = eliteDef.color,
                canStack = false
            };

            var equip = new CustomEquipment(equipDef, null, null, new ItemDisplayRule[0]);
            var buff = new CustomBuff(DarkElitesManager.BuffName, buffDef, null);
            var elite = new CustomElite(DarkElitesManager.EliteName, eliteDef, equip, buff, 1);
            return elite;
        }
    }
}
