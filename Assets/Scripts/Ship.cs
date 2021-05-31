using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Audio;
using Engine;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;

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
    public float minUserLimitedVelocity;

    public string ToJsonString() {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    [CanBeNull]
    public static ShipParameters FromJsonString(string json) {
        try {
            return JsonConvert.DeserializeObject<ShipParameters>(json);
        }
        catch (Exception e){
            Debug.LogWarning(e.Message);
            return null;
        }
    }
}

[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(Rigidbody))]
public class Ship : MonoBehaviour {
    
    // TODO: remove this stuff once params are finalised (this is for debug panel in release)
    public static ShipParameters ShipParameterDefaults {
        get => new ShipParameters {
            mass = 550f,
            drag = 0f,
            angularDrag = 0f,
            inertiaTensorMultiplier = 125f,
            maxSpeed = 800f,
            maxBoostSpeed = 932f,
            maxThrust = 120000f,
            torqueThrustMultiplier = 0.1f,
            throttleMultiplier = 1f,
            latHMultiplier = 0.7f,
            latVMultiplier = 0.9f,
            pitchMultiplier = 1f,
            rollMultiplier = 0.3f,
            yawMultiplier = 0.8f,
            thrustBoostMultiplier = 5f,
            torqueBoostMultiplier = 2f,
            totalBoostTime = 6f,
            totalBoostRotationalTime = 7f,
            boostMaxSpeedDropOffTime = 12f,
            boostRechargeTime = 4f,
            minUserLimitedVelocity = 250f,
        };
    }
    public ShipParameters Parameters {
        get {
            if (!_rigidBody) {
                return ShipParameterDefaults; 
            }
            var parameters = new ShipParameters();
            parameters.mass = Mathf.Round(_rigidBody.mass);
            parameters.drag = _rigidBody.drag;
            parameters.angularDrag = _rigidBody.angularDrag;
            parameters.inertiaTensorMultiplier = inertialTensorMultiplier;
            parameters.maxSpeed = maxSpeed; 
            parameters.maxBoostSpeed = maxBoostSpeed;
            parameters.maxThrust = maxThrust;
            parameters.torqueThrustMultiplier = torqueThrustMultiplier;
            parameters.throttleMultiplier = throttleMultiplier;
            parameters.latHMultiplier = latHMultiplier;
            parameters.latVMultiplier = latVMultiplier;
            parameters.pitchMultiplier = pitchMultiplier;
            parameters.rollMultiplier = rollMultiplier;
            parameters.yawMultiplier = yawMultiplier;
            parameters.thrustBoostMultiplier = thrustBoostMultiplier;
            parameters.torqueBoostMultiplier = torqueBoostMultiplier;
            parameters.totalBoostTime = totalBoostTime;
            parameters.totalBoostRotationalTime = totalBoostRotationalTime;
            parameters.boostMaxSpeedDropOffTime = boostMaxSpeedDropOffTime;
            parameters.boostRechargeTime = boostRechargeTime;
            parameters.minUserLimitedVelocity = minUserLimitedVelocity;
            return parameters;
        }
        set {
            _rigidBody.mass = value.mass;
            _rigidBody.drag = value.drag;
            _rigidBody.angularDrag = value.angularDrag;
            _rigidBody.inertiaTensor = _initialInertiaTensor * value.inertiaTensorMultiplier;
            inertialTensorMultiplier = value.inertiaTensorMultiplier;
            
            maxSpeed = value.maxSpeed;
            maxBoostSpeed = value.maxBoostSpeed;
            maxThrust = value.maxThrust;
            torqueThrustMultiplier = value.torqueThrustMultiplier;
            throttleMultiplier = value.throttleMultiplier;
            latHMultiplier = value.latHMultiplier;
            latVMultiplier = value.latVMultiplier;
            pitchMultiplier = value.pitchMultiplier;
            rollMultiplier = value.rollMultiplier;
            yawMultiplier = value.yawMultiplier;
            thrustBoostMultiplier = value.thrustBoostMultiplier;
            torqueBoostMultiplier = value.torqueBoostMultiplier;
            totalBoostTime = value.totalBoostTime;
            totalBoostRotationalTime = value.totalBoostRotationalTime;
            boostMaxSpeedDropOffTime = value.boostMaxSpeedDropOffTime;
            boostRechargeTime = value.boostRechargeTime;
            minUserLimitedVelocity = value.minUserLimitedVelocity;
        }
    }
    
    [SerializeField] private Text velocityIndicator;
    [SerializeField] private Light shipLights;
    
