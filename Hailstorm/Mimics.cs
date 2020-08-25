using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx;
using EntityStates;
using JarlykMods.Hailstorm.MimicStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    public sealed class Mimics
    {
        private readonly List<ChestMimicSpawner> _chestMimics = new List<ChestMimicSpawner>();
        private Xoroshiro128Plus _rng;

        public Mimics()
        {
            IL.RoR2.SceneDirector.PopulateScene += SceneDirectorOnPopulateScene;
            On.RoR2.DeathRewards.OnKilledServer += DeathRewardsOnOnKilledServer;
        }

        public void Awake()
        {
            SetupMonster();
        }

        public static GameObject BodyPrefab;

        public static GameObject MasterPrefab;

        public SkillLocator SkillLocator;
        
        private void SceneDirectorOnPopulateScene(ILContext il)
        {
            var cursor = new ILCursor(il);

            //The first TrySpawnObject is for interactables
            cursor.GotoNext(MoveType.After, x => x.MatchCallvirt("RoR2.DirectorCore", "TrySpawnObject"));
            cursor.Index += 1;
            cursor.Emit(OpCodes.Ldloc_S, (byte)5);
            cursor.EmitDelegate<System.Action<GameObject>>(OnSpawnInteractable);
        }

        private void OnSpawnInteractable(GameObject gameObj)
        {
            if (gameObj == null)
                return;

            var chest = gameObj.GetComponent<ChestBehavior>();
            if (chest != null)
            {
                if (_rng == null)
                {
                    _rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
                }

                if (_rng.nextNormalizedFloat < HailstormConfig.MimicChance.Value)
                {
                    Debug.Log("Mimic added");
                    var mimic = ChestMimicSpawner.Build(gameObj);
                    _chestMimics.Add(mimic);
                }
            }
        }

        private void DeathRewardsOnOnKilledServer(On.RoR2.DeathRewards.orig_OnKilledServer orig, DeathRewards self, DamageReport damagereport)
        {
            orig(self, damagereport);

            foreach (var mimic in _chestMimics.ToList())
            {
                if (mimic.BoundReward == self)
                {
                    PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(mimic.BoundItem), self.transform.position, 5f*Vector3.up);
                    _chestMimics.Remove(mimic);
                }
            }
        }

        private void SetupMonster()
        {
            SetupBody();
            SetupSkills();
            SetupMaster();
            RegisterAsSurvivor();
        }

        private void SetupBody()
        {
            //cloning the mushroom
            BodyPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/MiniMushroomBody"), "MimicBody");

            BodyPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;

            //instantiate the mimic's model
            GameObject model = HailstormAssets.MimicModel;

            GameObject modelBase = BodyPrefab.transform.Find("ModelBase").gameObject;

            //destroy mdlMushroom
            Object.Destroy(modelBase.transform.GetChild(0).gameObject);

            //replace it with our own model
            Transform transform = model.transform;
            transform.parent = modelBase.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            //ignore
            CharacterDirection characterDirection = BodyPrefab.GetComponent<CharacterDirection>();
            characterDirection.moveVector = Vector3.zero;
            characterDirection.targetTransform = modelBase.transform;
            characterDirection.overrideAnimatorForwardTransform = null;
            characterDirection.rootMotionAccumulator = null;
            characterDirection.modelAnimator = model.GetComponentInChildren<Animator>();
            characterDirection.driveFromRootRotation = false;
            characterDirection.turnSpeed = 720f;

            LanguageAPI.Add("MIMIC_NAME", "Mimic");

            //self explanatory
            CharacterBody bodyComponent = BodyPrefab.GetComponent<CharacterBody>();
            bodyComponent.bodyIndex = -1;
            bodyComponent.name = "MimicBody";
            bodyComponent.baseNameToken = "MIMIC_NAME";
            bodyComponent.subtitleNameToken = "NULL_SUBTITLE";
            bodyComponent.rootMotionInMainState = false;
            bodyComponent.mainRootSpeed = 0;
            bodyComponent.baseMaxHealth = 450;
            bodyComponent.levelMaxHealth = 120;
            bodyComponent.baseRegen = 0f;
            bodyComponent.levelRegen = 0f;
            bodyComponent.baseMaxShield = 0;
            bodyComponent.levelMaxShield = 0;
            bodyComponent.baseMoveSpeed = 7;
            bodyComponent.levelMoveSpeed = 0.1f;
            bodyComponent.baseAcceleration = 80;
            bodyComponent.baseJumpPower = 40;
            bodyComponent.levelJumpPower = 0;
            bodyComponent.baseDamage = 20;
            bodyComponent.levelDamage = 3.5f;
            bodyComponent.baseAttackSpeed = 1;
            bodyComponent.levelAttackSpeed = 0;
            bodyComponent.baseCrit = 0;
            bodyComponent.levelCrit = 0;
            bodyComponent.baseArmor = 0;
            bodyComponent.levelArmor = 0f;
            bodyComponent.baseJumpCount = 1;
            bodyComponent.sprintingSpeedMultiplier = 1.55f;
            bodyComponent.hideCrosshair = false;
            bodyComponent.crosshairPrefab = Resources.Load<GameObject>("Prefabs/Crosshair/SimpleDotCrosshair");
            bodyComponent.hullClassification = HullClassification.Human;
            //bodyComponent.portraitIcon = Assets.charPortrait;
            bodyComponent.isChampion = false;

            //also ignore
            CharacterMotor characterMotor = BodyPrefab.GetComponent<CharacterMotor>();
            characterMotor.walkSpeedPenaltyCoefficient = 1f;
            characterMotor.characterDirection = characterDirection;
            characterMotor.muteWalkMotion = false;
            characterMotor.mass = 100f;
            characterMotor.airControl = 0.25f;
            characterMotor.disableAirControlUntilCollision = false;
            characterMotor.generateParametersOnAwake = true;

            //set up the modellocator so stuff can find the model
            ModelLocator modelLocator = BodyPrefab.GetComponent<ModelLocator>();
            modelLocator.modelTransform = transform;
            modelLocator.modelBaseTransform = modelBase.transform;

            ChildLocator childLocator = model.GetComponent<ChildLocator>();

            //charactermodel and rendererinfo important for all visual stuff, skins, overlays, etc.
            CharacterModel characterModel = model.AddComponent<CharacterModel>();
            characterModel.body = bodyComponent;
            characterModel.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            characterModel.baseRendererInfos = new[]
            {
                new CharacterModel.RendererInfo
                {
                    defaultMaterial = model.GetComponentInChildren<SkinnedMeshRenderer>().material,
                    renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(),
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ignoreOverlays = false
                }
            };
            characterModel.autoPopulateLightInfos = true;
            characterModel.invisibilityCount = 0;
            characterModel.temporaryOverlays = new List<TemporaryOverlay>();

            //gotta replace the material with a new one using hotpoo shaders:tm: for maximum quality
            CharacterModel.RendererInfo[] rendererInfos = characterModel.baseRendererInfos;
            CharacterModel.RendererInfo[] array = new CharacterModel.RendererInfo[rendererInfos.Length];
            rendererInfos.CopyTo(array, 0);

            //clone commando material and replace with our own textures
            Material material = array[0].defaultMaterial;

            if (material)
            {
                material = Object.Instantiate(Resources.Load<GameObject>("Prefabs/NetworkedObjects/Chest/Chest1").GetComponentInChildren<SkinnedMeshRenderer>().material);
                // material.SetColor("_Color", Color.white);
                material.SetTexture("_MainTex", HailstormAssets.MimicMaterial.GetTexture("_MainTex"));
                material.SetColor("_EmColor", Color.white);
                material.SetFloat("_EmPower", 1);
                material.SetTexture("_EmTex", HailstormAssets.MimicMaterial.GetTexture("_EmissionMap"));

                array[0].defaultMaterial = material;
            }

            //now replace the material on our model
            characterModel.baseRendererInfos = array;            
            characterModel.SetFieldValue("mainSkinnedMeshRenderer", characterModel.baseRendererInfos[0].renderer.gameObject.GetComponent<SkinnedMeshRenderer>());

            TeamComponent teamComponent = null;
            if (BodyPrefab.GetComponent<TeamComponent>() != null) teamComponent = BodyPrefab.GetComponent<TeamComponent>();
            else teamComponent = BodyPrefab.GetComponent<TeamComponent>();
            teamComponent.hideAllyCardDisplay = false;
            teamComponent.teamIndex = TeamIndex.None;

            BodyPrefab.GetComponent<Interactor>().maxInteractionDistance = 3f;
            BodyPrefab.GetComponent<InteractionDriver>().highlightInteractor = true;

            //replace with custom sfx if applicable
            SfxLocator sfxLocator = BodyPrefab.GetComponent<SfxLocator>();
            sfxLocator.deathSound = "";
            sfxLocator.barkSound = "";
            sfxLocator.openSound = "";
            sfxLocator.landingSound = "Play_char_land";
            sfxLocator.fallDamageSound = "";
            sfxLocator.aliveLoopStart = "";
            sfxLocator.aliveLoopStop = "";

            Rigidbody rigidbody = BodyPrefab.GetComponent<Rigidbody>();
            rigidbody.mass = 100f;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0f;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.None;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rigidbody.constraints = RigidbodyConstraints.None;

            //add the main hurtbox
            HurtBoxGroup hurtBoxGroup = model.AddComponent<HurtBoxGroup>();

            HurtBox mainHurtbox = childLocator.FindChild("MainHurtbox").gameObject.AddComponent<HurtBox>();
            mainHurtbox.gameObject.layer = LayerIndex.entityPrecise.intVal;
            mainHurtbox.healthComponent = BodyPrefab.GetComponent<HealthComponent>();
            mainHurtbox.isBullseye = true;
            mainHurtbox.damageModifier = HurtBox.DamageModifier.Normal;
            mainHurtbox.hurtBoxGroup = hurtBoxGroup;
            mainHurtbox.indexInGroup = 0;
            hurtBoxGroup.hurtBoxes = new[] {mainHurtbox};
            hurtBoxGroup.OnValidate();

            //make a hitbox for the chomp
            HitBoxGroup hitBoxGroup = model.AddComponent<HitBoxGroup>();
            

            GameObject chompHitbox = childLocator.FindChild("ChompHitbox").gameObject;
            chompHitbox.transform.localScale = new Vector3(18f/180.0f, 18f/180.0f, 18f/180.0f);

            HitBox hitBox = chompHitbox.AddComponent<HitBox>();
            chompHitbox.layer = LayerIndex.projectile.intVal;

            hitBoxGroup.hitBoxes = new HitBox[]
            {
                hitBox
            };

            hitBoxGroup.groupName = "Chomp";

            //footstep sounds because mimics have feet
            FootstepHandler footstepHandler = model.AddComponent<FootstepHandler>();
            footstepHandler.baseFootstepString = "Play_player_footstep";
            footstepHandler.sprintFootstepOverrideString = "";
            footstepHandler.enableFootstepDust = true;
            footstepHandler.footstepDustPrefab = Resources.Load<GameObject>("Prefabs/GenericFootstepDust");

            //it doesn't have aim animations yet but doesn't hurt to include this for the future
            AimAnimator aimAnimator = model.AddComponent<AimAnimator>();
            aimAnimator.directionComponent = characterDirection;
            aimAnimator.pitchRangeMax = 60f;
            aimAnimator.pitchRangeMin = -60f;
            aimAnimator.yawRangeMin = -180f;
            aimAnimator.yawRangeMax = 180f;
            aimAnimator.pitchGiveupRange = 60f;
            aimAnimator.yawGiveupRange = 180f;
            aimAnimator.giveupDuration = 0.5f;
            aimAnimator.aimType = AimAnimator.AimType.Smart;
            aimAnimator.smoothTime = 0.1f;
            aimAnimator.inputBank = BodyPrefab.GetComponent<InputBankTest>();

            //Set up initial state
            var esm = BodyPrefab.GetComponent<EntityStateMachine>();
            esm.initialStateType = new SerializableEntityStateType(typeof(OrientToTargetState));

            //Add body to catalog
            BodyCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(BodyPrefab);
            };
        }

        private void SetupSkills()
        {
            foreach (GenericSkill obj in BodyPrefab.GetComponentsInChildren<GenericSkill>())
            {
                Object.DestroyImmediate(obj);
            }

            SkillLocator = BodyPrefab.GetComponent<SkillLocator>();
            PrimarySetup();
            UtilitySetup();
        }

        private void RegisterAsSurvivor()
        {
            SurvivorDef survivorDef = new SurvivorDef
            {
                name = "MIMIC_NAME",
                unlockableName = "",
                descriptionToken = "",
                primaryColor = Color.grey,
                bodyPrefab = BodyPrefab,
                displayPrefab = BodyPrefab,
                outroFlavorToken = "GENERIC_OUTRO_FLAVOR"
            };
            SurvivorAPI.AddSurvivor(survivorDef);
        }

        private void PrimarySetup()
        {
            var mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.skillNameToken = "MIMIC_MELEE";
            mySkillDef.activationState = new SerializableEntityStateType(typeof(MeleeAttackState));
            mySkillDef.activationStateMachineName = "Body";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 0f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isBullets = false;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;

            LoadoutAPI.AddSkillDef(mySkillDef);

            SkillLocator.primary = BodyPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            SkillLocator.primary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = SkillLocator.primary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };
        }

        private void UtilitySetup()
        {
            //LoadoutAPI.AddSkill(typeof());

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.skillNameToken = "MIMIC_POUNCE";
            mySkillDef.activationState = new SerializableEntityStateType(typeof(PreparePounceState));
            mySkillDef.activationStateMachineName = "Body";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 8f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Frozen;
            mySkillDef.isBullets = false;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;

            LoadoutAPI.AddSkillDef(mySkillDef);

            SkillLocator.utility = BodyPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            SkillLocator.utility.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = SkillLocator.utility.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };
        }
        private void SetupMaster()
        {
            MasterPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterMasters/BeetleMaster"), "MimicMaster");

            var master = MasterPrefab.GetComponent<CharacterMaster>();
            master.bodyPrefab = BodyPrefab;

            var baseAI = MasterPrefab.GetComponent<BaseAI>();
            baseAI.enemyAttentionDuration = 6000;
            baseAI.aimVectorMaxSpeed = 300;
            baseAI.aimVectorDampTime = 0.05f;

            var skillDrivers = MasterPrefab.GetComponents<AISkillDriver>();
            foreach (var oldDriver in skillDrivers)
                Object.Destroy(oldDriver);

            var meleeDriver = MasterPrefab.AddComponent<AISkillDriver>();
            meleeDriver.minDistance = 0;
            meleeDriver.maxDistance = 5;
            meleeDriver.customName = "Melee";
            meleeDriver.skillSlot = SkillSlot.Primary;
            meleeDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            meleeDriver.moveInputScale = 1.0f;
            meleeDriver.ignoreNodeGraph = true;
            meleeDriver.selectionRequiresTargetLoS = true;
            meleeDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            meleeDriver.requireSkillReady = true;
            meleeDriver.driverUpdateTimerOverride = -1;
            meleeDriver.movementType = AISkillDriver.MovementType.Stop;

            var pounceDriver = MasterPrefab.AddComponent<AISkillDriver>();
            pounceDriver.minDistance = 10;
            pounceDriver.maxDistance = 60;
            pounceDriver.customName = "Pounce";
            pounceDriver.skillSlot = SkillSlot.Utility;
            pounceDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            pounceDriver.ignoreNodeGraph = true;
            pounceDriver.selectionRequiresTargetLoS = true;
            pounceDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            pounceDriver.noRepeat = true;
            pounceDriver.requireSkillReady = true;
            pounceDriver.driverUpdateTimerOverride = -1;

            var walkDriver = MasterPrefab.AddComponent<AISkillDriver>();
            walkDriver.minDistance = 5;
            walkDriver.maxDistance = 20;
            walkDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            walkDriver.ignoreNodeGraph = true;
            walkDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            walkDriver.shouldSprint = true;
            walkDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            walkDriver.moveInputScale = 1.0f;
            walkDriver.driverUpdateTimerOverride = -1;
            walkDriver.skillSlot = SkillSlot.None;

            var routeDriver = MasterPrefab.AddComponent<AISkillDriver>();
            routeDriver.minDistance = 20;
            routeDriver.maxDistance = 150;
            routeDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            routeDriver.ignoreNodeGraph = false;
            routeDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            routeDriver.shouldSprint = true;
            routeDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            routeDriver.moveInputScale = 1.0f;
            routeDriver.driverUpdateTimerOverride = -1;
            routeDriver.skillSlot = SkillSlot.None;
        }
    }
}
