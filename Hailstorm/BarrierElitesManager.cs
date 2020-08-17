using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using EliteSpawningOverhaul;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace JarlykMods.Hailstorm
{
    public sealed class BarrierElitesManager
    {
        public const string EliteName = "Barrier";
        public const string BuffName = "Affix_Barrier";
        public const string EquipName = "ShieldBearer";

        private readonly EliteIndex _eliteIndex;
        private readonly BuffIndex _buffIndex;
        private readonly EquipmentIndex _equipIndex;
        private Material _barrierMaterial;
        private GameObject _tetherPrefab;
        private float _lastBarrierTime;

        public BarrierElitesManager()
        {
            var equipDef = new EquipmentDef
            {
                name = EquipName,
                cooldown = 10f,
                pickupModelPath = "",
                pickupIconPath = HailstormAssets.IconBarrierElite,
                nameToken = EquipName,
                pickupToken = "Shield-Bearer",
                descriptionToken = "Shield-Bearer",
                canDrop = false,
                enigmaCompatible = false
            };
            var equip = new CustomEquipment(equipDef, new ItemDisplayRule[0]);

            var buffDef = new BuffDef
            {
                name = BuffName,
                buffColor =  new Color32(162, 179, 241, 255),
                iconPath = HailstormAssets.IconBarrierElite,
                canStack = false
            };
            var buff = new CustomBuff(buffDef);

            var eliteDef = new EliteDef
            {
                name = EliteName,
                modifierToken = "ELITE_MODIFIER_BARRIER",
                color = buffDef.buffColor,
                eliteEquipmentIndex = _equipIndex
            };
            var elite = new CustomElite(eliteDef, 1);

            _eliteIndex = EliteAPI.Add(elite);
            _buffIndex = BuffAPI.Add(buff);
            _equipIndex = ItemAPI.Add(equip);
            eliteDef.eliteEquipmentIndex = _equipIndex;
            equipDef.passiveBuff = _buffIndex;
            buffDef.eliteIndex = _eliteIndex;

            //Barrier elites are a bit more uncommon than regular tier 1 elites
            //They're also a bit tankier than usual, but not more damaging
            var card = new EliteAffixCard
            {
                spawnWeight = 0.5f,
                costMultiplier = 10f,
                damageBoostCoeff = 1.0f,
                healthBoostCoeff = 10.0f,
                eliteOnlyScaling = 0.5f,
                eliteType = _eliteIndex,
                onSpawned = OnSpawned
            };

            //Register the card for spawning if ESO is enabled
            EsoLib.Cards.Add(card);
            Card = card;

            //Description of elite in UI when boss
            LanguageAPI.Add(eliteDef.modifierToken, "Barrier {0}");
            //eliteDef.modifierToken = "Barrier {0}";
        }

        public EliteAffixCard Card { get; }

        public void Awake()
        {
            _lastBarrierTime = Time.time;
            _barrierMaterial = new Material(HailstormAssets.BarrierMaterial);
            _barrierMaterial.SetTextureScale("_Cloud1Tex", new Vector2(0.2f, 0.2f));
        }

        public void Start()
        {
            //var tetherPrefab = Object.Instantiate(Resources.Load<GameObject>("Prefabs/Effects/gravspheretether"));
            ////var tetherPrefab = new GameObject("BarrierTether");
            //tetherPrefab.SetActive(false);

            //var lineRenderer = tetherPrefab.GetComponent<LineRenderer>();
            //lineRenderer.startColor = new Color32(212, 175, 55, 200);
            //lineRenderer.endColor = new Color32(212, 175, 55, 200);
            //lineRenderer.widthMultiplier = 0.8f;
            //lineRenderer.startWidth = 1;
            //lineRenderer.endWidth = 1;
            //lineRenderer.numCapVertices = 12;
            //lineRenderer.numCornerVertices = 6;
            //lineRenderer.textureMode = LineTextureMode.Tile;
            //lineRenderer.alignment = LineAlignment.View;
            //lineRenderer.material = _barrierMaterial;
            //lineRenderer.positionCount = 10;
            //lineRenderer.enabled = true;

            //var tetherEffect = tetherPrefab.GetComponent<TetherVfx>();
            //tetherEffect.enabled = true;

            //var curve = tetherPrefab.GetComponent<BezierCurveLine>();
            //curve.enabled = true;
            //curve.windFrequency = new Vector3(0.2f, 0.2f, 0.2f);
            //curve.windMagnitude = new Vector3(1, 1, 1);
            //curve.animateBezierWind = true;

            ////tetherPrefab.GetComponent<AkEvent>().enabled = false;
            //Object.DontDestroyOnLoad(tetherPrefab);
            //_tetherPrefab = tetherPrefab;
            //Debug.Log("Tether prefab constructed");
        }

        public void Update()
        {
            if (Time.time - _lastBarrierTime > 5.0f)
            {
                UpdateBarrier();
                _lastBarrierTime = Time.time;
            }

            _barrierMaterial.SetTextureOffset("_Cloud1Tex", new Vector2(Time.time%1f, 0));
        }

        private ForcelessTetherMaster BuildTetherMaster(GameObject gameObj)
        {
            var tetherMaster = gameObj.AddComponent<ForcelessTetherMaster>();
            tetherMaster.TetherPrefab = _tetherPrefab;
            tetherMaster.CanTether = TetherMasterCanTether;
            tetherMaster.Radius = 20f;
            tetherMaster.enabled = true;

            if (tetherMaster.TetherPrefab == null)
            {
                Debug.Log("Tether prefab is null on Build!");
            }

            Debug.Log("Built new tether master");
            return tetherMaster;
        }

        private bool TetherMasterCanTether(GameObject gameObj)
        {
            var body = gameObj.GetComponent<CharacterBody>();
            if (body == null)
                return false;

            return !body.HasBuff(_buffIndex);
        }

        private void UpdateBarrier()
        {
            var allBodies = CharacterBody.readOnlyInstancesList;
            var shieldBearers = allBodies.Where(b => b.HasBuff(_buffIndex));
            foreach (var shieldBearer in shieldBearers)
            {
                AkSoundEngine.PostEvent(SoundEvents.PlayBarrierWobble, shieldBearer.gameObject);
                var tetherMaster = shieldBearer.gameObject.GetComponent<ForcelessTetherMaster>();
                if (tetherMaster == null)
                    tetherMaster = BuildTetherMaster(shieldBearer.gameObject);

                if (NetworkServer.active)
                {
                    foreach (var obj in tetherMaster.GetTetheredObjects())
                    {
                        var healthComponent = obj.GetComponent<HealthComponent>();
                        if (healthComponent != null)
                            healthComponent.AddBarrier(0.1f*shieldBearer.maxHealth);
                    }
                }
            }
        }

        private static void OnSpawned(CharacterMaster master)
        {
            var bodyObj = master.GetBodyObject();
            var decor = UnityEngine.Object.Instantiate(HailstormAssets.DistortionQuad, bodyObj.transform);
            decor.transform.localScale = new Vector3(1f, 1f, 1f);
            decor.transform.localPosition = new Vector3(0, 0.2f, 0);
        }
    }
}
