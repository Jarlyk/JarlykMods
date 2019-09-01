using System;
using System.Collections.Generic;
using System.Text;

namespace JarlykMods.Hailstorm.Cataclysm.BossPhases
{
    public sealed class IntroductionPhase : PhaseBase
    {
        public IntroductionPhase(CataclysmBossFightController controller) : base(controller)
        {
        }

        public override BossPhase FixedUpdate()
        {
            return BossPhase.BreakShield;
        }
    }
}
