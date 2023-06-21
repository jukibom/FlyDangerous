using System.Collections;
using Core.ShipModel;
using Mirror;
using Misc;
using UnityEngine;

namespace Core.Player {
    [RequireComponent(typeof(Rigidbody))]
    public class ShipPlayer : FdPlayer {
        #region Attributes + Getters

        [SerializeField] private GameObject playerLogic;
        [SerializeField] private User user;
        [SerializeField] private ShipPhysics shipPhysics;
        [SerializeField] private ReflectionProbe reflectionProbe;

        public User User => user;
        public ShipPhysics ShipPhysics => shipPhysics;

        private bool _boostButtonHeld;
        private bool _velocityLimiterActive;
        private bool _flightAssistVectorControl;
        private bool _flightAssistRotationalControl;

        // input axes -1 to 1
        private float _throttleInput;
        private float _latVInput;
        private float _latHInput;
        private float _pitchInput;
        private float _yawInput;
        private float _rollInput;

        private Transform _transform;

        private bool _isDriftEnabled;

        public Rigidbody Rigidbody { get; private set; }

        public bool IsAutoRotateDriftEnabled {
            get => _isDriftEnabled;
            set {
                _isDriftEnabled = value;
                // update indicators and play sound effect if either flight assists are active
                if (_flightAssistVectorControl || _flightAssistRotationalControl)
                    ShipPhysics.ShipModel?.SetAssist(AssistToggleType.Both, !_isDriftEnabled);
            }
        }

        public bool IsVectorFlightAssistActive => !IsAutoRotateDriftEnabled &&
                                                  (_flightAssistVectorControl || Preferences.Instance.GetBool("autoShipRotation") ||
                                                   Preferences.Instance.GetString("controlSchemeType") == "arcade");

        public bool IsRotationalFlightAssistActive => !IsAutoRotateDriftEnabled &&
                                                      (_flightAssistRotationalControl || Preferences.Instance.GetBool("autoShipRotation") ||
                                                       Preferences.Instance.GetString("controlSchemeType") == "arcade");

        [SyncVar] private bool _serverReady;
        [SyncVar] public string playerName;
        [SyncVar] public string playerFlag;
        [SyncVar] public bool isHost;

        [SyncVar] private string _shipModelName;
        [SyncVar] private string _primaryColor;
        [SyncVar] private string _accentColor;
        [SyncVar] private string _thrusterColor;
        [SyncVar] private string _trailColor;
        [SyncVar] private string _headLightsColor;
        public Flag PlayerFlag { get; private set; }
        public ReflectionProbe ReflectionProbe => reflectionProbe;

        private bool IsReady => _transform && _serverReady;

        public bool Freeze {
            get => Rigidbody.constraints == RigidbodyConstraints.FreezeAll;
            set {
                if (Rigidbody) {
                    Rigidbody.constraints = value ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
                    // reinitialise rigidbody by resetting the params
                    ShipPhysics.FlightParameters = ShipPhysics.FlightParameters;
                }
            }
        }

        public Vector3 Position => _transform.position;

        // The position and rotation of the ship within the world, taking into account floating origin fix
        public Vector3 AbsoluteWorldPosition => FloatingOrigin.Instance.FocalTransform == _transform ? FloatingOrigin.Instance.FocalObjectPosition : Position;

        // Set the position and rotation of the ship to a relative / local position with respect to itself 
        public void SetTransformLocal(Vector3 position, Quaternion rotation) {
            GetComponent<Rigidbody>().Move(position, rotation);
        }

        // Set the position and rotation of the ship to a world location, taking into account floating origin
        public void SetTransformWorld(Vector3 position, Quaternion rotation) {
            SetTransformLocal(FloatingOrigin.Instance.FocalTransform == transform ? position - FloatingOrigin.Instance.Origin : position, rotation);
        }

        #endregion

        #region Lifecycle + Misc