    // TODO: split this into various thruster powers
    [SerializeField] private float maxSpeed = 800;
    [SerializeField] private float maxBoostSpeed = 932;
    [SerializeField] private float maxThrust = 120000;
    [SerializeField] private float torqueThrustMultiplier = 0.1f;
    [SerializeField] private float throttleMultiplier = 1f;
    [SerializeField] private float latHMultiplier = 0.7f;
    [SerializeField] private float latVMultiplier = 0.9f;
    [SerializeField] private float pitchMultiplier = 1f;
    [SerializeField] private float rollMultiplier = 0.3f;
    [SerializeField] private float yawMultiplier = 0.8f;
    [SerializeField] private float thrustBoostMultiplier = 5;
    [SerializeField] private float torqueBoostMultiplier = 2f;
    [SerializeField] private float totalBoostTime = 6f;
    [SerializeField] private float totalBoostRotationalTime = 7f;
    [SerializeField] private float boostMaxSpeedDropOffTime = 12f;
    [SerializeField] private float boostRechargeTime = 4f;
    [SerializeField] private float inertialTensorMultiplier = 125f;
    [SerializeField] private float minUserLimitedVelocity = 250f;

    private Vector3 _initialInertiaTensor;

    private bool _boostCharging;
    private bool _isBoosting;
    private float _currentBoostTime;
    private float _boostedMaxSpeedDelta;

    private float _prevVelocity;
    private bool _userVelocityLimit;
    private float _velocityLimitCap;
    private bool _flightAssistVectorControl;
    private bool _flightAssistRotationalDampening;

    // input axes -1 to 1
    private float _throttle;
    private float _latV;
    private float _latH;
    private float _pitch;
    private float _yaw;
    private float _roll;
    
    // flight assist targets
    private float _throttleTargetFactor;
    private float _latHTargetFactor;
    private float _latVTargetFactor;
    private float _pitchTargetFactor;
    private float _rollTargetFactor;
    private float _yawTargetFactor;

    [CanBeNull] private Coroutine _boostCoroutine;

    private Transform _transformComponent;
    private Rigidbody _rigidBody;
    
    public float Velocity {
        get {
            return Mathf.Round(_rigidBody.velocity.magnitude);
        }
    }

    public void Awake() {
        _transformComponent = GetComponent<Transform>();
        _rigidBody = GetComponent<Rigidbody>();
    }

    public void Start() {
        _flightAssistVectorControl = Preferences.Instance.GetBool("flightAssistOnByDefault");
        _flightAssistRotationalDampening = Preferences.Instance.GetBool("flightAssistOnByDefault");
        _rigidBody.centerOfMass = Vector3.zero;
        _rigidBody.inertiaTensorRotation = Quaternion.identity;

        // setup angular momentum for collisions (higher multiplier = less spin)
        _initialInertiaTensor = _rigidBody.inertiaTensor;
        _rigidBody.inertiaTensor *= inertialTensorMultiplier;
    }

