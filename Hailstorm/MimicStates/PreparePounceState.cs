using EntityStates;
using UnityEngine;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class PreparePounceState : BaseState
    {
        public float baseDuration = 1.2f;
        private float _duration;
        private EmPowerAnimator _emPowerAnimator;

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("Entering Mimic|PreparePounceState");

            _emPowerAnimator = modelLocator.modelTransform.GetComponent<EmPowerAnimator>();
            _emPowerAnimator.SetTarget(20);

            //Begin the windup animation
            _duration = baseDuration/attackSpeedStat;
            PlayAnimation("FullBody, Override", "LeapStart", "Leap.playbackRate", _duration);
            AkSoundEngine.PostEvent("Play_Mimic_ChargePounce", gameObject);
        }


        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (fixedAge >= 0.5f*_duration)
                _emPowerAnimator.SetTarget(120);

            //When windup animation is done, go to PouncingState
            if (isAuthority && fixedAge >= _duration)
                outer.SetNextState(Instantiate(typeof(PouncingState)));
        }

        public override void OnExit()
        {
            Debug.Log("Exiting Mimic|PreparePounceState");
            _emPowerAnimator.SetTarget(100);
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }
}