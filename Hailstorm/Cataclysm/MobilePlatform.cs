using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;

namespace JarlykMods.Hailstorm.Cataclysm
{
    public sealed class MobilePlatform : MonoBehaviour
    {
        private List<CharacterBody> _bodies;
        private Vector3 _lastPosition;

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
            }
        }

        private void Awake()
        {
            _bodies = new List<CharacterBody>();
        }

        private void Start()
        {
            _lastPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (_bodies.Count > 0)
            {
                var dp = transform.position - _lastPosition;
                foreach (var body in _bodies)
                {
                    //TODO: Account for body offset relative to center and displacement due to rotation
                    body.characterMotor.Motor.SetPosition(body.characterMotor.Motor.TransientPosition + dp, true);
                }
            }

            _lastPosition = transform.position;
        }
    }
}
