using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class SurpriseAttackState : BaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();

            //Play surprise attack start animation
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            
            //When animation is done, transition to PouncingState
        }
    }
}