        public void Awake() {
            playerLogic.SetActive(false);
            _transform = transform;
            Rigidbody = GetComponent<Rigidbody>();

            // always disable reflections until explicitly enabled by settings on the local client
            ReflectionProbe.gameObject.SetActive(false);
        }

        public void Start() {
            DontDestroyOnLoad(this);
        }

        private void OnEnable() {
            // perform positional correction on non-local client player objects like anything else in the world
            FloatingOrigin.OnFloatingOriginCorrection += NonLocalPlayerPositionCorrection;
            ShipPhysics.OnBoost += OnBoost;
            ShipPhysics.OnBoostCancel += OnBoostCancel;
            ShipPhysics.OnWaterSubmerged += OnWaterSubmerged;
            ShipPhysics.OnWaterEmerged += OnWaterEmerged;
        }

        private void OnDisable() {
            FloatingOrigin.OnFloatingOriginCorrection -= NonLocalPlayerPositionCorrection;
            ShipPhysics.OnBoost -= OnBoost;
            ShipPhysics.OnBoostCancel -= OnBoostCancel;
            ShipPhysics.OnWaterSubmerged -= OnWaterSubmerged;
            ShipPhysics.OnWaterEmerged -= OnWaterEmerged;
        }

        public override void OnStartLocalPlayer() {
            // set tag for terrain generation focus
            gameObject.tag = "Terrain Gen Marker";

            // enable input, camera, effects etc
            playerLogic.SetActive(true);

            // register self as floating origin focus
            if (FloatingOrigin.Instance.FocalTransform != null)
                FloatingOrigin.Instance.SwapFocalTransform(transform);
            else
                FloatingOrigin.Instance.FocalTransform = transform;

            // register local player UI 
            ShipPhysics.FeedbackEngine.SubscribeFeedbackObject(user.InGameUI.ShipStats);
            ShipPhysics.FeedbackEngine.SubscribeFeedbackObject(user.InGameUI.IndicatorSystem);

            // register integrations
            foreach (var integration in Engine.Instance.Integrations) ShipPhysics.FeedbackEngine.SubscribeFeedbackObject(integration);

            SetFlightAssistFromDefaults();

            var profile = ShipProfile.FromPreferences();
            CmdSetPlayerProfile(profile.playerName, profile.playerFlagFilename);
            CmdLoadShipModelPreferences(profile.shipModel, profile.primaryColor, profile.accentColor, profile.thrusterColor, profile.trailColor,
                profile.headLightsColor);

            // always disable the night vision post process shader until enabled by user action
            var nightVisionColor = ColorExtensions.ParseHtmlColor(profile.headLightsColor);
            Engine.Instance.NightVision.SetNightVisionColor(nightVisionColor);
            Engine.Instance.NightVision.SetNightVisionActive(false);

            RefreshShipModel();
        }

        // When a client connects, update all other ships on that local client
        public override void OnStartClient() {
            base.OnStartClient();
            foreach (var shipPlayer in FindObjectsOfType<ShipPlayer>())
                if (!shipPlayer.isLocalPlayer)
                    shipPlayer.RefreshShipModel();


            if (!isLocalPlayer) {
                // rigidbody angular momentum constraints 
                // TODO: Is this needed??
                Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

                // ensure local ships don't try to do clever interpolation of rigidbody (there's too much going on!)
                Rigidbody.interpolation = RigidbodyInterpolation.None;

                shipPhysics.ShipActive = true;

                // force new layer for non-local player
                var mask = LayerMask.NameToLayer("Non-Local Player");
                var protectedMask = LayerMask.NameToLayer("TransparentFX");
                foreach (var transformObject in GetComponentsInChildren<Transform>(true))
                    if (transformObject.gameObject.layer != protectedMask)
                        transformObject.gameObject.layer = mask;
            }
        }

