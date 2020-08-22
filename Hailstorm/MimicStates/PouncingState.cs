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
            const float baseDuration = 1.0f;
            duration = baseDuration / attackSpeedStat;
            startTime = Time.fixedTime;

            //PlayCrossfade("Gesture", "FireSpit", "FireSpit.playbackRate", duration, 0.1f);
            aimRay = base.GetAimRay();
            float speed = pounceSpeed;
            Ray ray = this.aimRay;
            ray.origin = this.aimRay.GetPoint(6f);
            RaycastHit raycastHit;
            if (Util.CharacterRaycast(base.gameObject, ray, out raycastHit, float.PositiveInfinity, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
            {
                var v = speed;
                Vector3 vCollision = raycastHit.point - this.aimRay.origin;
                Vector2 vxz = new Vector2(vCollision.x, vCollision.z);
                float groundDist = vxz.magnitude;
                float y = Trajectory.CalculateInitialYSpeed(groundDist/v, vCollision.y);
                Vector3 a = new Vector3(vxz.x/groundDist*v, y, vxz.y/groundDist*v);
                speed = a.magnitude;
                aimRay.direction = a / speed;
            }

            if (isAuthority)
            {
                characterMotor.velocity = speed*aimRay.direction;
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

        public static float pounceSpeed = 8.0f;

        public float duration;

        public float startTime;

        public Ray aimRay;
    }
}
