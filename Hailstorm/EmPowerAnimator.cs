using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JarlykMods.Hailstorm
{
    public sealed class EmPowerAnimator : MonoBehaviour
    {
        private AnimatedFloat _emPower;

        public Material material;

        public void SetTarget(float emPower, float accel = 20, float maxSpeed = 50)
        {
            _emPower.Setpoint = emPower;
            _emPower.Accel = accel;
            _emPower.MaxSpeed = maxSpeed;
        }

        public void Start()
        {
            _emPower = new AnimatedFloat();
            _emPower.Accel = 20;
            _emPower.MaxSpeed = 50;
            _emPower.Position = 100;
            _emPower.Setpoint = 100;
        }

        public void Update()
        {
            if (_emPower == null || !material)
                return;

            _emPower.Update(Time.deltaTime);
            material.SetFloat("_EmPower", _emPower.Position);
        }
    }
}
