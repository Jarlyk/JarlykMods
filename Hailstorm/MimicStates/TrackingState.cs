using EntityStates;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class TrackingState : BaseState
    {
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            //Track nearest player by adjusting orientation
            
            //If player is close enough and sufficient time has elapsed, transition to NearbyAttackState

            //When sufficient time has elapsed while unable to attack nearby, go to PreparePounceState
        }
    }
}