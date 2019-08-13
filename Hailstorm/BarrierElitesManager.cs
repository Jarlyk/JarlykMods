using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EliteSpawningOverhaul;
using ItemLib;
using RoR2;
using UnityEngine;

namespace JarlykMods.Hailstorm
{
    public sealed class BarrierElitesManager
    {
        public const string EliteName = "Barrier";
        public const string BuffName = "Affix_Barrier";
        public const string EquipName = "ShieldBearer";

        private readonly EliteAffixCard _card;
        private readonly EliteIndex _eliteIndex;
        private readonly BuffIndex _buffIndex;
        private readonly EquipmentIndex _equipIndex;
        private float _lastBarrierTime;

        public BarrierElitesManager()
        {
            //Custom items should be registered now, so grab their indices
            _eliteIndex = (EliteIndex) ItemLib.ItemLib.GetEliteId(EliteName);
            _buffIndex = (BuffIndex) ItemLib.ItemLib.GetBuffId(BuffName);
            _equipIndex = (EquipmentIndex) ItemLib.ItemLib.GetEquipmentId(EquipName);

            //Barrier elites are a bit more uncommon than regular tier 1 elites
            //They're also a bit tankier than usual, but not more damaging
            var card = new EliteAffixCard
            {
                spawnWeight = 0.5f,
                costMultiplier = 10.0f,
                damageBoostCoeff = 1.0f,
                healthBoostCoeff = 10.0f,
                eliteType = _eliteIndex
            };

            //Register the card for spawning if ESO is enabled
            EsoLib.Cards.Add(card);
            _card = card;
        }

        public void Awake()
        {
            _lastBarrierTime = Time.time;
        }

        public void Update()
        {
            if (Time.time - _lastBarrierTime > 3.0f)
            {
                UpdateBarrier();
                _lastBarrierTime = Time.time;
            }
        }

        private void UpdateBarrier()
        {
            const float barrierRadius = 25f;
            var barrierR2 = barrierRadius*barrierRadius;

            var allBodies = CharacterBody.readOnlyInstancesList;
            var shieldBearers = allBodies.Where(b => b.HasBuff(_buffIndex));
            foreach (var shieldBearer in shieldBearers)
            {
                var allies = allBodies.Where(b => b.teamComponent?.teamIndex == shieldBearer.teamComponent?.teamIndex);
                foreach (var ally in allies)
                {
                    //Apply to in-range allies, but only if they're not barrier generators themselves
                    //Preventing barrier on barrier generators is intended to avoid frustrating feedback loops resulting in unkillable elites
                    var distSq = (ally.corePosition - shieldBearer.corePosition).sqrMagnitude;
                    if (distSq <= barrierR2 && !ally.HasBuff(_buffIndex))
                    {
                        ally.healthComponent?.AddBarrier(0.25f*ally.maxHealth);
                    }
                }
            }
        }
    }
}
