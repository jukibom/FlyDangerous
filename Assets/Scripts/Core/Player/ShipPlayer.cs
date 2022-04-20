using System;
using System.Collections;
using Audio;
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

        // flight assist targets
        private float _throttleTargetFactor;
        private float _latHTargetFactor;
        private float _latVTargetFactor;
        private float _pitchTargetFactor;
        private float _rollTargetFactor;
        private float _yawTargetFactor;

        private Transform _transform;
        private Rigidbody _rigidbody;

        public bool IsVectorFlightAssistActive => _flightAssistVectorControl || Preferences.Instance.GetBool("autoShipRotation") ||
                                                  Preferences.Instance.GetString("controlSchemeType") == "arcade";

        public bool IsRotationalFlightAssistActive => _flightAssistRotationalControl || Preferences.Instance.GetBool("autoShipRotation") ||
                                                      Preferences.Instance.GetString("controlSchemeType") == "arcade";

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
            // set tag for finding for e.g. terrain generation focus
            gameObject.tag = "LocalPlayer";

            // enable input, camera, effects etc
            playerLogic.SetActive(true);

            // register self as floating origin focus
            if (FloatingOrigin.Instance.FocalTransform != null)
                FloatingOrigin.Instance.SwapFocalTransform(transform);
            else
                FloatingOrigin.Instance.FocalTransform = transform;

            SetFlightAssistDefaults(Preferences.Instance.GetString("flightAssistDefault"));

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
            _throttleTargetFactor = 0;
            _latHTargetFactor = 0;
            _latVTargetFactor = 0;
            _pitchTargetFactor = 0;
            _rollTargetFactor = 0;
            _yawTargetFactor = 0;
            _boostButtonHeld = false;
            _velocityLimiterActive = false;

            ShipPhysics.Reset();

            User.ShipCameraRig.Reset();
        }

        public void SetFlightAssistDefaults(string preference) {
            switch (preference) {
                case "vector assist only":
                    _flightAssistVectorControl = true;
                    break;
                case "rotational assist only":
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
                var maxSpeedWithBoost = ShipPhysics.CurrentParameters.maxSpeed + ShipPhysics.BoostedMaxSpeedDelta;

                /* FLIGHT ASSISTS */
                if (IsVectorFlightAssistActive) CalculateVectorControlFlightAssist(maxSpeedWithBoost);
                if (IsRotationalFlightAssistActive) CalculateRotationalDampeningFlightAssist();

                ShipPhysics.indicatorThrottleLocation = new Optional<float>(
                    _flightAssistVectorControl
                        ? _throttleTargetFactor
                        : _throttleInput
                );

                ShipPhysics.UpdateShip(_pitchInput, _rollInput, _yawInput, _throttleInput, _latHInput, _latVInput, _boostButtonHeld, _velocityLimiterActive,
                    IsVectorFlightAssistActive, IsRotationalFlightAssistActive);

                user.InGameUI.ShipStats.UpdateIndicators(ShipPhysics.ShipIndicatorData);
                User.ShipCameraRig.UpdateCameras(transform.InverseTransformDirection(ShipPhysics.Velocity), ShipPhysics.CurrentParameters.maxSpeed,
                    ShipPhysics.CurrentFrameThrust,
                    ShipPhysics.CurrentParameters.maxThrust);

                // Send the current floating origin along with the new position and rotation to the server
                CmdUpdate(FloatingOrigin.Instance.Origin, _transform.localPosition, _transform.rotation, ShipPhysics.Velocity, ShipPhysics.AngularVelocity,
                    ShipPhysics.CurrentFrameThrust, ShipPhysics.CurrentFrameTorque);

                ShipPhysics.CheckpointCollisionCheck();
            }
        }

        #endregion


        #region Input

        public void SetPitch(float value) {
            if (IsRotationalFlightAssistActive)
                _pitchTargetFactor = ClampInput(value);
            else
                _pitchInput = ClampInput(value);
        }

        public void SetRoll(float value) {
            if (IsRotationalFlightAssistActive)
                _rollTargetFactor = ClampInput(value);
            else
                _rollInput = ClampInput(value);
        }

        public void SetYaw(float value) {
            if (IsRotationalFlightAssistActive)
                _yawTargetFactor = ClampInput(value);
            else
                _yawInput = ClampInput(value);
        }

        public void SetThrottle(float value) {
            if (IsVectorFlightAssistActive)
                _throttleTargetFactor = ClampInput(value);
            else
                _throttleInput = ClampInput(value);
        }

        public void SetLateralH(float value) {
            if (IsVectorFlightAssistActive)
                _latHTargetFactor = ClampInput(value);
            else
                _latHInput = ClampInput(value);
        }

        public void SetLateralV(float value) {
            if (IsVectorFlightAssistActive)
                _latVTargetFactor = ClampInput(value);
            else
                _latVInput = ClampInput(value);
        }

        public void Boost(bool isPressed) {
            _boostButtonHeld = isPressed;
        }

        public void AllFlightAssistToggle() {
            // if any flight assist is enabled, deactivate (any on = all off)
            var isEnabled = !(IsVectorFlightAssistActive | IsRotationalFlightAssistActive);

            // if user has all flight assists on by default, flip that logic on its head (any off = all on)
            if (Preferences.Instance.GetString("flightAssistDefault") == "all on")
                isEnabled = !(IsVectorFlightAssistActive & IsRotationalFlightAssistActive);

            _flightAssistVectorControl = isEnabled;
            _flightAssistRotationalControl = isEnabled;

            Debug.Log("All Flight Assists " + (isEnabled ? "ON" : "OFF"));

            UIAudioManager.Instance.Play(isEnabled ? "ship-alternate-flight-on" : "ship-alternate-flight-off");

            if (Preferences.Instance.GetBool("forceRelativeMouseWithFAOff")) User.ResetMouseToCentre();
        }

        public void FlightAssistVectorControlToggle() {
            _flightAssistVectorControl = !_flightAssistVectorControl;
            Debug.Log("Vector Control Flight Assist " + (_flightAssistVectorControl ? "ON" : "OFF"));

            if (_flightAssistVectorControl)
                UIAudioManager.Instance.Play("ship-alternate-flight-on");
            else
                UIAudioManager.Instance.Play("ship-alternate-flight-off");
        }

        public void FlightAssistRotationalDampeningToggle() {
            _flightAssistRotationalControl = !_flightAssistRotationalControl;
            Debug.Log("Rotational Dampening Flight Assist " + (_flightAssistRotationalControl ? "ON" : "OFF"));

            if (_flightAssistRotationalControl)
                UIAudioManager.Instance.Play("ship-alternate-flight-on");
            else
                UIAudioManager.Instance.Play("ship-alternate-flight-off");

            if (Preferences.Instance.GetBool("forceRelativeMouseWithFAOff")) User.ResetMouseToCentre();
        }

        public void ShipLightsToggle() {
            ShipPhysics.ShipLightsToggle(CmdSetLights);
        }

        public void VelocityLimiterIsPressed(bool isPressed) {
            if (_velocityLimiterActive != isPressed) {
                _velocityLimiterActive = isPressed;
                if (_velocityLimiterActive)
                    UIAudioManager.Instance.Play("ship-velocity-limit-on");
                else
                    UIAudioManager.Instance.Play("ship-velocity-limit-off");
            }
        }

        /**
         * All axis should be between -1 and 1.
         */
        private float ClampInput(float input) {
            return Mathf.Min(Mathf.Max(input, -1), 1);
        }

        #endregion


        #region Flight Assist Calculations

        private void CalculateVectorControlFlightAssist(float maxSpeedWithBoost) {
            // TODO: Correctly calculate gravity for FA (need the actual velocity from acceleration caused in the previous frame)
            var localVelocity = transform.InverseTransformDirection(_rigidbody.velocity);

            CalculateAssistedAxis(_latHTargetFactor, localVelocity.x, 0.1f, maxSpeedWithBoost, out _latHInput);
            CalculateAssistedAxis(_latVTargetFactor, localVelocity.y, 0.1f, maxSpeedWithBoost, out _latVInput);
            CalculateAssistedAxis(_throttleTargetFactor, localVelocity.z, 0.1f, maxSpeedWithBoost, out _throttleInput);
        }

        private void CalculateRotationalDampeningFlightAssist() {
            // convert global rigid body velocity into local space
            var localAngularVelocity = transform.InverseTransformDirection(_rigidbody.angularVelocity);

            CalculateAssistedAxis(_pitchTargetFactor, localAngularVelocity.x * -1, 1f, 3.0f, out _pitchInput);
            CalculateAssistedAxis(_yawTargetFactor, localAngularVelocity.y, 1f, 3.0f, out _yawInput);
            CalculateAssistedAxis(_rollTargetFactor, localAngularVelocity.z * -1, 1f, 3.0f, out _rollInput);
        }

        /**
         * Given a target factor between 0 and 1 for a given axis, the current gross value and the maximum, calculate a
         * new axis value to apply as input.
         * @param targetFactor value between 0 and 1 (effectively the users' input)
         * @param currentAxisVelocity the non-normalised raw value of the current motion of the axis
         * @param interpolateAtPercent the point at which to begin linearly interpolating the acceleration
         * (e.g. 0.1 = at 10% of the MAXIMUM velocity of the axis away from the target, interpolate the axis -
         * if the current speed is 0, the target is 0.5 and this value is 0.1, this means that at 40% of the maximum
         * speed -- when the axis is at 0.4 -- decrease the output linearly such that it moves from 1 to 0 and slowly
         * decelerates.
         * @param max the maximum non-normalised value for this axis e.g. the maximum speed or maximum rotation in radians etc
         * @param out axis the value to apply the calculated new axis of input to
         */
        private void CalculateAssistedAxis(
            float targetFactor,
            float currentAxisVelocity,
            float interpolateAtPercent,
            float max,
            out float axis
        ) {
            var targetRate = max * targetFactor;

            // prevent tiny noticeable movement on start and jitter
            if (Math.Abs(currentAxisVelocity - targetRate) < 0.000001f) {
                axis = 0;
                return;
            }

            // basic max or min
            axis = currentAxisVelocity - targetRate < 0 ? 1 : -1;

            // interpolation over final range (interpolateAtPercent)
            var velocityInterpolateRange = max * interpolateAtPercent;

            // positive motion
            if (currentAxisVelocity < targetRate && currentAxisVelocity > targetRate - velocityInterpolateRange) {
                var startInterpolate = targetRate - velocityInterpolateRange;
                axis *= Mathf.InverseLerp(targetRate, startInterpolate, currentAxisVelocity);
            }

            // negative motion
            if (currentAxisVelocity > targetRate && currentAxisVelocity < targetRate + velocityInterpolateRange) {
                var startInterpolate = targetRate + velocityInterpolateRange;
                axis *= Mathf.InverseLerp(targetRate, startInterpolate, currentAxisVelocity);
            }
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
            RpcBoost(boostTime);
        }

        [ClientRpc]
        private void RpcBoost(float boostTime) {
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