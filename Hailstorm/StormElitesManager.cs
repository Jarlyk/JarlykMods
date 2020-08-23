using System;
using System.Collections.Generic;
using System.Text;
using EliteSpawningOverhaul;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    public sealed class StormElitesManager
    {
        public const string EliteName = "Storm";
        public const string BuffName = "Affix_Storm";
        public const string EquipName = "StormBringer";

        public StormElitesManager()
        {
            var eliteDef = new EliteDef
            {
                name = EliteName,
                modifierToken = "ELITE_MODIFIER_STORM",
                color = new Color32(210, 180, 140, 255)
            };
            var equipDef = new EquipmentDef
            {
                name = EquipName,
                cooldown = 10f,
                pickupModelPath = "",
                pickupIconPath = HailstormAssets.IconStormElite,
                nameToken = EquipName,
                pickupToken = "StormBringer",
                descriptionToken = "StormBringer",
                canDrop = false,
                enigmaCompatible = false
            };
            var buffDef = new BuffDef
            {
                name = BuffName,
                buffColor = eliteDef.color,
                iconPath = HailstormAssets.IconStormElite,
                canStack = false
            };

            var equip = new CustomEquipment(equipDef, new ItemDisplayRule[0]);
            var buff = new CustomBuff(buffDef);
            var elite = new CustomElite(eliteDef, 1);

            EliteIndex = EliteAPI.Add(elite);
            BuffIndex = BuffAPI.Add(buff);
            EquipIndex = ItemAPI.Add(equip);
            eliteDef.eliteEquipmentIndex = EquipIndex;
            equipDef.passiveBuff = BuffIndex;
            buffDef.eliteIndex = EliteIndex;

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

            //Description of elite in UI when boss
            LanguageAPI.Add(eliteDef.modifierToken, "StormBringer {0}");
            //eliteDef.modifierToken = "StormBringer {0}";
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
    }
}
