using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Audio;
using Core;
using Den.Tools;
using JetBrains.Annotations;
using Mirror;
using Misc;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Player {
    public class ShipParameters {
        public float mass;
        public float drag;
        public float angularDrag;
        public float inertiaTensorMultiplier;
        public float maxSpeed;
        public float maxBoostSpeed;
        public float maxThrust;
        public float torqueThrustMultiplier;
        public float throttleMultiplier;
        public float latHMultiplier;
        public float latVMultiplier;
        public float pitchMultiplier;
        public float rollMultiplier;
        public float yawMultiplier;
        public float thrustBoostMultiplier;
        public float torqueBoostMultiplier;
        public float totalBoostTime;
        public float totalBoostRotationalTime;
        public float boostMaxSpeedDropOffTime;
        public float boostRechargeTime;
        public float boostCapacitorPercentCost;
        public float boostCapacityPercentChargeRate;
        public float minUserLimitedVelocity;

        public string ToJsonString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [CanBeNull]
        public static ShipParameters FromJsonString(string json) {
            try {
                return JsonConvert.DeserializeObject<ShipParameters>(json);
            }
            catch (Exception e) {
                Debug.LogWarning(e.Message);
                return null;
            }
        }
    }

    [RequireComponent(typeof(Transform))]
    [RequireComponent(typeof(Rigidbody))]
    public class ShipPlayer : NetworkBehaviour {
        
        [CanBeNull]
        public static ShipPlayer FindLocal =>
            Array.Find(FindObjectsOfType<ShipPlayer>(), shipPlayer => shipPlayer.isLocalPlayer);
        
        // TODO: remove this stuff once params are finalised (this is for debug panel in release)
        public static ShipParameters ShipParameterDefaults {
            get => new ShipParameters {
                mass = 1100f,
                drag = 0f,
                angularDrag = 0f,
                inertiaTensorMultiplier = 175f,
                maxSpeed = 800f,
                maxBoostSpeed = 932f,
                maxThrust = 110000f,
                torqueThrustMultiplier = 0.075f,
                throttleMultiplier = 1f,
                latHMultiplier = 0.5f,
                latVMultiplier = 0.7f,
                pitchMultiplier = 1f,
                rollMultiplier = 0.3f,
                yawMultiplier = 0.8f,
                thrustBoostMultiplier = 6.5f,
                torqueBoostMultiplier = 2f,
                totalBoostTime = 5f,
                totalBoostRotationalTime = 6f,
                boostMaxSpeedDropOffTime = 12f,
                boostRechargeTime = 4f,
                boostCapacitorPercentCost = 70f,
                boostCapacityPercentChargeRate = 10f,
                minUserLimitedVelocity = 250f,
            };
        }

        public ShipParameters Parameters {
            get {
                if (!_rigidbody) {
                    return ShipParameterDefaults;
                }

                var parameters = new ShipParameters();
                parameters.mass = Mathf.Round(_rigidbody.mass);
                parameters.drag = _rigidbody.drag;
                parameters.angularDrag = _rigidbody.angularDrag;
                parameters.inertiaTensorMultiplier = _inertialTensorMultiplier;
                parameters.maxSpeed = _maxSpeed;
                parameters.maxBoostSpeed = _maxBoostSpeed;
                parameters.maxThrust = _maxThrust;
                parameters.torqueThrustMultiplier = _torqueThrustMultiplier;
                parameters.throttleMultiplier = _throttleMultiplier;
                parameters.latHMultiplier = _latHMultiplier;
                parameters.latVMultiplier = _latVMultiplier;
                parameters.pitchMultiplier = _pitchMultiplier;
                parameters.rollMultiplier = _rollMultiplier;
                parameters.yawMultiplier = _yawMultiplier;
                parameters.thrustBoostMultiplier = _thrustBoostMultiplier;
                parameters.torqueBoostMultiplier = _torqueBoostMultiplier;
                parameters.totalBoostTime = _totalBoostTime;
                parameters.totalBoostRotationalTime = _totalBoostRotationalTime;
                parameters.boostMaxSpeedDropOffTime = _boostMaxSpeedDropOffTime;
                parameters.boostRechargeTime = _boostRechargeTime;
                parameters.boostCapacitorPercentCost = _boostCapacitorPercentCost;
                parameters.boostCapacityPercentChargeRate = _boostCapacityPercentChargeRate;
                parameters.minUserLimitedVelocity = _minUserLimitedVelocity;
                return parameters;
            }
            set {
                _rigidbody.mass = value.mass;
                _rigidbody.drag = value.drag;
                _rigidbody.angularDrag = value.angularDrag;
                _rigidbody.inertiaTensor = _initialInertiaTensor * value.inertiaTensorMultiplier;
                _inertialTensorMultiplier = value.inertiaTensorMultiplier;

                _maxSpeed = value.maxSpeed;
                _maxBoostSpeed = value.maxBoostSpeed;
                _maxThrust = value.maxThrust;
                _torqueThrustMultiplier = value.torqueThrustMultiplier;
                _throttleMultiplier = value.throttleMultiplier;
                _latHMultiplier = value.latHMultiplier;
                _latVMultiplier = value.latVMultiplier;
                _pitchMultiplier = value.pitchMultiplier;
                _rollMultiplier = value.rollMultiplier;
                _yawMultiplier = value.yawMultiplier;
                _thrustBoostMultiplier = value.thrustBoostMultiplier;
                _torqueBoostMultiplier = value.torqueBoostMultiplier;
                _totalBoostTime = value.totalBoostTime;
                _totalBoostRotationalTime = value.totalBoostRotationalTime;
                _boostMaxSpeedDropOffTime = value.boostMaxSpeedDropOffTime;
                _boostRechargeTime = value.boostRechargeTime;
                _boostCapacitorPercentCost = value.boostCapacitorPercentCost;
                _boostCapacityPercentChargeRate = value.boostCapacityPercentChargeRate;
                _minUserLimitedVelocity = value.minUserLimitedVelocity;
            }
        }

        [SerializeField] private GameObject playerLogic;
        [SerializeField] private Text velocityIndicator;
        [SerializeField] private Image accelerationBar;
        [SerializeField] private Text boostIndicator;
        [SerializeField] private Image boostCapacitorBar;
        [SerializeField] private Light shipLights;

        private float _maxSpeed = ShipParameterDefaults.maxSpeed;
        private float _maxBoostSpeed = ShipParameterDefaults.maxBoostSpeed;
        private float _maxThrust = ShipParameterDefaults.maxThrust;
        private float _torqueThrustMultiplier = ShipParameterDefaults.torqueThrustMultiplier;
        private float _throttleMultiplier = ShipParameterDefaults.throttleMultiplier;
        private float _latHMultiplier = ShipParameterDefaults.latHMultiplier;
        private float _latVMultiplier = ShipParameterDefaults.latVMultiplier;
        private float _pitchMultiplier = ShipParameterDefaults.pitchMultiplier;
        private float _rollMultiplier = ShipParameterDefaults.rollMultiplier;
        private float _yawMultiplier = ShipParameterDefaults.yawMultiplier;
        private float _thrustBoostMultiplier = ShipParameterDefaults.thrustBoostMultiplier;
        private float _torqueBoostMultiplier = ShipParameterDefaults.torqueBoostMultiplier;
        private float _totalBoostTime = ShipParameterDefaults.totalBoostTime;
        private float _totalBoostRotationalTime = ShipParameterDefaults.totalBoostRotationalTime;
        private float _boostMaxSpeedDropOffTime = ShipParameterDefaults.boostMaxSpeedDropOffTime;
        private float _boostRechargeTime = ShipParameterDefaults.boostRechargeTime;
        private float _boostCapacitorPercentCost = ShipParameterDefaults.boostCapacitorPercentCost;
        private float _boostCapacityPercentChargeRate = ShipParameterDefaults.boostCapacityPercentChargeRate;
        private float _inertialTensorMultiplier = ShipParameterDefaults.inertiaTensorMultiplier;
        private float _minUserLimitedVelocity = ShipParameterDefaults.minUserLimitedVelocity;

        private Vector3 _initialInertiaTensor;

        private bool _boostCharging;
        private bool _isBoosting;
        private float _currentBoostTime;
        private float _boostedMaxSpeedDelta;
        private float _boostCapacitorPercent = 100f;

        private float _prevVelocity;
        private bool _userVelocityLimit;
        private float _velocityLimitCap;
        private bool _flightAssistVectorControl;
        private bool _flightAssistRotationalDampening;

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

        [CanBeNull] private Coroutine _boostCoroutine;

        private Transform _transform;
        private Rigidbody _rigidbody;
        [SyncVar] private bool _serverReady;
        
        private bool IsReady => _transform && _rigidbody && _serverReady;
        public float Velocity => Mathf.Round(_rigidbody.velocity.magnitude);
        public User User => GetComponentInChildren<User>();
        
        // The position and rotation of the ship within the world, taking into account floating origin fix
        public Vector3 AbsoluteWorldPosition {
            get {
                Vector3 position = transform.position;
                // if floating origin fix is active, overwrite position with corrected world space
                if (FloatingOrigin.Instance.FocalTransform == transform) {
                    position = FloatingOrigin.Instance.FocalObjectPosition;
                }
                return position;
            }
            set {
                Vector3 position = value;
                // if floating origin fix is active, overwrite position with corrected world space
                if (FloatingOrigin.Instance.FocalTransform == transform) {
                    position -= FloatingOrigin.Instance.Origin;
                }
                transform.position = position;
            }
        }

        public void Awake() {
            _transform = transform;
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Start() {
            DontDestroyOnLoad(this);
            
            // rigidbody defaults
            _rigidbody.centerOfMass = Vector3.zero;
            _rigidbody.inertiaTensorRotation = Quaternion.identity;
            _rigidbody.mass = ShipParameterDefaults.mass;
            _rigidbody.drag = ShipParameterDefaults.drag;
            _rigidbody.angularDrag = ShipParameterDefaults.angularDrag;

            // setup angular momentum for collisions (higher multiplier = less spin)
            var inertiaTensor = _rigidbody.inertiaTensor;
            _initialInertiaTensor = inertiaTensor;
            inertiaTensor *= _inertialTensorMultiplier;
            _rigidbody.inertiaTensor = inertiaTensor;
            
            // rigidbody angular momentum constraints (non local clients)
            if (!isLocalPlayer) {
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }

        private void OnEnable() {
            // perform positional correction on non-local client player objects like anything else in the world
            FloatingOrigin.OnFloatingOriginCorrection += NonLocalPlayerPositionCorrection;
        }
    
        private void OnDisable() {
            FloatingOrigin.OnFloatingOriginCorrection -= NonLocalPlayerPositionCorrection;
        }

        public override void OnStartLocalPlayer() {
            // enable input, camera, effects etc
            playerLogic.SetActive(true);
            
            // register self as floating origin focus
            FloatingOrigin.Instance.FocalTransform = transform;

            switch (Preferences.Instance.GetString("flightAssistDefault")) {
                case "vector assist only":
                    _flightAssistVectorControl = true;
                    break;
                case "rotational assist only":
                    _flightAssistRotationalDampening = true;
                    break;
                case "all off":
                    _flightAssistVectorControl = false;
                    _flightAssistRotationalDampening = false;
                    break;
                default:
                    _flightAssistVectorControl = true;
                    _flightAssistRotationalDampening = true;
                    break;
            }
        }
        
        // called when the server has finished instantiating all players
        public void ServerReady() {
            _serverReady = true;
        }

        public void Reset() {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
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
            _boostCharging = false;
            _isBoosting = false;
            _boostCapacitorPercent = 100f;
            _prevVelocity = 0;
            var shipCamera = GetComponentInChildren<ShipCamera>();
            if (shipCamera) {
                shipCamera.Reset();
            }

            if (_boostCoroutine != null) {
                StopCoroutine(_boostCoroutine);
            }

            AudioManager.Instance.Stop("ship-boost");
        }
        
        private void OnTriggerEnter(Collider other) {
            var checkpoint = other.GetComponentInParent<Checkpoint>();
            if (checkpoint) {
                checkpoint.Hit();
            }
        }
        
        // Apply all physics updates in fixed intervals (WRITE)
        private void FixedUpdate() {
            // TODO: Ensure ship updates are still processed in multiplayer (e.g. sounds, thrusters)
            if (isLocalPlayer && IsReady) {
                CalculateBoost(out var maxThrustWithBoost, out var maxTorqueWithBoost, out var boostedMaxSpeedDelta);
                CalculateFlightForces(
                    maxThrustWithBoost,
                    maxTorqueWithBoost,
                    _maxSpeed + boostedMaxSpeedDelta,
                    out var thrust);

                ClampMaxSpeed(boostedMaxSpeedDelta);
                UpdateIndicators(thrust);

                // Send the current floating origin along with the new position and rotation to the server
                CmdSetPosition(FloatingOrigin.Instance.Origin, _transform.localPosition, _transform.rotation, _rigidbody.velocity);
            }
        }

        #region Input
        public void SetPitch(float value) {
            if (_flightAssistRotationalDampening) {
                _pitchTargetFactor = ClampInput(value);
            }
            else {
                _pitchInput = ClampInput(value);
            }
        }

        public void SetRoll(float value) {
            if (_flightAssistRotationalDampening) {
                _rollTargetFactor = ClampInput(value);
            }
            else {
                _rollInput = ClampInput(value);
            }
        }

        public void SetYaw(float value) {
            if (_flightAssistRotationalDampening) {
                _yawTargetFactor = ClampInput(value);
            }
            else {
                _yawInput = ClampInput(value);
            }
        }

        public void SetThrottle(float value) {
            if (_flightAssistVectorControl) {
                _throttleTargetFactor = ClampInput(value);
            }
            else {
                _throttleInput = ClampInput(value);
            }
        }

        public void SetLateralH(float value) {
            if (_flightAssistVectorControl) {
                _latHTargetFactor = ClampInput(value);
            }
            else {
                _latHInput = ClampInput(value);
            }
        }

        public void SetLateralV(float value) {
            if (_flightAssistVectorControl) {
                _latVTargetFactor = ClampInput(value);
            }
            else {
                _latVInput = ClampInput(value);
            }
        }

        public void Boost(bool isPressed) {
            var boost = isPressed;
            if (boost && !_boostCharging && _boostCapacitorPercent > _boostCapacitorPercentCost) {
                _boostCapacitorPercent -= _boostCapacitorPercentCost;
                _boostCharging = true;

                IEnumerator DoBoost() {
                    AudioManager.Instance.Play("ship-boost");
                    yield return new WaitForSeconds(1);
                    _currentBoostTime = 0f;
                    _boostedMaxSpeedDelta = _maxBoostSpeed - _maxSpeed;
                    _isBoosting = true;
                    yield return new WaitForSeconds(_boostRechargeTime);
                    _boostCharging = false;
                }

                _boostCoroutine = StartCoroutine(DoBoost());
            }
        }

        public void AllFlightAssistToggle() {
            // if any flight assist is enabled, deactivate (any on = all off)
            var isEnabled = !(_flightAssistVectorControl | _flightAssistRotationalDampening);

            // if user has all flight assists on by default, flip that logic on its head (any off = all on)
            if (Preferences.Instance.GetString("flightAssistDefault") == "all on") {
                isEnabled = !(_flightAssistVectorControl & _flightAssistRotationalDampening);
            }

            _flightAssistVectorControl = isEnabled;
            _flightAssistRotationalDampening = isEnabled;

            Debug.Log("All Flight Assists " + (isEnabled ? "ON" : "OFF"));

            // TODO: proper flight assist sounds
            if (isEnabled) {
                AudioManager.Instance.Play("ship-alternate-flight-on");
            }
            else {
                AudioManager.Instance.Play("ship-alternate-flight-off");
            }
        }

        public void FlightAssistVectorControlToggle() {
            _flightAssistVectorControl = !_flightAssistVectorControl;
            Debug.Log("Vector Control Flight Assist " + (_flightAssistVectorControl ? "ON" : "OFF"));

            // TODO: proper flight assist sounds
            if (_flightAssistVectorControl) {
                AudioManager.Instance.Play("ship-alternate-flight-on");
            }
            else {
                AudioManager.Instance.Play("ship-alternate-flight-off");
            }
        }

        public void FlightAssistRotationalDampeningToggle() {
            _flightAssistRotationalDampening = !_flightAssistRotationalDampening;
            Debug.Log("Rotational Dampening Flight Assist " + (_flightAssistRotationalDampening ? "ON" : "OFF"));

            // TODO: proper flight assist sounds
            if (_flightAssistRotationalDampening) {
                AudioManager.Instance.Play("ship-alternate-flight-on");
            }
            else {
                AudioManager.Instance.Play("ship-alternate-flight-off");
            }
        }

        public void ShipLightsToggle() {
            AudioManager.Instance.Play("ui-nav");
            shipLights.enabled = !shipLights.enabled;
        }

        public void VelocityLimiterIsPressed(bool isPressed) {
            _userVelocityLimit = isPressed;

            if (_userVelocityLimit) {
                AudioManager.Instance.Play("ship-velocity-limit-on");
            }
            else {
                AudioManager.Instance.Play("ship-velocity-limit-off");
            }
        }
        
        /**
         * All axis should be between -1 and 1. 
         */
        private float ClampInput(float input) {
            return Mathf.Min(Mathf.Max(input, -1), 1);
        }
        #endregion

        #region Flight Calculations

        private void CalculateBoost(
            out float maxThrustWithBoost, 
            out float maxTorqueWithBoost,
            out float boostedMaxSpeedDelta
        ) {
            _boostCapacitorPercent = Mathf.Min(100,
                _boostCapacitorPercent + _boostCapacityPercentChargeRate * Time.fixedDeltaTime);

            maxThrustWithBoost = _maxThrust;
            maxTorqueWithBoost = _maxThrust * _torqueThrustMultiplier;
            boostedMaxSpeedDelta = _boostedMaxSpeedDelta;

            _currentBoostTime += Time.fixedDeltaTime;

            // reduce boost potency over time period
            if (_isBoosting) {
                // Ease-in (boost drop-off is more dramatic)
                float t = _currentBoostTime / _totalBoostTime;
                float tBoost = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                float tTorque = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);

                maxThrustWithBoost *= Mathf.Lerp(_thrustBoostMultiplier, 1, tBoost);
                maxTorqueWithBoost *= Mathf.Lerp(_torqueBoostMultiplier, 1, tTorque);
            }

            // reduce max speed over time until we're back at 0
            if (_boostedMaxSpeedDelta > 0) {
                float t = _currentBoostTime / _boostMaxSpeedDropOffTime;
                // TODO: an actual curve rather than this ... idk what this is
                // clamp at 1 as it's being used as a multiplier and the first ~2 seconds are at max speed 
                float tBoostVelocityMax = Math.Min(1, 0.15f - (Mathf.Cos(t * Mathf.PI * 0.6f) * -1));
                boostedMaxSpeedDelta *= tBoostVelocityMax;

                if (tBoostVelocityMax < 0) {
                    _boostedMaxSpeedDelta = 0;
                }
            }

            if (_currentBoostTime > _totalBoostRotationalTime) {
                _isBoosting = false;
            }
        }

        private void CalculateFlightForces(float maxThrustWithBoost, float maxTorqueWithBoost, float maxSpeedWithBoost,
            out float calculatedThrust) {

            /* FLIGHT ASSISTS */
            if (_flightAssistVectorControl) {
                CalculateVectorControlFlightAssist(maxSpeedWithBoost);
            }

            if (_flightAssistRotationalDampening) {
                CalculateRotationalDampeningFlightAssist();
            }

            /* INPUTS */
            // special case for throttle - no reverse while boosting but, while always going forward, the ship will change
            // vector less harshly while holding back (up to 40%)
            var throttle = _isBoosting && _currentBoostTime < _totalBoostTime
                ? Math.Min(1f, _throttleInput + 1.6f)
                : _throttleInput;

            // Get the raw inputs multiplied by the ship params multipliers as a vector3.
            // All components are between -1 and 1.
            var thrustInput = new Vector3(
                _latHInput * _latHMultiplier,
                _latVInput * _latVMultiplier,
                throttle * _throttleMultiplier
            );

            /* THRUST */
            // standard thrust calculated per-axis (each axis has it's own max thrust component including boost)
            var baseThrust = thrustInput * _maxThrust;
            var thrust = thrustInput * maxThrustWithBoost;
            _rigidbody.AddForce(transform.TransformDirection(thrust));
            // output var for indicators
            calculatedThrust = Math.Abs(baseThrust.x) + Math.Abs(baseThrust.y) + Math.Abs(baseThrust.z);

            /* TORQUE */
            // torque is applied entirely independently, this may be looked at later.
            var torque = new Vector3(
                _pitchInput * _pitchMultiplier * maxTorqueWithBoost,
                _yawInput * _yawMultiplier * maxTorqueWithBoost,
                _rollInput * _rollMultiplier * maxTorqueWithBoost * -1
            ) * _inertialTensorMultiplier; // if we don't counteract the inertial tensor of the rigidbody, the rotation spin would increase in lockstep

            _rigidbody.AddTorque(transform.TransformDirection(torque));
        }

        private void CalculateVectorControlFlightAssist(float maxSpeedWithBoost) {
            // convert global rigid body velocity into local space
            Vector3 localVelocity = transform.InverseTransformDirection(_rigidbody.velocity);

            CalculateAssistedAxis(_latHTargetFactor, localVelocity.x, 0.1f, maxSpeedWithBoost, out _latHInput);
            CalculateAssistedAxis(_latVTargetFactor, localVelocity.y, 0.1f, maxSpeedWithBoost, out _latVInput);
            CalculateAssistedAxis(_throttleTargetFactor, localVelocity.z, 0.1f, maxSpeedWithBoost, out _throttleInput);

        }

        private void CalculateRotationalDampeningFlightAssist() {
            // convert global rigid body velocity into local space
            Vector3 localAngularVelocity = transform.InverseTransformDirection(_rigidbody.angularVelocity);

            CalculateAssistedAxis(_pitchTargetFactor, localAngularVelocity.x, 0.3f, 2.0f, out _pitchInput);
            CalculateAssistedAxis(_yawTargetFactor, localAngularVelocity.y, 0.3f, 2.0f, out _yawInput);
            CalculateAssistedAxis(_rollTargetFactor, localAngularVelocity.z * -1, 0.3f, 2.0f, out _rollInput);
        }

        /**
     * Given a target factor between 0 and 1 for a given axis, the current gross value and the maximum, calculate a
     * new axis value to apply as input.
     * @param targetFactor value between 0 and 1 (effectively the users' input)
     * @param currentAxisVelocity the non-normalised raw value of the current motion of the axis
     * @param interpolateAtPercent the point at which to begin linearly interpolating the acceleration
     *      (e.g. 0.1 = at 10% of the MAXIMUM velocity of the axis away from the target, interpolate the axis -
     *      if the current speed is 0, the target is 0.5 and this value is 0.1, this means that at 40% of the maximum
     *      speed -- when the axis is at 0.4 -- decrease the output linearly such that it moves from 1 to 0 and slowly
     *      decelerates.
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

        // TODO: clamping should be based on input rather than modifying the rigid body - if gravity pulls you down
        //   then that's fine, similar to if a collision yeets you into a spinning mess.
        private void ClampMaxSpeed(float boostedMaxSpeedDelta) {
            // clamp max speed if user is holding the velocity limiter button down
            if (_userVelocityLimit) {
                _velocityLimitCap = Math.Max(_prevVelocity, _minUserLimitedVelocity);
                _rigidbody.velocity = Vector3.ClampMagnitude(_rigidbody.velocity, _velocityLimitCap);
            }

            // clamp max speed in general including boost variance (max boost speed minus max speed)
            _rigidbody.velocity = Vector3.ClampMagnitude(_rigidbody.velocity, _maxSpeed + boostedMaxSpeedDelta);
            _prevVelocity = _rigidbody.velocity.magnitude;
        }
        #endregion
        
        #region Network Position Sync
        // This is server-side and should really validate the positions coming in before blindly firing to all the clients!
        [Command]
        void CmdSetPosition(Vector3 origin, Vector3 position, Quaternion rotation, Vector3 velocity) {
            RpcUpdate(origin, position, rotation, velocity);
        }
    
        // On each client, update the position of this object if it's not the local player.
        [ClientRpc]
        void RpcUpdate(Vector3 remoteOrigin, Vector3 position, Quaternion rotation, Vector3 velocity) {
            if (!isLocalPlayer && IsReady) {
                // Calculate the local difference to position based on the local clients' floating origin.
                // If these values are gigantic, the doesn't really matter as they only update at fixed distances.
                // We'll lose precision here but we add our position on top after-the-fact, so we always have
                // local-level precision.
                var offset = remoteOrigin - FloatingOrigin.Instance.Origin;
                var localPosition = offset + position;

                _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, velocity, 0.1f);
                _transform.localPosition = Vector3.Lerp(_transform.localPosition, localPosition, 0.5f);
                _transform.localRotation = Quaternion.Lerp(_transform.localRotation, rotation, 0.5f);
            
                // add velocity to position as position would have moved on server at that velocity
                transform.localPosition += velocity * Time.fixedDeltaTime;
            }
        }
        
        private void NonLocalPlayerPositionCorrection(Vector3 offset) {
            if (!isLocalPlayer) {
                transform.position -= offset;
            }
        }
        #endregion
        
        // TODO: Move to a ship object script (it'll need more for thrusters, sounds etc)
        private void UpdateIndicators(float thrust) {
            if (velocityIndicator != null) {
                velocityIndicator.text = Velocity.ToString(CultureInfo.InvariantCulture);
            }

            if (accelerationBar != null) {
                var acceleration = thrust / _maxThrust;
                accelerationBar.fillAmount = MathfExtensions.Remap(0, 1, 0, 0.755f, acceleration);
                accelerationBar.color = Color.Lerp(Color.green, Color.red, acceleration);
            }

            if (boostIndicator != null) {
                boostIndicator.text = ((int) _boostCapacitorPercent).ToString(CultureInfo.InvariantCulture) + "%";
            }

            if (boostCapacitorBar != null) {
                boostCapacitorBar.fillAmount = MathfExtensions.Remap(0, 100, 0, 0.775f, _boostCapacitorPercent);
                boostCapacitorBar.color = Color.Lerp(Color.red, Color.green, _boostCapacitorPercent / 100);
            }
        }
    }
}