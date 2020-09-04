using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using UnityEngine;

namespace CombatDirectorTweaks
{
    [BepInPlugin(PluginGuid, "CombatDirectorTweaks", "0.0.1")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    public sealed class CombatDirectorTweaksPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.combatdirectortweaks";

        public CombatDirectorTweaksPlugin()
        {
            TweaksConfig.Init(Config);

            IL.RoR2.CombatDirector.Simulate += CombatDirector_Simulate;
        }

        private void CombatDirector_Simulate(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(m => m.MatchLdcI4(40),
                       m => m.MatchBlt(out _));

            var max = TweaksConfig.MaxEnemyCount.Value;
            c.Next.OpCode = OpCodes.Ldc_I4;
            c.Next.Operand = max;
        }

        public void Awake()
        {
        }
    }
}
