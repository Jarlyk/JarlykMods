using System;
using System.Collections.Generic;
using System.Text;

namespace JarlykMods.Hailstorm.Cataclysm.BossPhases
{
    public sealed class TheHatchingPhase : PhaseBase
    {
        public override BossPhase FixedUpdate()
        {
            return BossPhase.TheHatching;
        }

        public TheHatchingPhase(CataclysmBossFightController controller) : base(controller)
        {
        }
    }
}
