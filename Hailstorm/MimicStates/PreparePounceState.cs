using EntityStates;
using UnityEngine;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class PreparePounceState : BaseState
    {
        public float baseDuration = 1.2f;

        private Animator _modelAnimator;
        private float _duration;

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("Entering Mimic|PreparePounceState");

            //Begin the windup animation
            _modelAnimator = GetModelAnimator();
            _duration = baseDuration/attackSpeedStat;
            PlayAnimation("FullBody, Override", "LeapStart", "Leap.playbackRate", _duration);
            AkSoundEngine.PostEvent("Play_Mimic_ChargePounce", gameObject);
        }


        public override void FixedUpdate()
        {
            base.FixedUpdate();

            //When windup animation is done, go to PouncingState
            if (fixedAge >= _duration)
                outer.SetNextState(Instantiate(typeof(PouncingState)));
        }

        public override void OnExit()
        {
            Debug.Log("Exiting Mimic|PreparePounceState");
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }
}