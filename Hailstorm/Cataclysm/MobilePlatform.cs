using RoR2;
using System.Collections.Generic;
using System.Linq;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm.Cataclysm
{
    public sealed class MobilePlatform : MonoBehaviour
    {
        private List<GameObject> _occupants;
        private Vector3 _lastPosition;
        private Vector3 _lastDeltaPosition;
        private Matrix4x4 _lastWorldToLocal;
        private Collider _triggerCollider;

        private void OnTriggerEnter(Collider other)
        {
            var body = other.GetComponent<CharacterBody>();
            if (body != null && body.hasAuthority && body.GetComponent<Deployable>() == null)
            {
                _occupants.Add(other.gameObject);
                Debug.Log($"Platform add body: {other.name}");
            }

            //var projectile = other.GetComponent<ProjectileController>();
            //if (projectile != null && projectile.hasAuthority)
            //{
            //    _occupants.Add(other.gameObject);
            //    Debug.Log($"Platform add projectile: {other.name}");
            //}
        }

        private void OnTriggerExit(Collider other)
        {
            var body = other.GetComponent<CharacterBody>();
            if (body != null && body.hasAuthority)
            {
                //When body leaves the surface, it inherits some momentum from the platform
                var motor = body.characterMotor;
                if (motor != null)
                {
                    var force = (_lastDeltaPosition/Time.fixedDeltaTime)*motor.mass;
                    motor.ApplyForce(force, true, true);
                }
            }

            _occupants.Remove(other.gameObject);
        }

        private void Awake()
        {
            _occupants = new List<GameObject>();
            _triggerCollider = GetComponents<Collider>().FirstOrDefault(c => c.isTrigger);
            if (_triggerCollider == null)
            {
                Debug.LogWarning("MobilePlatform behaviour attached to an object with no trigger collider; disabling");
                enabled = false;
            }
        }

        private void Start()
        {
            _lastPosition = transform.position;
            _lastWorldToLocal = transform.worldToLocalMatrix;
        }

        private void FixedUpdate()
        {
            if (!isActiveAndEnabled)
                return;

            if (_occupants.Count > 0)
            {
                var dp = transform.position - _lastPosition;
                _lastDeltaPosition = dp;
                foreach (var gameObj in _occupants)
                {
                    bool moved = false;
                    var body = gameObj.GetComponent<CharacterBody>();
                    if (body != null)
                    {
                        var motor = body.characterMotor;
                        if (motor != null)
                        {
                            var bodyPos = motor.Motor.TransientPosition;
                            var locPos = _lastWorldToLocal.MultiplyPoint(bodyPos);
                            var newPos = transform.TransformPoint(locPos);
                            motor.Motor.SetPosition(newPos, true);
                            moved = true;
                        }
                    }

                    if (!moved)
                    {
                        var rigidBody = gameObj.GetComponent<Rigidbody>();
                        if (rigidBody != null)
                        {
                            var bodyPos = rigidBody.position;
                            var locPos = _lastWorldToLocal.MultiplyPoint(bodyPos);
                            var newPos = transform.TransformPoint(locPos);
                            rigidBody.MovePosition(newPos);
                        }
                        else
                        {
                            //No rigid body or motor controller, so just change its transform directly to follow
                            //This applies to Engi turrets in particular
                            var bodyPos = gameObj.transform.position;
                            var locPos = _lastWorldToLocal.MultiplyPoint(bodyPos);
                            var newPos = transform.TransformPoint(locPos);
                            gameObj.transform.position = newPos;
                        }
                    }
                }
            }

            _lastPosition = transform.position;
            _lastWorldToLocal = transform.worldToLocalMatrix;
        }
    }
}
