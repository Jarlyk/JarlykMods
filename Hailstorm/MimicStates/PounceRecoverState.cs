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
            PlayAnimation("FullBody, Override", "LeapExit", "Leap.playbackRate", recoverDuration);
            base.OnEnter();
        }

        public override void Update()
        {
            base.Update();
            if (age >= recoverDuration)
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
