using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace JarlykMods.Hailstorm.Cataclysm
{
    public sealed class MobilePlatform : MonoBehaviour
    {
        private List<CharacterBody> _bodies;
        private Vector3 _lastPosition;
        private Vector3 _lastDeltaPosition;
        private Matrix4x4 _lastWorldToLocal;

        private void OnTriggerEnter(Collider other)
        {
            var body = other.GetComponent<CharacterBody>();
            if (body != null && body.hasAuthority)
            {
                _bodies.Add(body);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var body = other.GetComponent<CharacterBody>();
            if (body != null && body.hasAuthority)
            {
                _bodies.Remove(body);

                //When body leaves the surface, it inherits some momentum from the platform
                var motor = body.characterMotor;
                if (motor != null)
                {
                    var force = (_lastDeltaPosition/Time.fixedDeltaTime)*motor.mass;
                    motor.ApplyForce(force, true, true);
                }
            }
        }

        private void Awake()
        {
            _bodies = new List<CharacterBody>();
        }

        private void Start()
        {
            _lastPosition = transform.position;
            _lastWorldToLocal = transform.worldToLocalMatrix;
        }

        private void FixedUpdate()
        {
            if (_bodies.Count > 0)
            {
                var dp = transform.position - _lastPosition;
                _lastDeltaPosition = dp;
                foreach (var body in _bodies)
                {
                    var bodyPos = body.characterMotor.Motor.TransientPosition;
                    var locPos = _lastWorldToLocal.MultiplyPoint(bodyPos);
                    var newPos = transform.TransformPoint(locPos);
                    body.characterMotor.Motor.SetPosition(newPos, true);
                }
            }

            _lastPosition = transform.position;
            _lastWorldToLocal = transform.worldToLocalMatrix;
        }
    }
}
