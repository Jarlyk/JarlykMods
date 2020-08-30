using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class MimicDeathState : GenericCharacterDeath
    {
        public override void OnEnter()
        {
            base.OnEnter();
            PlayAnimation("FullBody, Override", "BufferEmpty");
            
            var emPowerAnimator = modelLocator.modelTransform.GetComponent<EmPowerAnimator>();
            emPowerAnimator.SetTarget(0.1f);
        }
    }
}