        private void RefreshShipModel() {
            IEnumerator RefreshShipAsync() {
                while (string.IsNullOrEmpty(_shipModelName)) yield return new WaitForFixedUpdate();
                ShipPhysics.ShipProfile = new ShipProfile(playerName, playerFlag, _shipModelName, _primaryColor, _accentColor, _thrusterColor, _trailColor,
                    _headLightsColor);

                if (isLocalPlayer && shipPhysics.ShipModel != null) {
                    shipPhysics.ShipModel.ShipCameraRig = user.ShipCameraRig;

                    // subscribe to shield changes
                    shipPhysics.ShipModel.Shield.OnShieldImpact -= CmdShieldImpact;
                    shipPhysics.ShipModel.Shield.OnShieldImpact += CmdShieldImpact;
                }

                // handle any ship model specific stuff
                shipPhysics.ShipModel?.SetIsLocalPlayer(isLocalPlayer);
            }

            StartCoroutine(RefreshShipAsync());
        }

        // called when the server has finished instantiating all players
        public void ServerReady() {
            _serverReady = true;
        }

        public void Reset() {
            _pitchInput = 0;
            _rollInput = 0;
            _yawInput = 0;
            _throttleInput = 0;
            _latHInput = 0;
            _latVInput = 0;
            _boostButtonHeld = false;
            _velocityLimiterActive = false;

            ShipPhysics.ResetPhysics();

            User.ShipCameraRig.Reset();
        }

        public void SetFlightAssistFromDefaults() {
            var flightAssistPreference = Preferences.Instance.GetString("flightAssistDefault");
            switch (flightAssistPreference) {
                case "vector assist only":
                    _flightAssistVectorControl = true;
                    _flightAssistRotationalControl = false;
                    break;
                case "rotational assist only":
                    _flightAssistVectorControl = false;
                    _flightAssistRotationalControl = true;
                    break;
                case "all off":
                    _flightAssistVectorControl = false;
                    _flightAssistRotationalControl = false;
                    break;
                default:
                    _flightAssistVectorControl = true;
                    _flightAssistRotationalControl = true;
                    break;
            }
        }

        // Apply all physics updates in fixed intervals (WRITE)
        private void FixedUpdate() {
            if (isLocalPlayer && IsReady) {
                ShipPhysics.UpdateShip(_pitchInput, _rollInput, _yawInput, _throttleInput, _latHInput, _latVInput, _boostButtonHeld, _velocityLimiterActive,
                    IsVectorFlightAssistActive, IsRotationalFlightAssistActive);

                // update camera offset if not frozen
                var velocity = Freeze ? Vector3.zero : transform.InverseTransformDirection(ShipPhysics.Velocity);
                var frameThrust = Freeze ? Vector3.zero : ShipPhysics.CurrentFrameThrust;
                User.ShipCameraRig.UpdateCameras(velocity, ShipPhysics.FlightParameters.maxSpeed, frameThrust, ShipPhysics.FlightParameters.maxThrust);

                // Send the current floating origin along with the new position and rotation to the server
                CmdUpdate(FloatingOrigin.Instance.Origin, _transform.localPosition, _transform.rotation, ShipPhysics.Velocity, ShipPhysics.AngularVelocity,
                    ShipPhysics.CurrentFrameThrust, ShipPhysics.CurrentFrameTorque);

                ShipPhysics.LocalPlayerTriggerCollisionChecks();
                ShipPhysics.GeometryCollisionCheck();
            }
        }

        private void OnCollisionEnter(Collision collisionInfo) {
            ShipPhysics.OnCollision(collisionInfo, true);
        }

        private void OnCollisionStay(Collision collisionInfo) {
            ShipPhysics.OnCollision(collisionInfo, false);
        }

        #endregion

        #region Input

        public void SetPitch(float value) {
            _pitchInput = ClampInput(value);
        }

        public void SetRoll(float value) {
            _rollInput = ClampInput(value);
        }

        public void SetYaw(float value) {
            _yawInput = ClampInput(value);
        }

        public void SetThrottle(float value) {
            _throttleInput = ClampInput(value);
        }

        public void SetLateralH(float value) {
            _latHInput = ClampInput(value);
        }

        public void SetLateralV(float value) {
            _latVInput = ClampInput(value);
        }

