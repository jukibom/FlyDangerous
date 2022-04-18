using System;
using System.Collections;
using Core.Player;
using JetBrains.Annotations;
using Misc;
using UnityEngine;

namespace Core.ShipModel {
    public class ShipPhysics : MonoBehaviour {
        public delegate void BoostFiredAction(float boostTime);

        public delegate void ShipPhysicsUpdated();

        // This magic number is the original inertiaTensor of the puffin ship which, unbeknownst to me at the time,
        // is actually calculated from the rigid body bounding boxes and impacts the torque rotation physics.
        // Therefore, to maintains consistency with the flight parameters model this will likely never change.
        // Good fun!
        private static readonly Vector3 initialInertiaTensor = new(5189.9f, 5825.6f, 1471.6f);

        [SerializeField] private Rigidbody targetRigidbody;

        public Optional<float> indicatorThrottleLocation;

        // ray-casting without per-frame allocation
        private readonly RaycastHit[] _raycastHits = new RaycastHit[50];
        private float _boostCapacitorPercent = 100f;
        private bool _boostCharging;

        [CanBeNull] private Coroutine _boostCoroutine;
        private float _boostedMaxSpeedDelta;
        private float _currentBoostTime;
        private float _gforce;
        private bool _isBoosting;

        private Vector3 _prevVelocity;
        private ShipIndicatorData _shipIndicatorData;

        [CanBeNull] private IShipModel _shipModel;

        private ShipParameters _shipParameters;
        private float _velocityLimitCap;

        public ShipParameters CurrentParameters {
            get {
                if (!targetRigidbody || _shipParameters == null) return ShipParameters.Defaults;
                return _shipParameters;
            }
            set {
                _shipParameters = value;

                // handle rigidbody params
                targetRigidbody.mass = value.mass;
                targetRigidbody.drag = value.drag;
                targetRigidbody.angularDrag = value.angularDrag;
                targetRigidbody.maxAngularVelocity = value.maxAngularVelocity;

                // setup angular momentum for collisions (higher multiplier = less spin)
                targetRigidbody.inertiaTensor = initialInertiaTensor * value.inertiaTensorMultiplier;
            }
        }

        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public Vector3 Velocity => targetRigidbody.velocity;
        public Vector3 AngularVelocity => targetRigidbody.angularVelocity;
        public float VelocityMagnitude => Mathf.Round(targetRigidbody.velocity.magnitude);
        public float VelocityNormalised => targetRigidbody.velocity.sqrMagnitude / (CurrentParameters.maxBoostSpeed * CurrentParameters.maxBoostSpeed);
        private bool BoostReady => !_boostCharging && _boostCapacitorPercent > CurrentParameters.boostCapacitorPercentCost;
        public ShipIndicatorData ShipIndicatorData => _shipIndicatorData;

        public Vector3 CurrentFrameThrust { get; private set; }
        public Vector3 CurrentFrameTorque { get; private set; }
        public float MaxThrustWithBoost { get; private set; }
        public float MaxTorqueWithBoost { get; private set; }
        public float BoostedMaxSpeedDelta { get; private set; }


        public float Pitch { get; private set; }
        public float Roll { get; private set; }
        public float Yaw { get; private set; }
        public float Throttle { get; private set; }
        public float LatH { get; private set; }
        public float LatV { get; private set; }
        public bool BoostButtonHeld { get; private set; }
        public bool VelocityLimitActive { get; private set; }
        public bool VectorFlightAssistActive { get; private set; }
        public bool RotationalFlightAssistActive { get; private set; }
        public bool IsShipLightsActive { get; private set; }


        [CanBeNull]
        public IShipModel ShipModel {
            get {
                // if no ship associated, try to grab one from the entity tree and initialise it
                if (_shipModel == null) {
                    _shipModel ??= GetComponentInChildren<PuffinShipModel>(true);
                    _shipModel ??= GetComponentInChildren<CalidrisShipModel>(true);

                    ShipModel = _shipModel;
                }

                return _shipModel;
            }
            private set {
                if (value != null) {
                    var prev = _shipModel;

                    // store and parent to main player transform
                    _shipModel = value;
                    var entity = _shipModel.Entity();
                    entity.transform.SetParent(transform, false);

                    // clean up existing ship
                    if (prev != value && prev != null) {
                        Debug.Log("Cleaning up existing ... " + prev);
                        Destroy(prev.Entity().gameObject);
                    }
                }
            }
        }

