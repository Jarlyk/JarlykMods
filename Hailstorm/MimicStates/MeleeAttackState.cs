using EntityStates;
using RoR2;
using UnityEngine;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class MeleeAttackState : BaseState
    {
        public static float baseDuration = 1.5f;
        public static float damage = 15f;
        public static float forceMagnitude = 100f;

        private OverlapAttack _attack;
        private Animator _modelAnimator;
        private float _duration;

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("Entering Mimic|MeleeAttackState");
            Debug.Log($"body.radius = {characterBody.radius}");

            _duration = baseDuration/attackSpeedStat;
            _modelAnimator = GetModelAnimator();
            _attack = new OverlapAttack();
            _attack.attacker = gameObject;
            _attack.inflictor = gameObject;
            _attack.teamIndex = TeamComponent.GetObjectTeam(gameObject);
            _attack.damage = damage;
            _attack.hitBoxGroup = GetModelTransform().GetComponent<HitBoxGroup>();
            
            //TODO: Play melee start animation
            AkSoundEngine.PostEvent(SoundEvents.PlayChomp1, gameObject);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isAuthority)
            {
                _attack.forceVector = Vector3.zero;
                if (characterDirection)
                    _attack.forceVector = characterDirection.forward*forceMagnitude;

                //TODO: tie to animation
                //if (_modelAnimator && _modelAnimator.GetFloat("Melee1.hitBoxActive") > 0.5)
                if (fixedAge > 0.5*_duration)
                {
                    _attack.Fire();
                }

                //TODO: Play melee strike effect
            }

            if (fixedAge < _duration || !isAuthority)
                return;

            outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            Debug.Log("Exiting Mimic|MeleeAttackState");
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }
}