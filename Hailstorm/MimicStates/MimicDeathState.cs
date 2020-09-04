using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;
using RoR2;
using UnityEngine;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class MimicDeathState : GenericCharacterDeath
    {
        private Transform _modelTransform;

        public override void OnEnter()
        {
            base.OnEnter();
            PlayAnimation("FullBody, Override", "BufferEmpty");

            _modelTransform = modelLocator.modelTransform;
            var emPowerAnimator = _modelTransform.GetComponent<EmPowerAnimator>();
            emPowerAnimator.SetTarget(0.1f);
        }

        public override void OnExit()
        {
            if (isAuthority)
            {
                //Split the item pickup from the mimic model
                var pickup = _modelTransform.GetComponentInChildren<GenericPickupController>();
                if (pickup)
                    pickup.gameObject.transform.SetParent(null, true);
            }
            base.OnExit();
        }
    }
}