        public void Reset() {
            targetRigidbody.velocity = Vector3.zero;
            targetRigidbody.angularVelocity = Vector3.zero;
            _boostCharging = false;
            _isBoosting = false;
            _boostCapacitorPercent = 100f;
            _prevVelocity = Vector3.zero;

            if (_boostCoroutine != null) StopCoroutine(_boostCoroutine);
        }

        public void Start() {
            DontDestroyOnLoad(this);

            // rigidbody non-configurable defaults
            targetRigidbody.centerOfMass = Vector3.zero;
            targetRigidbody.inertiaTensorRotation = Quaternion.identity;

            // init physics
            CurrentParameters = ShipParameters.Defaults;
        }

        public event BoostFiredAction OnBoost;
        public event ShipPhysicsUpdated OnShipPhysicsUpdated;

        public void RefreshShipModel(ShipProfile shipProfile) {
            var shipData = ShipMeta.FromString(shipProfile.shipModel);
            // TODO: make this async
            var shipModel = Instantiate(Resources.Load(shipData.PrefabToLoad, typeof(GameObject)) as GameObject);

            void SetLayerMaskRecursive(int layer, GameObject targetGameObject) {
                targetGameObject.layer = layer;
                foreach (Transform child in targetGameObject.transform) SetLayerMaskRecursive(layer, child.gameObject);
            }

            SetLayerMaskRecursive(gameObject.layer, shipModel);

            var shipObject = shipModel.GetComponent<IShipModel>();
            shipObject.SetPrimaryColor(shipProfile.primaryColor);
            shipObject.SetAccentColor(shipProfile.accentColor);
            shipObject.SetThrusterColor(shipProfile.thrusterColor);
            shipObject.SetTrailColor(shipProfile.trailColor);
            shipObject.SetHeadLightsColor(shipProfile.headLightsColor);
            ShipModel = shipObject;
        }

        // standard OnTriggerEnter doesn't cut the mustard at these speeds so we need to do something a bit more precise
        public void CheckpointCollisionCheck() {
            var shipTransform = transform;

            var frameVelocity = targetRigidbody.velocity * Time.fixedDeltaTime;
            var start = shipTransform.position + frameVelocity * 2; // + 1 to include the current frame

            // Check for checkpoint collisions (layer mask 9) using an inverse velocity ray rather than the inbuilt box check
            // use the velocity * 4 to make damn sure we capture everything.
            // use a box cast to eliminate the problem of missing at the extreme edges.
            var raycastHitCount = Physics.BoxCastNonAlloc(start, new Vector3(5, frameVelocity.magnitude * 4, 3), frameVelocity.normalized,
                _raycastHits, Quaternion.identity, frameVelocity.magnitude, 1 << 7); // 7th layer is checkpoint collider

            // Debug.DrawRay(start, frameVelocity * 2 * -1, Color.red);

            for (var i = 0; i < raycastHitCount; i++) {
                var raycastHit = _raycastHits[i];
                var checkpoint = raycastHit.collider.GetComponentInParent<Checkpoint>();
                if (checkpoint) checkpoint.Hit();
            }
        }

        public void ShipLightsToggle(Action<bool> shipLightStatus) {
            IsShipLightsActive = !IsShipLightsActive;
            shipLightStatus(IsShipLightsActive);
        }

        public void Stop() {
            targetRigidbody.velocity = Vector3.zero;
            targetRigidbody.angularVelocity = Vector3.zero;
            Pitch = 0;
            Roll = 0;
            Yaw = 0;
            Throttle = 0;
            LatH = 0;
            LatV = 0;
            BoostButtonHeld = false;
            VelocityLimitActive = false;
            VectorFlightAssistActive = false;
            RotationalFlightAssistActive = false;
            CurrentFrameThrust = Vector3.zero;
            CurrentFrameTorque = Vector3.zero;
            UpdateMotionInformation(Vector3.zero, Vector3.zero, Vector3.zero);
            UpdateIndicators();
        }

        private void AttemptBoost() {
            if (BoostReady) {
                _boostCapacitorPercent -= CurrentParameters.boostCapacitorPercentCost;
                _boostCharging = true;

                IEnumerator DoBoost() {
                    OnBoost?.Invoke(CurrentParameters.totalBoostTime);
                    yield return YieldExtensions.WaitForFixedFrames(YieldExtensions.SecondsToFixedFrames(1));
                    _currentBoostTime = 0f;
                    _boostedMaxSpeedDelta = CurrentParameters.maxBoostSpeed - CurrentParameters.maxSpeed;
                    _isBoosting = true;
                    yield return YieldExtensions.WaitForFixedFrames(YieldExtensions.SecondsToFixedFrames(CurrentParameters.boostRechargeTime));
                    _boostCharging = false;
                }

                _boostCoroutine = StartCoroutine(DoBoost());
            }
        }

