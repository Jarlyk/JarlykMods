using System;
using System.Collections.Generic;
using System.Text;

namespace JarlykMods.Hailstorm.Cataclysm.BossPhases
{
    public sealed class FinalePhase : PhaseBase
    {
        public FinalePhase(CataclysmBossFightController controller) : base(controller)
        {
        }

        public override BossPhase FixedUpdate()
        {
            return BossPhase.Finale;
        }
    }
}
