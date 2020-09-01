using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class PounceRecoverState : BaseState
    {
        public static float recoverDuration = 1.2f;

        public override void OnEnter()
        {
            base.OnEnter();
            PlayAnimation("FullBody, Override", "LeapExit", "Leap.playbackRate", recoverDuration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge >= recoverDuration)
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
