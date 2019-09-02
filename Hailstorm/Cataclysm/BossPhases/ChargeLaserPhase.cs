using System;
using System.Collections.Generic;
using System.Text;

namespace JarlykMods.Hailstorm.Cataclysm.BossPhases
{
    public sealed class ChargeLaserPhase : PhaseBase
    {
        public ChargeLaserPhase(CataclysmBossFightController controller) : base(controller)
        {
        }

        public override BossPhase FixedUpdate()
        {
            Controller.AutoSpawnGravBombs(20, 8, 8);
            return BossPhase.ChargeLaser;
        }
    }
}
