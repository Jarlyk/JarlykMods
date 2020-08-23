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
        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("Entering Mimic|PouncingState");
            duration = 1.0f;
            startTime = Time.fixedTime;

            //Start the leaping animation
            PlayAnimation("FullBody, Override", "LeapLoop");

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
                var target = characterBody.GetComponent<MimicContext>().target;
                if (target != null)
                {
                    characterMotor.velocity = speed*(target.transform.position - characterBody.transform.position).normalized + new Vector3(0, 16f, 0);
                }
                else
                {
                    aimRay = GetAimRay();
                    characterMotor.velocity = speed*aimRay.direction + new Vector3(0, 16f, 0);
                }
                characterMotor.disableAirControlUntilCollision = true;
                characterMotor.Motor.ForceUnground();
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            //Once we collide with player or after duration, return to standard state
            if (characterMotor.isGrounded && (Time.fixedTime - startTime) >= duration/2)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            Debug.Log("Exiting Mimic|PouncingState");
            base.OnExit();
        }

        public static float pounceSpeed = 30.0f;

        public float duration;

        public float startTime;

        public Ray aimRay;

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }
}
