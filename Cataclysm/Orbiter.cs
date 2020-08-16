using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JarlykMods.Hailstorm.Cataclysm
{
    public sealed class Orbiter : MonoBehaviour
    {
        private Rigidbody _rigidBody;

        public float AngularVelocity { get; set; }

        private void Awake()
        {
            _rigidBody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            var w = Time.fixedTime*AngularVelocity % 360.0f;
            var rot = Quaternion.AngleAxis(w, Vector3.up);
            _rigidBody.MoveRotation(rot);
            //_rigidBody.MovePosition(0.01f*w*Vector3.forward);
        }
    }
}
