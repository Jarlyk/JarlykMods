using System;
using System.Collections.Generic;
using System.Text;

namespace JarlykMods.Hailstorm.Cataclysm.BossPhases
{
    public sealed class RewardPhase : PhaseBase
    {
        public override BossPhase FixedUpdate()
        {
            return BossPhase.Reward;
        }

        public RewardPhase(CataclysmBossFightController controller) : base(controller)
        {
        }
    }
}
