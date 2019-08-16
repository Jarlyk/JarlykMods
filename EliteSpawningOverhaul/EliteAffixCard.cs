using System;
using System.Collections.Generic;
using RoR2;

namespace EliteSpawningOverhaul
{
    /// <summary>
    /// Card specifying the spawn probability, cost and power level of a particular elite type.  Costlier elites
    /// will tend to only show up later in the game, but may still spawn quite often once the difficulty gets high
    /// enough that the CombatDirector has a lot of budget to work with.  You can use <see cref="spawnWeight"/> to
    /// adjust how frequently the elite type spawns when it can afford it and can individually tweak each elite
    /// type's cost/dmg/hp boosts.  For extra fun, consider hooking the onSpawned delegate to add extra power, such
    /// as by granting items.  You can also create multiple cards for the same elite type, with different costs/weights.
    /// </summary>
    public sealed class EliteAffixCard
    {
        public EliteIndex eliteType;

        /// <summary>
        /// Base spawn weight for this elite type; a value of 1.0f makes it equally likely as 'vanilla' elites at this cost
        /// </summary>
        public float spawnWeight = 1.0f;

        /// <summary>
        /// Cost multiplier that this elite type applies to the base spawn card; a value of 6.0f is typical for tier 1 elites
        /// </summary>
        public float costMultiplier = 1.0f;

        /// <summary>
        /// Damage boost effect for this elite type; a value of 2.0f is typical for tier 1 elites
        /// </summary>
        public float damageBoostCoeff = 1.0f;

        /// <summary>
        /// Health boost effect for this elite type; a value of 4.7f is typical for tier 1 elites
        /// </summary>
        public float healthBoostCoeff = 1.0f;

        /// <summary>
        /// Delegate that evaluates whether this elite type can be spawned currently; for example, checking Run.instance.loopClearCount
        /// </summary>
        public Func<bool> isAvailable = () => true;

        /// <summary>
        /// Map of multipliers to spawn weight by spawn card name
        /// </summary>
        public Dictionary<string, float> spawnCardMultipliers = new Dictionary<string, float>();

        /// <summary>
        /// Delegate that will be called after this elite affix is spawned; you can use this to do any extra setup, like granting it items
        /// </summary>
        public Action<CharacterMaster> onSpawned = null;

        /// <summary>
        /// Get the adjusted spawn weight for this card, taking into account
        /// both the base weight and cost for this elite, as well as any card-specific multiplier.
        /// </summary>
        /// <param name="monsterCard">Card to be spawned</param>
        /// <returns>Adjusted spawn weight</returns>
        public float GetSpawnWeight(DirectorCard monsterCard)
        {
            if (!spawnCardMultipliers.TryGetValue(monsterCard.spawnCard.name, out var multiplier))
                multiplier = 1;

            //Note here that we scale by cost (to favor more expensive elites, when affordable)
            //We also square this, to better approximate the vanilla game's strong favoring of greater cost via tiers
            var s = multiplier*costMultiplier;
            return s*s*spawnWeight;
        }
    }
}