        public void Boost(bool isPressed) {
            _boostButtonHeld = isPressed;
        }

        public void SetAllFlightAssistEnabled(bool isEnabled, bool updateShipModel = true) {
            _flightAssistVectorControl = isEnabled;
            _flightAssistRotationalControl = isEnabled;

            if (updateShipModel) ShipPhysics.ShipModel?.SetAssist(AssistToggleType.Both, isEnabled);
            if (Preferences.Instance.GetBool("forceRelativeMouseWithFAOff")) User.ResetMouseToCentre();
        }

        public void SetFlightAssistVectorControlEnabled(bool isEnabled, bool updateShipModel = true) {
            _flightAssistVectorControl = isEnabled;
            if (updateShipModel) ShipPhysics.ShipModel?.SetAssist(AssistToggleType.Vector, _flightAssistVectorControl);
        }

        public void SetFlightAssistRotationalDampeningEnabled(bool isEnabled, bool updateShipModel = true) {
            _flightAssistRotationalControl = isEnabled;
            if (updateShipModel) ShipPhysics.ShipModel?.SetAssist(AssistToggleType.Rotational, _flightAssistRotationalControl);
            if (Preferences.Instance.GetBool("forceRelativeMouseWithFAOff")) User.ResetMouseToCentre();
        }

        public void SetNightVisionEnabled(bool isEnabled) {
            ShipPhysics.NightVisionToggle(isEnabled, CmdSetNightVision);
        }

        public void VelocityLimiterIsPressed(bool isPressed) {
            if (_velocityLimiterActive != isPressed) {
                _velocityLimiterActive = isPressed;

                ShipPhysics.ShipModel?.SetVelocityLimiter(_velocityLimiterActive);
            }
        }

        private void OnBoost(float spoolTime, float boostTime) {
            // do local boost effects immediately
            if (isLocalPlayer)
                ShipPhysics.ShipModel?.Boost(spoolTime, boostTime);
            // signal other clients to reflect boost effect
            CmdBoost(spoolTime, boostTime);
        }

        private void OnBoostCancel() {
            // local boost cancel immediately
            if (isLocalPlayer)
                ShipPhysics.ShipModel?.BoostCancel();
            // signal to other players
            CmdBoostCancel();
        }

        private void OnWaterSubmerged() {
            // local do immediately
            if (isLocalPlayer)
                ShipPhysics.ShipModel?.WaterSubmerged(AbsoluteWorldPosition, ShipPhysics.Velocity);
            // signal to other players
            CmdWaterSubmerged(AbsoluteWorldPosition);
        }

        private void OnWaterEmerged() {
            // local do immediately
            if (isLocalPlayer)
                ShipPhysics.ShipModel?.WaterEmerged(AbsoluteWorldPosition, ShipPhysics.Velocity);
            // signal to other players
            CmdWaterEmerged(AbsoluteWorldPosition);
        }

        /**
         * All axis should be between -1 and 1.
         */
        private float ClampInput(float input) {
            return Mathf.Min(Mathf.Max(input, -1), 1);
        }

        #endregion

        #region Network Position Sync etc

        // This is server-side and should really validate the positions coming in before blindly firing to all the clients!
        [Command]
        private void CmdUpdate(Vector3 origin, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, Vector3 thrust,
            Vector3 torque) {
            RpcUpdate(origin, position, rotation, velocity, angularVelocity, thrust, torque);
        }

