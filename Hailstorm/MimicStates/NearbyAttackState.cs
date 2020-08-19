using EntityStates;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class NearbyAttackState : BaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();

            //Perform attack animation and/or effect
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            //When sufficient time has elapsed, apply attack damage/effects

            //When sufficient time has elapsed, transition to Tracking state
        }
    }
}