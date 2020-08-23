using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
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
        private static Type _helperClass;

        private const string HelperClassName = "<>c__DisplayClass72_0";

        internal static void Init()
        {
            _helperClass = typeof(CombatDirector).GetNestedType(HelperClassName, BindingFlags.NonPublic);

            On.RoR2.CombatDirector.PrepareNewMonsterWave += CombatDirectorOnPrepareNewMonsterWave;
            IL.RoR2.CombatDirector.AttemptSpawnOnTarget += CombatDirectorOnAttemptSpawnOnTarget;
            
            //We also need to override usages of highestEliteCostMultiplier, but there's no built-in hook for this
            //We have to use MonoMod directly
            var method = typeof(CombatDirector).GetMethod("get_highestEliteCostMultiplier");
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(method, (Action<ILContext>)CombatDirectorGetHighestEliteCostMultiplier);

            //We also need to hook the private compiler method used to implement the submethod used for the delegate in AttemptSpawnOnTarget now
            var subMethod = _helperClass.GetMethod("<AttemptSpawnOnTarget>g__OnCardSpawned|0", BindingFlags.NonPublic | BindingFlags.Instance);
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(subMethod, (Action<ILContext>)CombatDirectorHelperOnCardSpawned);

            On.RoR2.CharacterModel.UpdateMaterials += CharacterModelOnUpdateMaterials;

            //Create default cards for vanilla elites
            Cards.Add(new EliteAffixCard
            {
                spawnWeight = 1.0f,
                costMultiplier = 6.0f,
                damageBoostCoeff = 2.0f,
                healthBoostCoeff = 4f,
                eliteOnlyScaling = 0.5f,
                eliteType = EliteIndex.Fire
            });
            Cards.Add(new EliteAffixCard
            {
                spawnWeight = 1.0f,
                costMultiplier = 6.0f,
                damageBoostCoeff = 2.0f,
                healthBoostCoeff = 4f,
                eliteOnlyScaling = 0.5f,
                eliteType = EliteIndex.Ice
            });
            Cards.Add(new EliteAffixCard
            {
                spawnWeight = 1.0f,
                costMultiplier = 6.0f,
                damageBoostCoeff = 2.0f,
                healthBoostCoeff = 4f,
                eliteOnlyScaling = 0.5f,
                eliteType = EliteIndex.Lightning
            });
            Cards.Add(new EliteAffixCard
            {
                spawnWeight = 1.0f,
                costMultiplier = 36.0f,
                damageBoostCoeff = 6.0f,
                healthBoostCoeff = 18.0f,
                eliteType = EliteIndex.Poison,
                isAvailable = () => Run.instance.loopClearCount > 0
            });
            Cards.Add(new EliteAffixCard
            {
                spawnWeight = 1.0f,
                costMultiplier = 36.0f,
                damageBoostCoeff = 6.0f,
                healthBoostCoeff = 18.0f,
                eliteType = EliteIndex.Haunted,
                isAvailable = () => Run.instance.loopClearCount > 0
            });
        }

        private static void CharacterModelOnUpdateMaterials(On.RoR2.CharacterModel.orig_UpdateMaterials orig, CharacterModel self)
        {
            orig(self);

            //Vanilla elites aren't adjusted
            var eliteIndex = self.GetFieldValue<EliteIndex>("myEliteIndex");
            if (eliteIndex < EliteIndex.Count)
                return;

            var eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
            var rendererInfos = self.baseRendererInfos;
            var propertyStorage = self.GetFieldValue<MaterialPropertyBlock>("propertyStorage");
            for (int i = rendererInfos.Length - 1; i >= 0; --i)
            {
                var baseRendererInfo = rendererInfos[i];
                Renderer renderer = baseRendererInfo.renderer;
                renderer.GetPropertyBlock(propertyStorage);
                propertyStorage.SetColor("_Color", eliteDef.color);
                propertyStorage.SetFloat("_EliteIndex", 0);
                renderer.SetPropertyBlock(propertyStorage);
            }
        }

        /// <summary>
        /// The cards used for assigning Elite affixes to spawned enemies; note that you may register multiple cards with the same EliteIndex,
        /// which can be useful for creating different 'tiers' of the same elite type, with stat boosts or other customization using the onSpawned delegate.
        /// </summary>
        public static List<EliteAffixCard> Cards { get; } = new List<EliteAffixCard>();

        private static bool EliteOnlyArtifactEnabled => RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.eliteOnlyArtifactDef);

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
            
            if (EliteOnlyArtifactEnabled)
            {
                //We have to choose an elite, so just pick any card at random that's available
                return rng.NextElementUniform(Cards.Where(c => c.isAvailable()).ToList());
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
                        var cost = monsterCard.cost*card.costMultiplier*self.eliteBias;
                        if (cost <= self.monsterCredit)
                        {
                            eliteSelection.AddChoice(card, weight);
                        }
                    }
                }

                var rng = self.GetFieldValue<Xoroshiro128Plus>("rng");
                if (eliteSelection.Count > 0)
                {
                    var card = eliteSelection.Evaluate(rng.nextNormalizedFloat);
                    ChosenAffix[self] = card;
                }
                else if (EliteOnlyArtifactEnabled)
                {
                    //We have to choose an elite, so just pick any card at random that's available
                    ChosenAffix[self] = rng.NextElementUniform(Cards.Where(c => c.isAvailable()).ToList());
                }
            }

            self.lastAttemptedMonsterCard = monsterCard;
            self.SetFieldValue("spawnCountInCurrentWave", 0);
        }

        private delegate EliteIndex GetNextEliteDel(CombatDirector self, ref int scaledCost, ref int finalCost);

        private delegate void GetCoeffsDel(CombatDirector self, out float hpCoeff, out float dmgCoeff);

        private static void CombatDirectorOnAttemptSpawnOnTarget(ILContext il)
        {
            var monsterCostField = _helperClass.GetField("monsterCostThatMayOrMayNotBeElite");

            var c = new ILCursor(il);

            //First, we rewrite the section of the code that handles scaling cost by elite tier, to see if it can spawn as elite
            //Since cost scaling is now part of the card, we use that instead of the tier def
            c.GotoNext(i => i.MatchLdloc(0),
                       i => i.MatchLdarg(0),
                       i => i.MatchLdfld("RoR2.CombatDirector", "currentActiveEliteTier"));

            //Get the next elite to store into eliteIndex, also saving the cost in the monsterCost field
            c.Emit(OpCodes.Ldloc_0);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca_S, (byte) 1);
            c.Emit(OpCodes.Ldloc_0);
            c.Emit(OpCodes.Ldflda, monsterCostField);
            c.EmitDelegate<GetNextEliteDel>(GetNextElite);
            c.Emit(OpCodes.Stfld, _helperClass.GetField("eliteIndex"));
            
            //And we'll need to skip over the code in the original method
            var skip1 = c.DefineLabel();
            c.Emit(OpCodes.Br, skip1);
            c.GotoNext(i => i.MatchLdarg(0),
                       i => i.MatchLdfld("RoR2.CombatDirector", "currentMonsterCard"));
            c.MarkLabel(skip1);

            //The rest of the spawn code can work as normal, as it will call the inner OnCardSpawned
            //The elite information is conveyed via the helper class implementing the closure
        }

        private static void CombatDirectorHelperOnCardSpawned(ILContext il)
        {
            var c = new ILCursor(il);

            //The original code starts with setting up squad assignment
            //We're going to modify once it reaches the part where it starts applying the Elite-related changes
            c.GotoNext(i => i.MatchLdarg(0),
                       i => i.MatchLdfld("RoR2.CombatDirector/" + HelperClassName, "eliteTier"));

            //From here we need to back up a bit due to a brfalse that points to this instruction
            var fixBranch = c.DefineLabel();
            c.GotoPrev(i => i.MatchBrfalse(out var oldLabel));
            c.Next.OpCode = OpCodes.Brfalse;
            c.Next.Operand = fixBranch;

            c.GotoNext(i => i.MatchLdarg(0),
                       i => i.MatchLdfld("RoR2.CombatDirector/" + HelperClassName, "eliteTier"),
                       i => i.MatchLdfld("RoR2.CombatDirector/EliteTierDef", "healthBoostCoefficient"));

            c.MarkLabel(fixBranch);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, _helperClass.GetField("<>4__this"));
            c.Emit(OpCodes.Ldloca_S, (byte) 0);
            c.Emit(OpCodes.Ldloca_S, (byte) 1);
            c.EmitDelegate<GetCoeffsDel>(GetCoeffs);
            var skip2 = c.DefineLabel();
            c.Emit(OpCodes.Br, skip2);
            c.Index += 8;
            c.MarkLabel(skip2);

            //Finally, just before it launches the spawnEffect, we'll give the card's creator a chance to apply changes to the character
            c.GotoNext(i => i.MatchLdarg(0),
                       i => i.MatchLdfld("RoR2.CombatDirector/" + HelperClassName, "<>4__this"),
                       i => i.MatchLdfld("RoR2.CombatDirector", "spawnEffectPrefab"));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, _helperClass.GetField("<>4__this"));
            c.Emit(OpCodes.Ldloc_2);
            c.EmitDelegate<Action<CombatDirector, CharacterMaster>>(RunOnSpawn);
        }

        private static EliteIndex GetNextElite(CombatDirector self, ref int scaledCost, ref int finalCost)
        {
            ChosenAffix.TryGetValue(self, out var affix);
            if (affix != null)
            {
                var multiplier = affix.costMultiplier;
                if (EliteOnlyArtifactEnabled)
                {
                    multiplier = Mathf.LerpUnclamped(1.0f, multiplier, affix.eliteOnlyScaling);
                }

                scaledCost = (int) (multiplier*finalCost);
                if (scaledCost < self.monsterCredit || EliteOnlyArtifactEnabled)
                {
                    finalCost = scaledCost;
                    return affix.eliteType;
                }

                ChosenAffix[self] = null;
            }
            else
            {
                scaledCost = finalCost;
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

                if (EliteOnlyArtifactEnabled)
                {
                    hpCoeff = Mathf.LerpUnclamped(1.0f, hpCoeff, affix.eliteOnlyScaling);
                    dmgCoeff = Mathf.LerpUnclamped(1.0f, dmgCoeff, affix.eliteOnlyScaling);
                }
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
            float maxCost = 1.0f;

            foreach (var card in Cards)
            {
                if (card.isAvailable())
                {
                    maxCost = Mathf.Max(maxCost, card.costMultiplier);
                }
            }

            return maxCost;
        }
    }
}
