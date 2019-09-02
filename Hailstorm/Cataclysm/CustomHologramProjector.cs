using System.Collections.ObjectModel;
using RoR2;
using UnityEngine;

namespace JarlykMods.Hailstorm.Cataclysm
{
    /// <summary>
    /// This is a copy of a RoR2 behaviour in order to work around an issue
    /// where the IHologramContentProvider interface was internal.  This prevented
    /// creating completely new interactables, so we just duplicate this functionality with
    /// our own copy of the class.
    /// </summary>
    public class CustomHologramProjector : MonoBehaviour
    {
        private void Awake()
        {
            this.contentProvider = base.GetComponent<ICustomHologramContentProvider>();
        }

        private Transform FindViewer(Vector3 position)
        {
            if (this.viewerReselectTimer > 0f)
            {
                return this.cachedViewer;
            }
            this.viewerReselectTimer = this.viewerReselectInterval;
            this.cachedViewer = null;
            float num = float.PositiveInfinity;
            ReadOnlyCollection<PlayerCharacterMasterController> instances = PlayerCharacterMasterController.instances;
            int i = 0;
            int count = instances.Count;
            while (i < count)
            {
                GameObject bodyObject = instances[i].master.GetBodyObject();
                if (bodyObject)
                {
                    float sqrMagnitude = (bodyObject.transform.position - position).sqrMagnitude;
                    if (sqrMagnitude < num)
                    {
                        num = sqrMagnitude;
                        this.cachedViewer = bodyObject.transform;
                    }
                }
                i++;
            }
            return this.cachedViewer;
        }

        private void Update()
        {
            this.viewerReselectTimer -= Time.deltaTime;
            Vector3 vector = this.hologramPivot ? this.hologramPivot.position : base.transform.position;
            this.viewer = this.FindViewer(vector);
            Vector3 b = this.viewer ? this.viewer.position : base.transform.position;
            bool flag = false;
            Vector3 forward = Vector3.zero;
            if (this.viewer)
            {
                forward = vector - b;
                if (forward.sqrMagnitude <= this.displayDistance * this.displayDistance)
                {
                    flag = true;
                }
            }
            if (flag)
            {
                flag = this.contentProvider.ShouldDisplayHologram(this.viewer.gameObject);
            }
            if (flag)
            {
                if (!this.hologramContentInstance)
                {
                    this.BuildHologram();
                }
                if (this.hologramContentInstance && this.contentProvider != null)
                {
                    this.contentProvider.UpdateHologramContent(this.hologramContentInstance);
                    if (!this.disableHologramRotation)
                    {
                        this.hologramContentInstance.transform.rotation = Util.SmoothDampQuaternion(this.hologramContentInstance.transform.rotation, Util.QuaternionSafeLookRotation(forward), ref this.transformDampVelocity, 0.2f);
                        return;
                    }
                }
            }
            else
            {
                this.DestroyHologram();
            }
        }

        // Token: 0x06001ACB RID: 6859 RVA: 0x00077344 File Offset: 0x00075544
        private void BuildHologram()
        {
            this.DestroyHologram();
            if (this.contentProvider != null)
            {
                GameObject hologramContentPrefab = this.contentProvider.GetHologramContentPrefab();
                if (hologramContentPrefab)
                {
                    this.hologramContentInstance = UnityEngine.Object.Instantiate<GameObject>(hologramContentPrefab);
                    this.hologramContentInstance.transform.parent = (this.hologramPivot ? this.hologramPivot : base.transform);
                    this.hologramContentInstance.transform.localPosition = Vector3.zero;
                    this.hologramContentInstance.transform.localRotation = Quaternion.identity;
                    this.hologramContentInstance.transform.localScale = Vector3.one;
                    if (this.viewer && !this.disableHologramRotation)
                    {
                        Vector3 a = this.hologramPivot ? this.hologramPivot.position : base.transform.position;
                        Vector3 position = this.viewer.position;
                        Vector3 forward = a - this.viewer.position;
                        this.hologramContentInstance.transform.rotation = Util.QuaternionSafeLookRotation(forward);
                    }
                    this.contentProvider.UpdateHologramContent(this.hologramContentInstance);
                }
            }
        }

        private void DestroyHologram()
        {
            if (this.hologramContentInstance)
            {
                UnityEngine.Object.Destroy(this.hologramContentInstance);
            }
            this.hologramContentInstance = null;
        }

        [Tooltip("The range in meters at which the hologram begins to display.")]
        public float displayDistance = 15f;

        [Tooltip("The position at which to display the hologram.")]
        public Transform hologramPivot;

        [Tooltip("Whether or not the hologram will pivot to the player")]
        public bool disableHologramRotation;

        private float transformDampVelocity;

        private ICustomHologramContentProvider contentProvider;

        private float viewerReselectTimer;

        private float viewerReselectInterval = 0.25f;

        private Transform cachedViewer;

        private Transform viewer;

        private GameObject hologramContentInstance;
    }
}