        // TODO: clamping should be based on input rather than modifying the rigid body - if gravity pulls you down
        //   then that's fine, similar to if a collision yeets you into a spinning mess.
        private void ClampMaxSpeed(bool velocityLimiterActive) {
            // clamp max speed if user is holding the velocity limiter button down
            if (velocityLimiterActive) {
                _velocityLimitCap = Math.Max(_prevVelocity.magnitude, CurrentParameters.minUserLimitedVelocity);
                targetRigidbody.velocity = Vector3.ClampMagnitude(targetRigidbody.velocity, _velocityLimitCap);
            }

            // clamp max speed in general including boost variance (max boost speed minus max speed)
            targetRigidbody.velocity = Vector3.ClampMagnitude(targetRigidbody.velocity, CurrentParameters.maxSpeed + BoostedMaxSpeedDelta);

            // calculate g-force 
            var currentVelocity = targetRigidbody.velocity;
            _gforce = Math.Abs((currentVelocity - _prevVelocity).magnitude / (Time.fixedDeltaTime * 9.8f));
            _prevVelocity = currentVelocity;
        }

        private void UpdateBoostStatus() {
            _boostCapacitorPercent = Mathf.Min(100,
                _boostCapacitorPercent + CurrentParameters.boostCapacityPercentChargeRate * Time.fixedDeltaTime);

            // copy defaults before modifying
            MaxThrustWithBoost = CurrentParameters.maxThrust;
            MaxTorqueWithBoost = CurrentParameters.maxThrust * CurrentParameters.torqueThrustMultiplier;
            BoostedMaxSpeedDelta = _boostedMaxSpeedDelta;

            _currentBoostTime += Time.fixedDeltaTime;

            // reduce boost potency over time period
            if (_isBoosting) {
                // Ease-in (boost drop-off is more dramatic)
                var t = _currentBoostTime / CurrentParameters.totalBoostTime;
                var tBoost = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                var tTorque = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);

                MaxThrustWithBoost *= Mathf.Lerp(CurrentParameters.thrustBoostMultiplier, 1, tBoost);
                MaxTorqueWithBoost *= Mathf.Lerp(CurrentParameters.torqueBoostMultiplier, 1, tTorque);
            }

            // reduce max speed over time until we're back at 0
            if (_boostedMaxSpeedDelta > 0) {
                var t = _currentBoostTime / CurrentParameters.boostMaxSpeedDropOffTime;
                // TODO: an actual curve rather than this ... idk what this is
                // clamp at 1 as it's being used as a multiplier and the first ~2 seconds are at max speed 
                var tBoostVelocityMax = Math.Min(1, 0.15f - Mathf.Cos(t * Mathf.PI * 0.6f) * -1);
                BoostedMaxSpeedDelta *= tBoostVelocityMax;

                if (tBoostVelocityMax < 0) _boostedMaxSpeedDelta = 0;
            }

            if (_currentBoostTime > CurrentParameters.totalBoostRotationalTime) _isBoosting = false;
        }

        public void UpdateShip(float pitch, float roll, float yaw, float throttle, float latH, float latV, bool boostButtonHeld, bool velocityLimitActive,
            bool vectorFlightAssistActive, bool rotationalFlightAssistActive) {
            Pitch = pitch;
            Roll = roll;
            Yaw = yaw;
            Throttle = throttle;
            LatH = latH;
            LatV = latV;
            BoostButtonHeld = boostButtonHeld;
            VelocityLimitActive = velocityLimitActive;
            VectorFlightAssistActive = vectorFlightAssistActive;
            RotationalFlightAssistActive = rotationalFlightAssistActive;

            OnShipPhysicsUpdated?.Invoke();

            if (boostButtonHeld) AttemptBoost();
            UpdateBoostStatus();
            ApplyFlightForces();
            UpdateIndicators();
            UpdateMotionInformation(Velocity, CurrentFrameThrust, CurrentFrameTorque);
        }

        public void UpdateMotionInformation(Vector3 velocity, Vector3 thrust, Vector3 torque) {
            var torqueNormalised = torque / (CurrentParameters.maxThrust * CurrentParameters.torqueThrustMultiplier);
            var torqueVec = new Vector3(
                torqueNormalised.x,
                MathfExtensions.Remap(-0.8f, 0.8f, -1, 1, torqueNormalised.y),
                MathfExtensions.Remap(-0.3f, 0.3f, -1, 1, torqueNormalised.z)
            );
            ShipModel?.UpdateMotionInformation(velocity, CurrentParameters.maxBoostSpeed, thrust / CurrentParameters.maxThrust, torqueVec);
        }


