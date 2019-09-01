using System;
using System.Collections.Generic;
using System.Text;

namespace JarlykMods.Hailstorm.Cataclysm.BossPhases
{
    public sealed class VoidspawnPhase : PhaseBase
    {
        public override BossPhase FixedUpdate()
        {
            return BossPhase.Voidspawn;
        }

        public VoidspawnPhase(CataclysmBossFightController controller) : base(controller)
        {
        }
    }
}