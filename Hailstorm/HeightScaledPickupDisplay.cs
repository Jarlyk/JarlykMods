using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace JarlykMods.Hailstorm
{
    public class HeightScaledPickupDisplay : MonoBehaviour
    {
        [Tooltip("The vertical motion of the display model.")]
        public Wave verticalWave;
        public bool dontInstantiatePickupModel;
        [Tooltip("The speed in degrees/second at which the display model rotates about the y axis.")]
        public float spinSpeed = 75f;
        public GameObject tier1ParticleEffect;
        public GameObject tier2ParticleEffect;
        public GameObject tier3ParticleEffect;
        public GameObject equipmentParticleEffect;
        public GameObject lunarParticleEffect;
        public GameObject bossParticleEffect;
        [Tooltip("The particle system to tint.")]
        public ParticleSystem[] coloredParticleSystems;
        private PickupIndex pickupIndex = PickupIndex.none;
        private bool hidden;
        public Highlight highlight;
        private GameObject modelObject;
        private GameObject modelPrefab;
        private float modelScale;
        private float localTime;

        public void CopyFrom(PickupDisplay display)
        {
            verticalWave = display.verticalWave;
            dontInstantiatePickupModel = display.dontInstantiatePickupModel;
            tier1ParticleEffect = display.tier1ParticleEffect;
            tier2ParticleEffect = display.tier2ParticleEffect;
            tier3ParticleEffect = display.tier3ParticleEffect;
            equipmentParticleEffect = display.equipmentParticleEffect;
            lunarParticleEffect = display.lunarParticleEffect;
            bossParticleEffect = display.bossParticleEffect;
            coloredParticleSystems = display.coloredParticleSystems;
            highlight = display.highlight;
        }
        public void SetPickupIndex(PickupIndex newPickupIndex, bool newHidden = false)
        {
            if (pickupIndex == newPickupIndex && hidden == newHidden)
                return;
            pickupIndex = newPickupIndex;
            hidden = newHidden;
            RebuildModel();
        }

        private void DestroyModel()
        {
            if (!(bool)modelObject)
                return;
            Destroy(modelObject);
            modelObject = null;
        }

        private void RebuildModel()
        {
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            GameObject gameObject = null;
            if (pickupDef != null)
                gameObject = hidden ? PickupCatalog.GetHiddenPickupDisplayPrefab() : pickupDef.displayPrefab;
            if (modelPrefab == gameObject)
                return;
            DestroyModel();
            modelPrefab = gameObject;
            modelScale = transform.lossyScale.x;
            if (!dontInstantiatePickupModel && modelPrefab != null)
            {
                modelObject = Instantiate<GameObject>(modelPrefab);
                var renderers = modelObject.GetComponentsInChildren<Renderer>();
                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                foreach (var renderer in renderers)
                {
                    modelObject.transform.rotation = Quaternion.identity;
                    bounds.Encapsulate(renderer.bounds);
                    if (highlight && !highlight.targetRenderer)
                        highlight.targetRenderer = renderer;
                }

                modelScale *= 1.0f/bounds.size.y;
                modelObject.transform.parent = transform;
                modelObject.transform.localPosition = localModelPivotPosition;
                modelObject.transform.localRotation = Quaternion.identity;
                modelObject.transform.localScale = new Vector3(modelScale, modelScale, modelScale);
            }
            if ((bool)tier1ParticleEffect)
                tier1ParticleEffect.SetActive(false);
            if ((bool)tier2ParticleEffect)
                tier2ParticleEffect.SetActive(false);
            if ((bool)tier3ParticleEffect)
                tier3ParticleEffect.SetActive(false);
            if ((bool)equipmentParticleEffect)
                equipmentParticleEffect.SetActive(false);
            if ((bool)lunarParticleEffect)
                lunarParticleEffect.SetActive(false);
            ItemIndex itemIndex = pickupDef?.itemIndex ?? ItemIndex.None;
            EquipmentIndex equipmentIndex = pickupDef?.equipmentIndex ?? EquipmentIndex.None;
            if (itemIndex != ItemIndex.None)
            {
                switch (ItemCatalog.GetItemDef(itemIndex).tier)
                {
                    case ItemTier.Tier1:
                        if ((bool)tier1ParticleEffect)
                        {
                            tier1ParticleEffect.SetActive(true);
                        }
                        break;
                    case ItemTier.Tier2:
                        if ((bool)tier2ParticleEffect)
                        {
                            tier2ParticleEffect.SetActive(true);
                        }
                        break;
                    case ItemTier.Tier3:
                        if ((bool)tier3ParticleEffect)
                        {
                            tier3ParticleEffect.SetActive(true);
                        }
                        break;
                }
            }
            else if (equipmentIndex != EquipmentIndex.None && equipmentParticleEffect)
                equipmentParticleEffect.SetActive(true);
            if (bossParticleEffect)
                bossParticleEffect.SetActive(pickupDef != null && pickupDef.isBoss);
            if (lunarParticleEffect)
                lunarParticleEffect.SetActive(pickupDef != null && pickupDef.isLunar);
            if (highlight)
            {
                highlight.isOn = true;
                highlight.pickupIndex = pickupIndex;
            }
            foreach (ParticleSystem coloredParticleSystem in coloredParticleSystems)
            {
                coloredParticleSystem.gameObject.SetActive(modelPrefab != null);
                var main = coloredParticleSystem.main;
                main.startColor = pickupDef?.baseColor ?? PickupCatalog.invalidPickupColor;
            }
        }

        private Vector3 localModelPivotPosition => Vector3.up * verticalWave.Evaluate(localTime);

        private void Start() => localTime = 0.0f;

        private void Update()
        {
            localTime += Time.deltaTime;
            if (!(bool)modelObject)
                return;
            Transform transform = modelObject.transform;
            Vector3 localEulerAngles = transform.localEulerAngles;
            localEulerAngles.y = spinSpeed * localTime;
            transform.localEulerAngles = localEulerAngles;
            transform.localPosition = localModelPivotPosition;
        }
    }
}
