using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm.Cataclysm
{
    public sealed class LaserChargerInteraction : NetworkBehaviour, IInteractable, IDisplayNameProvider, ICustomHologramContentProvider
    {
        private ActivationState _previousState;
        private GameObject _teleporterPositionIndicator;
        private GameObject _clearRadiusIndicator;

        private ActivationState CurrentState
        {
            get { return (ActivationState)_stateInternal; }
            set { NetworkStateInternal = (uint)value; }
        }

        public bool IsFullyCharged => CurrentState == ActivationState.Charged;

        public float chargeDuration = 60;
        public float clearRadius = 60;
        public float remainingChargeTimer;

        [SyncVar]
        public uint chargePercent;

        [SyncVar]
        private uint _stateInternal;

        public uint NetworkStateInternal
        {
            get
            {
                return _stateInternal;
            }
            set
            {
                SetSyncVar(value, ref _stateInternal, 1u);
            }
        }

        public uint NetworkChargePercent
        {
            get
            {
                return chargePercent;
            }
            set
            {
                SetSyncVar(value, ref chargePercent, 2u);
            }
        }

        private void Awake()
        {
            remainingChargeTimer = chargeDuration;
        }

        private void Start()
        {
            if (_clearRadiusIndicator)
            {
                float num = clearRadius * 2f;
                _clearRadiusIndicator.transform.localScale = new Vector3(num, num, num);
            }
        }

        private void FixedUpdate()
        {
            if (_previousState != CurrentState)
            {
                OnStateChanged(_previousState, CurrentState);
            }

            _previousState = CurrentState;
            StateFixedUpdate();
        }

        private void OnStateChanged(ActivationState prevState, ActivationState nextState)
        {
            //TODO
        }

        private void StateFixedUpdate()
        {
            switch (CurrentState)
            {
                case ActivationState.Idle:
                    break;
                case ActivationState.Charging:
                    int chargeScale = Run.instance ? Run.instance.livingPlayerCount : 0;
                    float adjustedCharge = (chargeScale != 0) ? ((float)GetPlayerCountInRadius() / (float)chargeScale * Time.fixedDeltaTime) : 0f;
                    bool isCharging = adjustedCharge > 0f;
                    remainingChargeTimer = Mathf.Max(remainingChargeTimer - adjustedCharge, 0f);
                    if (NetworkServer.active)
                    {
                        NetworkChargePercent = (uint) ((byte) Mathf.RoundToInt(99f*(1f - remainingChargeTimer/chargeDuration)));
                    }
                    if (SceneWeatherController.instance)
                    {
                        SceneWeatherController.instance.weatherLerp = SceneWeatherController.instance.weatherLerpOverChargeTime.Evaluate(1f - remainingChargeTimer / chargeDuration);
                    }
                    if (!_teleporterPositionIndicator)
                    {
                        _teleporterPositionIndicator = Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/PositionIndicators/TeleporterChargingPositionIndicator"), transform.position, Quaternion.identity);
                        _teleporterPositionIndicator.GetComponent<PositionIndicator>().targetTransform = transform;
                    }
                    else
                    {
                        var component = _teleporterPositionIndicator.GetComponent<ChargeIndicatorController>();
                        component.isCharging = isCharging;
                        component.chargingText.text = chargePercent.ToString() + "%";
                    }

                    if (remainingChargeTimer <= 0f && NetworkServer.active)
                    {
                        CurrentState = ActivationState.Charged;
                        OnChargingFinished();
                    }

                    //We can go to sleep, as we've served our purpose
                    enabled = false;
                    break;
                case ActivationState.Charged:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private int GetPlayerCountInRadius()
        {
            int num = 0;
            Vector3 position = transform.position;
            float num2 = clearRadius * clearRadius;
            var teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Player);
            for (int i = 0; i < teamMembers.Count; i++)
            {
                if (Util.LookUpBodyNetworkUser(teamMembers[i].gameObject) && (teamMembers[i].transform.position - position).sqrMagnitude <= num2)
                {
                    num++;
                }
            }
            return num;
        }

        [Server]
        private void OnChargingFinished()
        {
        }

        public bool IsInChargingRange(GameObject gameObject)
        {
            return (gameObject.transform.position - transform.position).sqrMagnitude <= clearRadius * clearRadius;
        }

        public bool IsInChargingRange(CharacterBody characterBody)
        {
            return IsInChargingRange(characterBody.gameObject);
        }

        public string GetContextString(Interactor activator)
        {
            return "Begin charging";
        }

        public string GetDisplayName()
        {
            return "Power Source";
        }

        public Interactability GetInteractability(Interactor activator)
        {
            return CurrentState == ActivationState.Idle ? Interactability.Available : Interactability.Disabled;
        }

        public void OnInteractionBegin(Interactor activator)
        {
            if (CurrentState == ActivationState.Idle)
            {
                Chat.SendBroadcastChat(new Chat.SubjectChatMessage
                {
                    subjectCharacterBodyGameObject = activator.gameObject,
                    baseToken = "{0} has begun charging the laser power node"
                });
                CurrentState = ActivationState.Charging;
            }
        }

        public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
        {
            return false;
        }

        public bool ShouldShowOnScanner()
        {
            return true;
        }

        public bool ShouldDisplayHologram(GameObject viewer)
        {
            return CurrentState == ActivationState.Charging;
        }

        public GameObject GetHologramContentPrefab()
        {
            return Resources.Load<GameObject>("Prefabs/TimerHologramContent");
        }

        public void UpdateHologramContent(GameObject hologramContentObject)
        {
            var component = hologramContentObject.GetComponent<TimerHologramContent>();
            if (component)
            {
                component.displayValue = remainingChargeTimer;
            }
        }

        private void UNetVersion()
        {
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _stateInternal = reader.ReadPackedUInt32();
                chargePercent = reader.ReadPackedUInt32();
            }
            int num = (int)reader.ReadPackedUInt32();
            if ((num & 1) != 0)
            {
                _stateInternal = reader.ReadPackedUInt32();
            }
            if ((num & 2) != 0)
            {
                chargePercent = reader.ReadPackedUInt32();
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool forceAll)
        {
            if (forceAll)
            {
                writer.WritePackedUInt32(_stateInternal);
                writer.WritePackedUInt32(chargePercent);
                return true;
            }
            bool written = false;
            if ((syncVarDirtyBits & 1u) != 0u)
            {
                if (!written)
                {
                    writer.WritePackedUInt32(syncVarDirtyBits);
                    written = true;
                }
                writer.WritePackedUInt32(_stateInternal);
            }
            if ((syncVarDirtyBits & 2u) != 0u)
            {
                if (!written)
                {
                    writer.WritePackedUInt32(syncVarDirtyBits);
                    written = true;
                }
                writer.WritePackedUInt32(chargePercent);
            }
            if (!written)
            {
                writer.WritePackedUInt32(syncVarDirtyBits);
            }
            return written;
        }

        public static void AugmentPrefab(GameObject prefab)
        {
            var nid = prefab.AddComponent<NetworkIdentity>();

            var holo = prefab.AddComponent<CustomHologramProjector>();

            var lci = prefab.AddComponent<LaserChargerInteraction>();

            var h = prefab.AddComponent<Highlight>();
            h.highlightColor = Highlight.HighlightColor.teleporter;
        }

        private enum ActivationState
        {
            Idle,
            Charging,
            Charged
        }
    }
}