    public void Reset() {
        _rigidBody.velocity = Vector3.zero;
        _rigidBody.angularVelocity = Vector3.zero;
        _pitch = 0;
        _roll = 0;
        _yaw = 0;
        _throttle = 0;
        _latH = 0;
        _latV = 0;
        _throttleTargetFactor = 0;
        _latHTargetFactor = 0;
        _latVTargetFactor = 0;
        _pitchTargetFactor = 0;
        _rollTargetFactor = 0;
        _yawTargetFactor = 0;
        _boostCharging = false;
        _isBoosting = false;
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

    public void SetPitch(float value) {
        if (_flightAssistRotationalDampening) {
            _pitchTargetFactor = ClampInput(value);
        }
        else {
            _pitch = ClampInput(value);
        }
    }

    public void SetRoll(float value) {
        if (_flightAssistRotationalDampening) {
            _rollTargetFactor = ClampInput(value);
        }
        else {
            _roll = ClampInput(value);
        }
    }

    public void SetYaw(float value) {
        if (_flightAssistRotationalDampening) {
            _yawTargetFactor = ClampInput(value);
        }
        else {
            _yaw = ClampInput(value);
        }
    }

    public void SetThrottle(float value) {
        if (_flightAssistVectorControl) {
            _throttleTargetFactor = ClampInput(value);
        }
        else {
            _throttle = ClampInput(value);
        }
    }
    
    public void SetLateralH(float value) {
        if (_flightAssistVectorControl) {
            _latHTargetFactor = ClampInput(value);
        }
        else {
            _latH = ClampInput(value);
        }
    }
    
    public void SetLateralV(float value) {
        if (_flightAssistVectorControl) {
            _latVTargetFactor = ClampInput(value);
        }
        else {
            _latV = ClampInput(value);
        }
    }

    public void Boost(bool isPressed) {
        var boost = isPressed;
        if (boost && !_boostCharging) {
            _boostCharging = true;

            IEnumerator DoBoost() {
                AudioManager.Instance.Play("ship-boost");
                yield return new WaitForSeconds(1);
                _currentBoostTime = 0f;
                _boostedMaxSpeedDelta = maxBoostSpeed - maxSpeed;
                _isBoosting = true;
                yield return new WaitForSeconds(boostRechargeTime);
                _boostCharging = false;
            }
            _boostCoroutine = StartCoroutine(DoBoost());
        }
    }

    public void AllFlightAssistToggle() {
        // if any flight assist is enabled, deactivate (any on = all off)
        var isEnabled = !(_flightAssistVectorControl | _flightAssistRotationalDampening);
        
        // if user has flight assists on by default, flip that logic on its head (any off = all on)
        if (Preferences.Instance.GetBool("flightAssistOnByDefault")) {
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
    
    // Get the position and rotation of the ship within the world, taking into account floating origin fix
    public void AbsoluteWorldPosition(out Vector3 position, out Quaternion rotation) {
        var t = transform; 
        var p = t.position; 
        var r = t.rotation.eulerAngles;
        position.x = p.x;
        position.y = p.y;
        position.z = p.z;
        rotation = Quaternion.Euler(r.x, r.y, r.z);

        // if floating origin fix is active, overwrite position with corrected world space
        var floatingOrigin = FindObjectOfType<FloatingOrigin>();
        if (floatingOrigin) {
            var origin = floatingOrigin.FocalObjectPosition;
            position.x = origin.x;
            position.y = origin.y;
            position.z = origin.z;
        }
    }

    private void OnTriggerEnter(Collider other) {
        var checkpoint = other.GetComponentInParent<Checkpoint>();
        if (checkpoint) {
            checkpoint.Hit();
        }
    }

    // Apply all physics updates in fixed intervals (WRITE)
    private void FixedUpdate() {
        CalculateBoost(out var maxThrustWithBoost, out var maxTorqueWithBoost, out var boostedMaxSpeedDelta);
        CalculateFlightForces(maxThrustWithBoost, maxTorqueWithBoost);
        
        // TODO: clamping should be based on input rather than modifying the rigid body - if gravity pulls you down then that's fine, similar to if a collision yeets you into a spinning mess.
        ClampMaxSpeed(boostedMaxSpeedDelta);
        UpdateIndicators();
    }

    private void UpdateIndicators() {
        if (velocityIndicator != null) {
            velocityIndicator.text = Velocity.ToString(CultureInfo.InvariantCulture);
        }
    }
    
    /**
     * All axis should be between -1 and 1. 
     */
    private float ClampInput(float input) {
        return Mathf.Min(Mathf.Max(input, -1), 1);
    }

    private void CalculateBoost(out float maxThrustWithBoost, out float maxTorqueWithBoost, out float boostedMaxSpeedDelta) {
        maxThrustWithBoost = maxThrust;
        maxTorqueWithBoost = maxThrust * torqueThrustMultiplier;
        boostedMaxSpeedDelta = _boostedMaxSpeedDelta;
        
        _currentBoostTime += Time.fixedDeltaTime;

        // reduce boost potency over time period
        if (_isBoosting) {
            // Ease-in (boost dropoff is more dramatic)
            float t = _currentBoostTime / totalBoostTime;
            float tBoost = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
            float tTorque = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);

            maxThrustWithBoost *= Mathf.Lerp(thrustBoostMultiplier, 1, tBoost);
            maxTorqueWithBoost *= Mathf.Lerp(torqueBoostMultiplier, 1, tTorque);
        }

        // reduce max speed over time until we're back at 0
        if (_boostedMaxSpeedDelta > 0) {
            float t = _currentBoostTime / boostMaxSpeedDropOffTime;
            // TODO: an actual curve rather than this ... idk what this is
            // clamp at 1 as it's being used as a multiplier and the first ~2 seconds are at max speed 
            float tBoostVelocityMax =  Math.Min(1, 0.15f - (Mathf.Cos(t * Mathf.PI * 0.6f) * -1));
            boostedMaxSpeedDelta *= tBoostVelocityMax;
            
            if (tBoostVelocityMax < 0) {
                _boostedMaxSpeedDelta = 0;
            }
        }
        
        if (_currentBoostTime > totalBoostRotationalTime) {
            _isBoosting = false;
        }
    }

    private void CalculateFlightForces(float maxThrustWithBoost, float maxTorqueWithBoost) {
        if (_flightAssistVectorControl) {
            CalculateVectorControlFlightAssist();
        }

        if (_flightAssistRotationalDampening) {
            CalculateRotationalDampeningFlightAssist();
        }
        
        // special case for throttle - no reverse while boosting but, while always going forward, the ship will change vector less harshly while holding back
        var throttle = _isBoosting && _currentBoostTime < totalBoostTime
            ? Math.Max(1f, _throttle + 1.6f)
            : _throttle;

        // When applying the actual thrust to the rigidbody, we only have a limited amount of thrust (maxThrustWithBoost).
        // To avoid applying max thrust in multiple directions (e.g. faster than a single direction), we need to combine
        // all our directions and divide our result thrust vector by the base input total requested.
        // e.g. if two axes are held down fully then our request is 2 (ignoring multipliers). Therefore, both axes
        // will receive 0.5x their respective thrust. 
        var thrustInput = new Vector3(
            _latH * latHMultiplier,
            _latV * latVMultiplier,
            throttle * throttleMultiplier
        );
        
        // Sum the total absolute requested thrust to divide each axis with.
        // Clamp this to 1 as a lower bound otherwise small forces are amplified (divide by fraction). We only care
        // if the combined axes are greater than 1.
        var totalRequestedThrustInput = Math.Max(1f, Math.Abs(thrustInput.x) + Math.Abs(thrustInput.y) + Math.Abs(thrustInput.z));

        // final thrust calculated from raw input * available thrust and divided by the total requested from three axes
        var thrust = (thrustInput * maxThrustWithBoost) / totalRequestedThrustInput ;
        
        var torque = new Vector3(
            _pitch * pitchMultiplier * maxTorqueWithBoost,
            _yaw * yawMultiplier * maxTorqueWithBoost,
            _roll * rollMultiplier * maxTorqueWithBoost * -1
        ) * inertialTensorMultiplier;   // if we don't counteract the inertial tensor of the rigidbody, the rotation spin would increase in lockstep
        
        _rigidBody.AddForce(transform.TransformDirection(thrust));
        _rigidBody.AddTorque(transform.TransformDirection(torque));
    }

    private void CalculateVectorControlFlightAssist() {
        // convert global rigid body velocity into local space
        Vector3 localVelocity = transform.InverseTransformDirection(_rigidBody.velocity);

        CalculateAssistedAxis(_latHTargetFactor, localVelocity.x, 0.1f, maxSpeed, out _latH);
        CalculateAssistedAxis(_latVTargetFactor, localVelocity.y, 0.1f, maxSpeed, out _latV);
        CalculateAssistedAxis(_throttleTargetFactor, localVelocity.z, 0.1f, maxSpeed, out _throttle);

    }
    
    private void CalculateRotationalDampeningFlightAssist() {
        // convert global rigid body velocity into local space
        Vector3 localAngularVelocity = transform.InverseTransformDirection(_rigidBody.angularVelocity);

        CalculateAssistedAxis(_pitchTargetFactor, localAngularVelocity.x, 0.3f, 2, out _pitch);
        CalculateAssistedAxis(_yawTargetFactor, localAngularVelocity.y, 0.3f, 1.5f, out _yaw);
        CalculateAssistedAxis(_rollTargetFactor, localAngularVelocity.z * -1, 0.3f, 1, out _roll);
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

        // prevent tiny noticeable movement on restart (floating point comparison, really only ever true when both are zero) 
        if (currentAxisVelocity == targetRate) {
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

    private void ClampMaxSpeed(float boostedMaxSpeedDelta) {
        // clamp max speed if user is holding the velocity limiter button down
        if (_userVelocityLimit) {
            _velocityLimitCap = Math.Max(_prevVelocity, minUserLimitedVelocity);
            _rigidBody.velocity = Vector3.ClampMagnitude(_rigidBody.velocity, _velocityLimitCap);
        }

        // clamp max speed in general including boost variance (max boost speed minus max speed)
        _rigidBody.velocity = Vector3.ClampMagnitude(_rigidBody.velocity, maxSpeed + boostedMaxSpeedDelta);
        _prevVelocity = _rigidBody.velocity.magnitude;
    }
}
