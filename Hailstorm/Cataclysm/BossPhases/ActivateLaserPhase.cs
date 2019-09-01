using System;
using System.Collections.Generic;
using System.Text;

namespace JarlykMods.Hailstorm.Cataclysm.BossPhases
{
    public sealed class ActivateLaserPhase : PhaseBase
    {
        public override BossPhase FixedUpdate()
        {
            return BossPhase.ActivateLaser;
        }

        public ActivateLaserPhase(CataclysmBossFightController controller) : base(controller)
        {
        }
    }
}