        private void UpdateIndicators() {
            _shipIndicatorData.throttlePosition = indicatorThrottleLocation.Enabled ? indicatorThrottleLocation.Value : Throttle;

            _shipIndicatorData.acceleration = (Math.Abs(CurrentFrameThrust.x) + Math.Abs(CurrentFrameThrust.y) + Math.Abs(CurrentFrameThrust.z)) /
                                              CurrentParameters.maxThrust;
            _shipIndicatorData.velocity = VelocityMagnitude;
            _shipIndicatorData.throttle = Throttle;
            _shipIndicatorData.boostCapacitorPercent = _boostCapacitorPercent;
            _shipIndicatorData.boostTimerReady = !_boostCharging;
            _shipIndicatorData.boostChargeReady = _boostCapacitorPercent > CurrentParameters.boostCapacitorPercentCost;
            _shipIndicatorData.lightsActive = IsShipLightsActive;
            _shipIndicatorData.velocityLimiterActive = VelocityLimitActive;
            _shipIndicatorData.vectorFlightAssistActive = VectorFlightAssistActive;
            _shipIndicatorData.rotationalFlightAssistActive = RotationalFlightAssistActive;
            _shipIndicatorData.gForce = _gforce;

            ShipModel?.UpdateIndicators(_shipIndicatorData);
        }

        private void ApplyFlightForces() {
            /* GRAVITY */
            var gravity = new Vector3(
                Game.Instance.LoadedLevelData.gravity.x,
                Game.Instance.LoadedLevelData.gravity.y,
                Game.Instance.LoadedLevelData.gravity.z
            );
            targetRigidbody.AddForce(targetRigidbody.mass * gravity);

            /* INPUTS */

            var latH = LatH;
            var latV = LatV;
            var throttle = Throttle;

            // special case for throttle - no reverse while boosting but, while always going forward, the ship will change
            // vector less harshly while holding back (up to 40%). The whole reverse axis is remapped to 40% for this calculation.
            // any additional throttle thrust not used in boost to be distributed across laterals
            if (_isBoosting && _currentBoostTime < CurrentParameters.totalBoostTime) {
                throttle = Mathf.Min(1f, MathfExtensions.Remap(-1, 0, 0.6f, 1, Throttle));

                var delta = 1f - throttle;
                if (delta > 0) {
                    var latInputTotal = Mathf.Abs(latH) + Mathf.Abs(latV);
                    if (latInputTotal > 0) {
                        var latHComponent = Mathf.Abs(latH) / latInputTotal;
                        var latVComponent = Mathf.Abs(latV) / latInputTotal;
                        latH *= 1 + delta * latHComponent;
                        latV *= 1 + delta * latVComponent;
                    }
                }
            }

            // Get the raw inputs multiplied by the ship params multipliers as a vector3.
            // All components are between -1 and 1.
            var thrustInput = new Vector3(
                latH * CurrentParameters.latHMultiplier,
                latV * CurrentParameters.latVMultiplier,
                throttle * CurrentParameters.throttleMultiplier
            );

            /* THRUST */
            // standard thrust calculated per-axis (each axis has it's own max thrust component including boost)
            var thrust = thrustInput * MaxThrustWithBoost;
            targetRigidbody.AddForce(transform.TransformDirection(thrust));

            /* TORQUE */
            // torque is applied entirely independently, this may be looked at later.
            var torque = new Vector3(
                Pitch * CurrentParameters.pitchMultiplier * MaxTorqueWithBoost * -1,
                Yaw * CurrentParameters.yawMultiplier * MaxTorqueWithBoost,
                Roll * CurrentParameters.rollMultiplier * MaxTorqueWithBoost * -1
            ) * CurrentParameters
                .inertiaTensorMultiplier; // if we don't counteract the inertial tensor of the rigidbody, the rotation spin would increase in lockstep

            targetRigidbody.AddTorque(transform.TransformDirection(torque));

            ClampMaxSpeed(VelocityLimitActive);

            // output var for indicators etc
            CurrentFrameThrust = thrustInput * CurrentParameters.maxThrust;
            CurrentFrameTorque = torque / CurrentParameters.inertiaTensorMultiplier;
        }
    }
}