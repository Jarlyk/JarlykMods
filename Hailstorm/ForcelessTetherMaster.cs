using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    public sealed class ForcelessTetherMaster : MonoBehaviour
    {
        private List<GameObject> _tetheredObjects = new List<GameObject>();
        private TeamComponent _teamComponent;

        public GameObject TetherPrefab;

        public float Radius;

        public Func<GameObject, bool> CanTether = x => x.GetComponent<CharacterBody>() != null;

        public IReadOnlyList<GameObject> GetTetheredObjects() => _tetheredObjects;

        private void Awake()
        {
            _teamComponent = GetComponent<TeamComponent>();
        }

        private void AddToList(GameObject affectedObject)
        {
            if (TetherPrefab == null)
            {
                Debug.Log("No tether prefab active for ForcelessTetherMaster; disabling");
                enabled = false;
                return;
            }

            var tetherObj = Instantiate(TetherPrefab, affectedObject.transform);
            tetherObj.SetActive(true);

            var component = tetherObj.GetComponent<TetherEffect>();
            component.tetherEndTransform = transform;
            component.tetherMaxDistance = Radius + 1.5f;
            _tetheredObjects.Add(affectedObject);
        }

        private void FixedUpdate()
        {
            var colliders = Physics.OverlapSphere(transform.position, Radius, LayerIndex.defaultLayer.mask);
            var newTethered = new List<GameObject>();
            foreach (var collider in colliders)
            {
                bool canTether = collider.gameObject != gameObject && CanTether(collider.gameObject);
                if (canTether && _teamComponent)
                {
                    var teamComponent = collider.GetComponent<TeamComponent>();
                    canTether = teamComponent != null && teamComponent.teamIndex == _teamComponent.teamIndex;
                }

                if (canTether)
                {
                    newTethered.Add(collider.gameObject);
                }
            }

            //Any new objects should have a tether constructed
            foreach (var tethered in newTethered)
            {
                if (!_tetheredObjects.Contains(tethered))
                    AddToList(tethered);
            }

            //Replace the list so it only shows what's currently in-range and thus tethered
            _tetheredObjects = newTethered;
        }
    }
}
