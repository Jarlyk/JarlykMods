using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;
using UnityEngine;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class SurprisePounceState : BaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();
        }


        public override void FixedUpdate()
        {
            base.FixedUpdate();

            //Immediately go to pouncing state when surprising
            //This intermediate state is just to give a frame for some things to update first

            outer.SetNextState(Instantiate(typeof(PouncingState)));
        }
    }
}
