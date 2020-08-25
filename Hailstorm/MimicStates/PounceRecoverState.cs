using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class PounceRecoverState : BaseState
    {
        public override void OnEnter()
        {
            PlayAnimation("FullBody, Override", "LeapExit", "Leap.playbackRate", 0.5f);
            base.OnEnter();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.5f)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            PlayAnimation("FullBody, Override", "BufferEmpty");
            base.OnExit();
        }
    }
}
