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
            //Split the item pickup from the mimic model
            var pickup = _modelTransform.GetComponentInChildren<GenericPickupController>();
            if (pickup)
            {
                if (isAuthority)
                {
                    PickupDropletController.CreatePickupDroplet(pickup.pickupIndex, pickup.transform.position,
                                                                Vector3.zero);
                }

                pickup.gameObject.SetActive(false);
            }

            base.OnExit();
        }
    }
}
