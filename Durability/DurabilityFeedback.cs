using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using RoR2.UI;
using UnityEngine.UI;
using Console = RoR2.Console;

namespace JarlykMods.Durability
{
    public sealed class DurabilityFeedback : MonoBehaviour
    {
        private GameObject _bar;
        private GameObject _barImage;
        private Vector2 _origAnchorMin;
        private Vector2 _origAnchorMax;


        public float percentDurability;

        public bool showBar;

        private void Start()
        {
            StartCoroutine(DelayedAdd());
        }

        private IEnumerator DelayedAdd()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            var equipIcon = GetComponent<EquipmentIcon>();
            if (equipIcon)
            {
                if (!equipIcon.displayRoot)
                {
                    Debug.LogError("Equipment icon displayRoot not yet set in DurabilityFeedback.DelayedAdd");
                    enabled = false;
                    yield break;
                }

                _bar = Instantiate(DurabilityAssets.DurabilityBarPrefab, equipIcon.displayRoot.transform);
                _barImage = _bar.transform.GetChild(1).gameObject;
                var rectTrans = (RectTransform) _barImage.transform;
                _origAnchorMin = rectTrans.anchorMin;
                _origAnchorMax = rectTrans.anchorMax;
                rectTrans.ForceUpdateRectTransforms();
            }
            else
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (_bar == null)
                return;

            _bar.SetActive(showBar);
            if (showBar)
            {
                var origWidth = _origAnchorMax.x - _origAnchorMin.x;
                var rectTrans =_barImage.transform as RectTransform;
                if (rectTrans != null)
                {
                    var newWidth = percentDurability*origWidth/100.0f;
                    var newX = _origAnchorMin.x + newWidth;
                    rectTrans.anchorMax = new Vector2(newX, _origAnchorMax.y);
                    rectTrans.ForceUpdateRectTransforms();
                }
            }
        }
    }
}