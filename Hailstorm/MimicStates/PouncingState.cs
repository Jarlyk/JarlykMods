using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;
using R2API;
using RoR2;
using UnityEngine;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class PouncingState : BaseState
    {
        private KinematicCharacterController.KinematicCharacterMotor _kinMotor;
        private GameObject _blastEffectPrefab;
        private GameObject _blastImpactEffectPrefab;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = 1.0f;
            var leapState = ((EntityStates.Croco.BaseLeap) Instantiate(typeof(EntityStates.Croco.BaseLeap)));
            _blastImpactEffectPrefab = leapState.blastImpactEffectPrefab;
            _blastEffectPrefab = leapState.blastEffectPrefab;

            //Start the leaping animation
            PlayAnimation("FullBody, Override", "LeapLoop");
            AkSoundEngine.PostEvent("Play_Mimic_Leap", gameObject);

            //aimRay = GetAimRay();
            float speed = pounceSpeed;

            //var ray = aimRay;
            //ray.origin = this.aimRay.GetPoint(6f);
            //RaycastHit raycastHit;
            //if (Util.CharacterRaycast(base.gameObject, ray, out raycastHit, float.PositiveInfinity, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
            //{
            //    var v = speed;
            //    Vector3 vCollision = raycastHit.point - this.aimRay.origin;
            //    Vector2 vxz = new Vector2(vCollision.x, vCollision.z);
            //    float groundDist = vxz.magnitude;
            //    float y = Trajectory.CalculateInitialYSpeed(groundDist / v, vCollision.y);
            //    Vector3 a = new Vector3(vxz.x / groundDist * v, y, vxz.y / groundDist * v);
            //    speed = a.magnitude;
            //    aimRay.direction = a / speed;
            //}

            if (isAuthority)
            {
                var target = characterBody.GetComponent<MimicContext>()?.target;
                if (target != null)
                {
                    characterMotor.velocity = speed*(target.transform.position - characterBody.transform.position).normalized + new Vector3(0, 20f, 0);
                }
                else
                {
                    aimRay = GetAimRay();
                    characterMotor.velocity = speed*aimRay.direction + new Vector3(0, 16f, 0);
                }
                characterMotor.disableAirControlUntilCollision = true;
                characterMotor.Motor.ForceUnground();

                _kinMotor = GetComponent<KinematicCharacterController.KinematicCharacterMotor>();
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isAuthority)
            {
                var target = characterBody.GetComponent<MimicContext>()?.target;
                if (target != null)
                {
                    //Check how close we are to the target
                    var displacement = target.transform.position - characterBody.transform.position;
                    displacement = new Vector3(displacement.x, 0, displacement.z);
                    var distXZsqr = displacement.sqrMagnitude;
                    var v = new Vector3(characterMotor.velocity.x, 0, characterMotor.velocity.z);

                    //If we're still far enough, allow a bit of lateral vectoring
                    if (distXZsqr > 10.0f*10.0f)
                    {
                        var errLateral = v - Vector3.Project(v, displacement/Mathf.Sqrt(distXZsqr));
                        characterMotor.ApplyForce(-0.01f*(rigidbody.mass/Time.fixedDeltaTime)*errLateral);
                    }

                    //If we're close, brake and slam
                    if (distXZsqr < 4.0f*4.0f)
                    {
                        characterMotor.ApplyForce(-0.001f*(rigidbody.mass/Time.fixedDeltaTime)*new Vector3(v.x, 0.2f, v.z));
                    }
                }

                if (_kinMotor.LastMovementIterationFoundAnyGround)
                {
                    Vector3 footPosition = characterBody.footPosition;
                    EffectManager.SpawnEffect(_blastEffectPrefab, new EffectData
                    {
                        origin = footPosition,
                        scale = 7.0f
                    }, true);

                    var blastAttack = new BlastAttack
                    {
                        attacker = gameObject,
                        attackerFiltering = AttackerFiltering.NeverHit,
                        baseDamage = 1.9f*damageStat,
                        baseForce = 2000,
                        bonusForce = new Vector3(0, 500, 0),
                        crit = false,
                        damageColorIndex = DamageColorIndex.Default,
                        damageType = DamageType.Generic,
                        falloffModel = BlastAttack.FalloffModel.None,
                        impactEffect =  EffectCatalog.FindEffectIndexFromPrefab(_blastImpactEffectPrefab),
                        position = characterBody.corePosition,
                        radius = 7.0f,
                        teamIndex = teamComponent.teamIndex,
                        inflictor = gameObject,
                        losType = BlastAttack.LoSType.None,
                        procCoefficient = 1.0f
                    };
                    blastAttack.Fire();
                    AkSoundEngine.PostEvent("play_acrid_m2_explode", gameObject);

                    outer.SetNextState(Instantiate(typeof(PounceRecoverState)));
                    return;
                }
            }

            //Once we collide with player or after duration, animate recovery from failed pounce
            if (isAuthority && characterMotor.isGrounded && fixedAge >= duration)
            {
                outer.SetNextState(Instantiate(typeof(PounceRecoverState)));
            }
        }

        public override void OnExit()
        {
            AkSoundEngine.PostEvent("Stop_Mimic_Leap", gameObject);
            base.OnExit();
        }

        public static float pounceSpeed = 40.0f;

        public float duration;

        public Ray aimRay;

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }
}