        // On each client, update the position of this object if it's not the local player.
        [ClientRpc]
        private void RpcUpdate(Vector3 remoteOrigin, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, Vector3 thrust, Vector3 torque) {
            if (!isLocalPlayer && IsReady) {
                // Calculate the local difference to position based on the local clients' floating origin.
                // If these values are gigantic, that doesn't really matter as they only update at fixed distances.
                // We'll lose precision here but we add our position on top after-the-fact, so we always have
                // local-level precision (when clients are near to each other and that precision is important).
                var localOffset = FloatingOrigin.Instance.Origin;
                var offset = remoteOrigin - localOffset;
                var localPosition = offset + position;

                Rigidbody.velocity = Vector3.Lerp(Rigidbody.velocity, velocity, 0.1f);
                Rigidbody.angularVelocity = angularVelocity;
                _transform.localPosition = Vector3.Lerp(_transform.localPosition, localPosition, 0.5f);
                _transform.localRotation = Quaternion.Lerp(_transform.localRotation, rotation, 0.5f);

                // add velocity to position as position would have moved on server at that velocity
                transform.localPosition += velocity * Time.fixedDeltaTime;

                ShipPhysics.UpdateBoostStatus();
                ShipPhysics.UpdateMotionData(velocity, thrust, torque);
            }
        }

        [Command]
        private void CmdBoost(float spoolTime, float boostTime) {
            RpcBoost(spoolTime, boostTime);
        }

        [ClientRpc]
        private void RpcBoost(float spoolTime, float boostTime) {
            if (!isLocalPlayer) ShipPhysics.ShipModel?.Boost(spoolTime, boostTime);
        }

        [Command]
        private void CmdBoostCancel() {
            RpcBoostCancel();
        }

        [ClientRpc]
        private void RpcBoostCancel() {
            if (!isLocalPlayer) ShipPhysics.ShipModel?.BoostCancel();
        }

        [Command]
        private void CmdWaterSubmerged(Vector3 atWorldPosition) {
            RpcWaterSubmerged(atWorldPosition);
        }

        [ClientRpc]
        private void RpcWaterSubmerged(Vector3 atWorldPosition) {
            if (!isLocalPlayer) ShipPhysics.ShipModel?.WaterSubmerged(atWorldPosition, ShipPhysics.Velocity);
        }

        [Command]
        private void CmdWaterEmerged(Vector3 atWorldPosition) {
            RpcWaterEmerged(atWorldPosition);
        }

        [ClientRpc]
        private void RpcWaterEmerged(Vector3 atWorldPosition) {
            if (!isLocalPlayer) ShipPhysics.ShipModel?.WaterEmerged(atWorldPosition, ShipPhysics.Velocity);
        }

        [Command]
        private void CmdShieldImpact(float impactForceNormalised, Vector3 impactDirection) {
            RpcSetShieldImpact(impactForceNormalised, impactDirection);
        }

        [ClientRpc]
        private void RpcSetShieldImpact(float impactForceNormalised, Vector3 impactDirection) {
            if (!isLocalPlayer) ShipPhysics.ShipModel?.Shield.ShieldImpact(impactForceNormalised, impactDirection);
        }

        [Command]
        private void CmdSetNightVision(bool active) {
            // Trigger night vision post process effects
            if (isLocalPlayer)
                Engine.Instance.NightVision.SetNightVisionActive(active);

            RpcSetNightVision(active);
        }

        [ClientRpc]
        private void RpcSetNightVision(bool active) {
            ShipPhysics.ShipModel?.SetNightVision(active);
        }

        private void NonLocalPlayerPositionCorrection(Vector3 offset) {
            if (!isLocalPlayer) _transform.position -= offset;
        }

        [Command]
        private void CmdSetPlayerProfile(string newName, string flag) {
            if (newName == "") newName = "UNNAMED SCRUB";

            playerName = newName;
            playerFlag = flag;
            RpcSetFlag(flag);
        }

        [ClientRpc]
        private void RpcSetFlag(string flagFilename) {
            PlayerFlag = Flag.FromFilename(flagFilename);
        }

        [Command]
        private void CmdLoadShipModelPreferences(string shipModel, string primaryColor, string accentColor,
            string thrusterColor, string trailColor, string headLightsColor) {
            _shipModelName = shipModel;
            _primaryColor = primaryColor;
            _accentColor = accentColor;
            _thrusterColor = thrusterColor;
            _trailColor = trailColor;
            _headLightsColor = headLightsColor;
        }

        #endregion
    }
}