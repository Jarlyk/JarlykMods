using System.Collections;
using EntityStates;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class MeleeAttackState : BaseState
    {
        public static float baseDuration = 1.2f;
        public static float forceMagnitude = 1000f;

        private OverlapAttack _attack;
        private Animator _modelAnimator;
        private float _duration;
        private GameObject _muzzleBone;

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("Entering Mimic|MeleeAttackState");

            _duration = baseDuration/attackSpeedStat;
            _modelAnimator = GetModelAnimator();

            _attack = new OverlapAttack();
            _attack.attacker = gameObject;
            _attack.inflictor = gameObject;
            _attack.teamIndex = TeamComponent.GetObjectTeam(gameObject);
            _attack.damage = damageStat;
            _attack.hitBoxGroup = GetModelTransform().GetComponent<HitBoxGroup>();
            _attack.hitEffectPrefab = HailstormAssets.MimicBiteEffect;
            
            AkSoundEngine.PostEvent(SoundEvents.PlayChomp1, gameObject);
            PlayAnimation("FullBody, Override", "Bite", "Leap.playbackRate", _duration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isAuthority)
            {
                //_attack.forceVector = Vector3.zero;
                //if (characterDirection)
                //{
                //    _attack.forceVector = characterDirection.forward;
                //    _attack.pushAwayForce = forceMagnitude;
                //}

                //TODO: tie to animation
                //if (_modelAnimator && _modelAnimator.GetFloat("Melee1.hitBoxActive") > 0.5)
                if (fixedAge > 0.5*_duration)
                {
                    _attack.Fire();
                }
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