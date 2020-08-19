using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class PouncingState : BaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();

            //Calculate and perform the pounce, as well as associated animation
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            //Once we collide with player, transition to TrackingState
        }
    }
}
