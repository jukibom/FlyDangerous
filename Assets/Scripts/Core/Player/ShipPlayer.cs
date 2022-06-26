using System.Collections;
using Core.ShipModel;
using Mirror;
using UnityEngine;

namespace Core.Player {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ReflectionProbe))]
    public class ShipPlayer : FdPlayer {
        #region Attributes + Getters

        [SerializeField] private GameObject playerLogic;
        [SerializeField] private User user;
        [SerializeField] private ShipPhysics shipPhysics;

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
        private Rigidbody _rigidbody;

        private bool _isDriftEnabled;

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
        public ReflectionProbe ReflectionProbe { get; private set; }

        private bool IsReady => _transform && _serverReady;

        public bool Freeze {
            get => _rigidbody.constraints == RigidbodyConstraints.FreezeAll;
            set {
                if (_rigidbody) {
                    _rigidbody.constraints = value ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
                    // reinitialise rigidbody by resetting the params
                    ShipPhysics.CurrentParameters = ShipPhysics.CurrentParameters;
                }
            }
        }

        // The position and rotation of the ship within the world, taking into account floating origin fix
        public Vector3 AbsoluteWorldPosition {
            get {
                var position = transform.position;
                // if floating origin fix is active, overwrite position with corrected world space
                if (FloatingOrigin.Instance.FocalTransform == transform) position = FloatingOrigin.Instance.FocalObjectPosition;
                return position;
            }
            set {
                var position = value;
                // if floating origin fix is active, overwrite position with corrected world space
                if (FloatingOrigin.Instance.FocalTransform == transform) position -= FloatingOrigin.Instance.Origin;
                transform.position = position;
            }
        }

        #endregion


        #region Lifecycle + Misc

        public void Awake() {
            playerLogic.SetActive(false);
            _transform = transform;
            _rigidbody = GetComponent<Rigidbody>();
            ReflectionProbe = GetComponent<ReflectionProbe>();
            // always disable reflections until explicitly enabled by settings on the local client
            ReflectionProbe.enabled = false;
        }

        public void Start() {
            DontDestroyOnLoad(this);
        }

        private void OnEnable() {
            // perform positional correction on non-local client player objects like anything else in the world
            FloatingOrigin.OnFloatingOriginCorrection += NonLocalPlayerPositionCorrection;
            ShipPhysics.OnBoost += CmdBoost;
        }

        private void OnDisable() {
            FloatingOrigin.OnFloatingOriginCorrection -= NonLocalPlayerPositionCorrection;
            ShipPhysics.OnBoost -= CmdBoost;
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

            SetFlightAssistFromDefaults();

            var profile = ShipProfile.FromPreferences();
            CmdSetPlayerProfile(profile.playerName, profile.playerFlagFilename);
            CmdLoadShipModelPreferences(profile.shipModel, profile.primaryColor, profile.accentColor, profile.thrusterColor, profile.trailColor,
                profile.headLightsColor);

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
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

                // force new layer for non-local player
                var mask = LayerMask.NameToLayer("Non-Local Player");
                foreach (var transformObject in GetComponentsInChildren<Transform>(true)) transformObject.gameObject.layer = mask;
            }
        }

        private void RefreshShipModel() {
            IEnumerator RefreshShipAsync() {
                while (string.IsNullOrEmpty(_shipModelName)) yield return new WaitForFixedUpdate();
                ShipPhysics.RefreshShipModel(new ShipProfile(playerName, playerFlag, _shipModelName, _primaryColor, _accentColor, _thrusterColor, _trailColor,
                    _headLightsColor));
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

                user.InGameUI.ShipStats.UpdateIndicators(ShipPhysics.ShipIndicatorData);

                // update camera offset if not frozen
                var velocity = Freeze ? Vector3.zero : transform.InverseTransformDirection(ShipPhysics.Velocity);
                var frameThrust = Freeze ? Vector3.zero : ShipPhysics.CurrentFrameThrust;
                User.ShipCameraRig.UpdateCameras(velocity, ShipPhysics.CurrentParameters.maxSpeed, frameThrust, ShipPhysics.CurrentParameters.maxThrust);

                // Send the current floating origin along with the new position and rotation to the server
                CmdUpdate(FloatingOrigin.Instance.Origin, _transform.localPosition, _transform.rotation, ShipPhysics.Velocity, ShipPhysics.AngularVelocity,
                    ShipPhysics.CurrentFrameThrust, ShipPhysics.CurrentFrameTorque);

                ShipPhysics.CheckpointCollisionCheck();
            }
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

        public void ShipLightsToggle() {
            ShipPhysics.ShipLightsToggle(CmdSetLights);
        }

        public void VelocityLimiterIsPressed(bool isPressed) {
            if (_velocityLimiterActive != isPressed) {
                _velocityLimiterActive = isPressed;

                ShipPhysics.ShipModel?.SetVelocityLimiter(_velocityLimiterActive);
            }
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
        private void RpcUpdate(Vector3 remoteOrigin, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, Vector3 thrust,
            Vector3 torque) {
            if (!isLocalPlayer && IsReady) {
                // Calculate the local difference to position based on the local clients' floating origin.
                // If these values are gigantic, that doesn't really matter as they only update at fixed distances.
                // We'll lose precision here but we add our position on top after-the-fact, so we always have
                // local-level precision.
                var offset = remoteOrigin - FloatingOrigin.Instance.Origin;
                var localPosition = offset + position;

                _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, velocity, 0.1f);
                _rigidbody.angularVelocity = angularVelocity;
                _transform.localPosition = Vector3.Lerp(_transform.localPosition, localPosition, 0.5f);
                _transform.localRotation = Quaternion.Lerp(_transform.localRotation, rotation, 0.5f);

                // add velocity to position as position would have moved on server at that velocity
                transform.localPosition += velocity * Time.fixedDeltaTime;

                ShipPhysics.UpdateMotionInformation(velocity, thrust, torque);
            }
        }

        [Command]
        private void CmdBoost(float boostTime) {
            // do local boost effects immediately
            if (isLocalPlayer)
                ShipPhysics.ShipModel?.Boost(boostTime);
            // signal other clients to reflect boost effect
            RpcBoost(boostTime);
        }

        [ClientRpc]
        private void RpcBoost(float boostTime) {
            if (!isLocalPlayer)
                ShipPhysics.ShipModel?.Boost(boostTime);
        }

        [Command]
        private void CmdSetLights(bool active) {
            RpcSetLights(active);
        }

        [ClientRpc]
        private void RpcSetLights(bool active) {
            ShipPhysics.ShipModel?.SetLights(active);
        }

        private void NonLocalPlayerPositionCorrection(Vector3 offset) {
            if (!isLocalPlayer) transform.position -= offset;
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