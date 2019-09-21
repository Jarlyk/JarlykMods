using System;
using System.Collections.Generic;
using System.Text;

namespace JarlykMods.Hailstorm.Cataclysm.BossPhases
{
    public sealed class RunLaserPhase : PhaseBase
    {
        public RunLaserPhase(CataclysmBossFightController controller) : base(controller)
        {
        }

        public override void OnEnter()
        {
            
        }

        public override BossPhase FixedUpdate()
        {
            return BossPhase.RunLaser;
        }
    }
}
