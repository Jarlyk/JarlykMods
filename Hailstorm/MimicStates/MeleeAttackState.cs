using System.Collections;
using EntityStates;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class MeleeAttackState : BaseSkillState
    {
        public static float baseDuration = 1.2f;
        public static float forceMagnitude = 1000f;

        private OverlapAttack _attack;
        private float _duration;

        public override void OnEnter()
        {
            base.OnEnter();

            _duration = baseDuration/attackSpeedStat;

            if (isAuthority)
            {
                _attack = new OverlapAttack();
                _attack.attacker = gameObject;
                _attack.inflictor = gameObject;
                _attack.teamIndex = TeamComponent.GetObjectTeam(gameObject);
                _attack.damage = damageStat;
                _attack.hitBoxGroup = GetModelTransform().GetComponent<HitBoxGroup>();
                _attack.hitEffectPrefab = HailstormAssets.MimicBiteEffect;
            }
            
            AkSoundEngine.PostEvent(SoundEvents.PlayChomp1, gameObject);
            PlayAnimation("FullBody, Override", "Bite", "Leap.playbackRate", _duration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isAuthority && fixedAge > 0.5*_duration)
                _attack.Fire();

            if (isAuthority && fixedAge >= _duration)
                outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }
}