using EntityStates;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class PreparePounceState : BaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();

            //Begin the windup animation
        }


        public override void FixedUpdate()
        {
            base.FixedUpdate();

            //When windup animation is done, go to PouncingState
        }
    }
}