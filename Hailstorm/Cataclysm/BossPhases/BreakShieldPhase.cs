using System;
using System.Collections.Generic;
using System.Text;

namespace JarlykMods.Hailstorm.Cataclysm.BossPhases
{
    public sealed class BreakShieldPhase : PhaseBase
    {
        public BreakShieldPhase(CataclysmBossFightController controller) : base(controller)
        {
        }

        public override BossPhase FixedUpdate()
        {
            Controller.AutoSpawnGravBombs(20, 8, 8);
            return BossPhase.BreakShield;
        }
    }
}
