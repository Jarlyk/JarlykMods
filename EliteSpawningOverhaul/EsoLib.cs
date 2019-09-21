using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace EliteSpawningOverhaul
{
    /// <summary>
    /// Provides a toolset for customizing elite spawning on a per-elite basis; this may be disabled in the ItemLib configuration,
    /// so consumers of this class may look at the <see cref="Enabled"/> property to check this.  Cards are automatically created
    /// for the vanilla elite types with parameters matching those from the vanilla game.
    /// </summary>
    public static class EsoLib
    {
        private static readonly Dictionary<CombatDirector, EliteAffixCard> ChosenAffix = new Dictionary<CombatDirector, EliteAffixCard>();

        internal static void Init()
        {
            On.RoR2.CombatDirector.PrepareNewMonsterWave += CombatDirectorOnPrepareNewMonsterWave;
            IL.RoR2.CombatDirector.AttemptSpawnOnTarget += CombatDirectorOnAttemptSpawnOnTarget;

            //We also need to override usages of highestEliteCostMultiplier, but there's no built-in hook for this
            //We have to use MonoMod directly
            var method = typeof(CombatDirector).GetMethod("get_highestEliteCostMultiplier");
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(method, (Action<ILContext>)CombatDirectorGetHighestEliteCostMultiplier);

            //Create default cards for vanilla elites
            Cards.Add(new EliteAffixCard
            {
                spawnWeight = 1.0f,
                costMultiplier = 6.0f,
                damageBoostCoeff = 2.0f,
                healthBoostCoeff = 4.7f,
                eliteType = EliteIndex.Fire
            });
            Cards.Add(new EliteAffixCard
            {
                spawnWeight = 1.0f,
                costMultiplier = 6.0f,
                damageBoostCoeff = 2.0f,
                healthBoostCoeff = 4.7f,
                eliteType = EliteIndex.Ice
            });
            Cards.Add(new EliteAffixCard
            {
                spawnWeight = 1.0f,
                costMultiplier = 6.0f,
                damageBoostCoeff = 2.0f,
                healthBoostCoeff = 4.7f,
                eliteType = EliteIndex.Lightning
            });
            Cards.Add(new EliteAffixCard
            {
                spawnWeight = 1.0f,
                costMultiplier = 36.0f,
                damageBoostCoeff = 6.0f,
                healthBoostCoeff = 23.5f,
                eliteType = EliteIndex.Poison,
                isAvailable = () => Run.instance.loopClearCount > 0
            });
            Cards.Add(new EliteAffixCard
            {
                spawnWeight = 1.0f,
                costMultiplier = 36.0f,
                damageBoostCoeff = 6.0f,
                healthBoostCoeff = 23.5f,
                eliteType = EliteIndex.Haunted,
                isAvailable = () => Run.instance.loopClearCount > 0
            });
        }

        /// <summary>
        /// The cards used for assigning Elite affixes to spawned enemies; note that you may register multiple cards with the same EliteIndex,
        /// which can be useful for creating different 'tiers' of the same elite type, with stat boosts or other customization using the onSpawned delegate.
        /// </summary>
        public static List<EliteAffixCard> Cards { get; } = new List<EliteAffixCard>();

        /// <summary>
        /// Spawn a particular elite type with the specified monster type at the specified location.  This will
        /// apply appropriate HP and Dmg scaling per the specifications on the affix card, as well as calling the affix onSpawned.
        /// This is primarily intended for testing, but could also be used to easily spawn elites for other purposes.
        /// Note that this does not set XP and Gold rewards, as it does not have access to the cost function; you will need to
        /// add those yourself if you want these.
        /// </summary>
        /// <param name="spawnCard">Card describing the type of monster to spawn</param>
        /// <param name="affixCard">Card describing the type of elite to spawn; may pass null to spawn a non-elite</param>
        /// <param name="placement">How to place the elite in the scene</param>
        /// <param name="rng">Random number generator to use for placement</param>
        /// <returns></returns>
        public static CharacterMaster TrySpawnElite(CharacterSpawnCard spawnCard, EliteAffixCard affixCard, DirectorPlacementRule placement, Xoroshiro128Plus rng)
        {
            var spawnRequest = new DirectorSpawnRequest(spawnCard, placement, rng)
            {
                teamIndexOverride = TeamIndex.Monster,
                ignoreTeamMemberLimit = true
            };
            var spawned = DirectorCore.instance.TrySpawnObject(spawnRequest);
            if (spawned == null)
                return null;

            //Configure as the chosen elite
            var spawnedMaster = spawned.GetComponent<CharacterMaster>();
            if (affixCard != null)
            {
                //Elites are boosted
                var healthBoost = affixCard.healthBoostCoeff;
                var damageBoost = affixCard.damageBoostCoeff;

                spawnedMaster.inventory.GiveItem(ItemIndex.BoostHp, Mathf.RoundToInt((float)((healthBoost - 1.0) * 10.0)));
                spawnedMaster.inventory.GiveItem(ItemIndex.BoostDamage, Mathf.RoundToInt((float)((damageBoost - 1.0) * 10.0)));
                var eliteDef = EliteCatalog.GetEliteDef(affixCard.eliteType);
                if (eliteDef != null)
                    spawnedMaster.inventory.SetEquipmentIndex(eliteDef.eliteEquipmentIndex);

                affixCard.onSpawned?.Invoke(spawnedMaster);
            }
            return spawnedMaster;
        }

        public static EliteAffixCard ChooseEliteAffix(DirectorCard monsterCard, double monsterCredit, Xoroshiro128Plus rng)
        {
            if (((CharacterSpawnCard) monsterCard.spawnCard).noElites)
                return null;

            var eliteSelection = new WeightedSelection<EliteAffixCard>();

            foreach (var card in Cards)
            {
                var weight = card.GetSpawnWeight(monsterCard);
                if (weight > 0 && card.isAvailable())
                {
                    var cost = monsterCard.cost*card.costMultiplier;
                    if (cost <= monsterCredit)
                    {
                        eliteSelection.AddChoice(card, weight);
                    }
                }
            }

            if (eliteSelection.Count > 0)
            {
                var card = eliteSelection.Evaluate(rng.nextNormalizedFloat);
                return card;
            }

            return null;
        }

        private static void CombatDirectorOnPrepareNewMonsterWave(On.RoR2.CombatDirector.orig_PrepareNewMonsterWave orig, CombatDirector self, DirectorCard monsterCard)
        {
            //NOTE: We're completely rewriting this method, so we don't call back to the orig

            self.SetFieldValue("currentMonsterCard", monsterCard);
            ChosenAffix[self] = null;
            if (!((CharacterSpawnCard) monsterCard.spawnCard).noElites)
            {
                var eliteSelection = new WeightedSelection<EliteAffixCard>();

                foreach (var card in Cards)
                {
                    var weight = card.GetSpawnWeight(monsterCard);
                    if (weight > 0 && card.isAvailable())
                    {
                        var cost = monsterCard.cost*card.costMultiplier;
                        if (cost <= self.monsterCredit)
                        {
                            eliteSelection.AddChoice(card, weight);
                        }
                    }
                }

                if (eliteSelection.Count > 0)
                {
                    var rng = self.GetFieldValue<Xoroshiro128Plus>("rng");
                    var card = eliteSelection.Evaluate(rng.nextNormalizedFloat);
                    ChosenAffix[self] = card;
                }
            }

            self.lastAttemptedMonsterCard = monsterCard;
            self.SetFieldValue("spawnCountInCurrentWave", 0);
        }

        private delegate EliteIndex GetNextEliteDel(CombatDirector self, ref float cost, out float scaledCost);

        private delegate void GetCoeffsDel(CombatDirector self, out float hpCoeff, out float dmgCoeff);

        private static void CombatDirectorOnAttemptSpawnOnTarget(ILContext il)
        {
            var c = new ILCursor(il);

            //First, we rewrite the section of the code that haandles scaling cost by elite tier, to see if it can spawn as elite
            //Since cost scaling is now part of the card, we use that instead of the tier def
            c.GotoNext(i => i.MatchLdarg(0),
                       i => i.MatchLdfld("RoR2.CombatDirector", "currentActiveEliteTier"));

            //Getting the next elite also needs to set some local variables as side effects for the rest of the code
            //Thus, the ugly-looking delegate
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca_S, (byte) 0);
            c.Emit(OpCodes.Ldloca_S, (byte) 1);
            c.EmitDelegate<GetNextEliteDel>(GetNextElite);
            c.Emit(OpCodes.Stloc_3);
            
            //And we'll need to skip over the code in the original method
            var skip1 = c.DefineLabel();
            c.Emit(OpCodes.Br, skip1);
            c.GotoNext(i => i.MatchLdarg(0),
                       i => i.MatchLdfld("RoR2.CombatDirector", "currentMonsterCard"));
            c.MarkLabel(skip1);

            //The original code now spawns the actual monster and assigns it to a squad, if applicable
            //We're going to modify once it reaches the part where it starts applying the Elite-related changes
            c.GotoNext(i => i.MatchLdloc(2),
                       i => i.MatchLdfld("RoR2.CombatDirector/EliteTierDef", "healthBoostCoefficient"));

            //From here we need to back up a bit due to a brfalse that points to this instruction
            var fixBranch = c.DefineLabel();
            c.GotoPrev(i => i.MatchBrfalse(out var oldLabel));
            c.Next.OpCode = OpCodes.Brfalse;
            c.Next.Operand = fixBranch;

            c.GotoNext(i => i.MatchLdloc(2),
                       i => i.MatchLdfld("RoR2.CombatDirector/EliteTierDef", "healthBoostCoefficient"));

            c.MarkLabel(fixBranch);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca_S, (byte) 8);
            c.Emit(OpCodes.Ldloca_S, (byte) 9);
            c.EmitDelegate<GetCoeffsDel>(GetCoeffs);
            var skip2 = c.DefineLabel();
            c.Emit(OpCodes.Br, skip2);
            c.Index += 6;
            c.MarkLabel(skip2);

            //Finally, just before it launches the spawnEffect, we'll give the card's creator a chance to apply changes to the character
            c.GotoNext(i => i.MatchLdarg(0),
                       i => i.MatchLdfld("RoR2.CombatDirector", "spawnEffectPrefab"));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_S, (byte) 10);
            c.EmitDelegate<Action<CombatDirector, CharacterMaster>>(RunOnSpawn);
        }

        private static EliteIndex GetNextElite(CombatDirector self, ref float cost, out float scaledCost)
        {
            ChosenAffix.TryGetValue(self, out var affix);
            scaledCost = affix != null ? affix.costMultiplier*cost : cost;
            if (scaledCost < self.monsterCredit)
            {
                cost = scaledCost;
                return affix?.eliteType ?? EliteIndex.None;
            }

            return EliteIndex.None;
        }

        private static void GetCoeffs(CombatDirector self, out float hpCoeff, out float dmgCoeff)
        {
            ChosenAffix.TryGetValue(self, out var affix);
            if (affix != null)
            {
                hpCoeff = affix.healthBoostCoeff;
                dmgCoeff = affix.damageBoostCoeff;
            }
            else
            {
                hpCoeff = 1;
                dmgCoeff = 1;
            }
        }

        private static void RunOnSpawn(CombatDirector self, CharacterMaster master)
        {
            ChosenAffix.TryGetValue(self, out var affix);
            affix?.onSpawned?.Invoke(master);
        }

        private static void CombatDirectorGetHighestEliteCostMultiplier(ILContext il)
        {
            var c = new ILCursor(il);

            //We completely replace this getter
            c.Goto(0);
            c.EmitDelegate<Func<float>>(GetHighestEliteCostMultiplier);
            c.Emit(OpCodes.Ret);
        }

        private static float GetHighestEliteCostMultiplier()
        {
            return Cards.Count == 0 ? 1.0f : Cards.Max(c => c.costMultiplier);
        }
    }
}
