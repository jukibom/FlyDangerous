using System;
using Core.Player;
using Core.ShipModel.Feedback.interfaces;
using FdUI;
using JetBrains.Annotations;
using Misc;
using NaughtyAttributes;
using Ogs.Telemetry;
using UnityEngine;
using CoreVector3 = System.Numerics.Vector3;
using CoreQuaternion = System.Numerics.Quaternion;

namespace Core.ShipModel.Feedback.ogs {
    /**
     * OGS Telemetry Standard (OGS-TS) broadcaster - https://github.com/Open-Game-Standards/OGS-Telemetry-Standard
     *
     * Sends the ship pose, body dynamics and a self-describing channel set over UDP multicast
     * (default group 239.255.17.17:40123, TTL 1 - local subnet only) as an OMT1 envelope carrying
     * canonical Cap'n Proto Discovery/Motion/Event frames, so any number of OGS-TS receivers
     * (motion platforms, haptics, dashboards, loggers) can consume the stream at once.
     *
     * Lives under the Integrations object like the raw UDP socket / Sim Racing Studio outputs and is fed
     * by the FeedbackEngine the same way (IShipMotion / IShipInstruments / IShipFeedback).
     *
     * Enabled via preferences: "ogsTelemetryEnabled" (options menu toggle), endpoint configurable in the
     * config file via "ogsTelemetryMulticastAddress" / "ogsTelemetryPort".
     *
     * The sender core comes from the com.ogs.telemetry.unity package (MIT).
     *
     * Pause (standard CONVENTIONS C-11): the single-player pause sets Time.timeScale = 0, which
     * stops FixedUpdate, so frames are published from LateUpdate on real time. The stream keeps
     * flowing through a pause with session.paused set, and receivers bring rigs to rest instead
     * of freezing mid-manoeuvre or dropping the session.
     */
    public class OgsTelemetryServer : Singleton<OgsTelemetryServer>, IShipMotion, IShipInstruments, IShipFeedback {
        private const float AccelFilterTau = 0.1f;
        private const double MaxSampleGapSeconds = 0.1;

        // FixedUpdate runs at 50 Hz (Fixed Timestep 0.02) which is also the data cadence the
        // FeedbackEngine pushes at, so the publisher is capped to match (it publishes from
        // LateUpdate and rate-limits itself).
        private const double MotionHz = 50.0;

        private TelemetryPublisher _publisher;
        private TelemetrySender _sender;

        private bool _isEnabled;
        private string _multicastAddress = "239.255.17.17";
        private int _multicastPort = 40123;

        // latest ship data (stable class instances, refreshed in place by the FeedbackEngine)
        [CanBeNull] private IShipMotionData _motionData;
        [CanBeNull] private IShipInstrumentData _instrumentData;

        // derived body dynamics, fed to the publisher via BindBodyDynamics
        private CoreVector3 _specificForce;
        private CoreVector3 _angularVelocityC4;

        // specific force derivation state (acceleration low-pass, per BodyDynamicsDeriver semantics)
        private Vector3 _filteredAccel;
        private Vector3 _prevWorldVelocity;
        private double _lastSampleTime = double.NaN;

        [CanBeNull] private string _lastTypeLabel;

        private bool ReadyToPublish =>
            _isEnabled && _publisher != null &&
            Game.Instance.InGame && Game.Instance.GameModeHandler.HasStarted &&
            _motionData != null && _instrumentData != null;

        // body dynamics sample at the physics rate, where the FeedbackEngine refreshes the ship data
        private void FixedUpdate() {
            if (ReadyToPublish) UpdateBodyDynamics();
        }

        private void LateUpdate() {
            if (!ReadyToPublish) return;

            // surface the actual ship being tracked in the (stable) object's label - a ship change
            // re-sends discovery per the standard's re-send-on-change rule
            var shipName = FdPlayer.FindLocalShipPlayer?.ShipPhysics.ShipProfile?.shipModel;
            if (!string.IsNullOrEmpty(shipName) && shipName != _lastTypeLabel) {
                _lastTypeLabel = shipName;
                _publisher.SetObjectTypeLabel(shipName);
            }

            // ambient environment per level (space: vacuum, no gravity) — the
            // publisher early-outs when unchanged and re-sends discovery on change
            var terrain = Game.Instance.IsTerrainMap;
            _publisher.SetEnvironment(
                _instrumentData.Gravity,
                terrain ? 1.225f : 0f,
                20f,
                terrain ? 101325f : 0f, // Pa
                terrain ? Ogs.Telemetry.Schema.ContactMedium.Air : Ogs.Telemetry.Schema.ContactMedium.Vacuum);

            var paused = Time.timeScale == 0f;
            if (paused != _publisher.Paused) _publisher.SetPaused(paused);
            _publisher.Tick(Time.realtimeSinceStartupAsDouble);
        }

        /**
         * Derive accelerometer-style specific force and C-4 body angular velocity from the engine's
         * own measurements. Fly Dangerous flies far faster than the vendored BodyDynamicsDeriver's
         * teleport threshold (>2.5 m per 20 ms sample), so specific force is derived here from the
         * rigidbody world velocity (precise at any world scale) instead of pose deltas, with the same
         * low-pass filter and gravity-reaction semantics (specificForce = q⁻¹·(a + g), +9.8 heave at rest).
         */
        private void UpdateBodyDynamics() {
            var orientation = Quaternion.Euler(_motionData.WorldRotationEuler);
            var worldVelocity = _motionData.CurrentLateralVelocity;
            var now = Time.fixedTimeAsDouble;
            var gravity = _instrumentData.Gravity;

            if (double.IsNaN(_lastSampleTime) || now - _lastSampleTime > MaxSampleGapSeconds ||
                !_instrumentData.ShipActive) {
                // estimator reset (first sample / pause gap / ship change): hold static gravity reaction
                _filteredAccel = Vector3.zero;
                _prevWorldVelocity = worldVelocity;
                _lastSampleTime = now;
                _specificForce = ToCore(Quaternion.Inverse(orientation) * new Vector3(0f, gravity, 0f));
                _angularVelocityC4 = CoreVector3.Zero;
                return;
            }

            var dt = (float)(now - _lastSampleTime);
            var accel = (worldVelocity - _prevWorldVelocity) / dt;
            _filteredAccel += (accel - _filteredAccel) * Mathf.Min(1f, dt / AccelFilterTau);
            _specificForce = ToCore(Quaternion.Inverse(orientation) * (_filteredAccel + new Vector3(0f, gravity, 0f)));

            // body angular velocity from the rigidbody's native world rate. Unity's stored rotation
            // components map onto the standard's C-4 sense as (-x, +y, -z) - see the vendored
            // BodyDynamicsDeriver doc for the derivation.
            var localAngularVelocity = Quaternion.Inverse(orientation) * _motionData.CurrentAngularVelocity;
            _angularVelocityC4 = new CoreVector3(
                -localAngularVelocity.x,
                localAngularVelocity.y,
                -localAngularVelocity.z);

            _prevWorldVelocity = worldVelocity;
            _lastSampleTime = now;
        }

        public void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData) {
            if (!_isEnabled) return;
            _instrumentData = shipInstrumentData;
        }

        public void OnShipMotionUpdate(IShipMotionData shipMotionData) {
            if (!_isEnabled) return;
            _motionData = shipMotionData;
        }

        public void OnShipFeedbackUpdate(IShipFeedbackData shipFeedbackData) {
            if (!_isEnabled) return;

            // feedback-driven channels cached for the publisher's pull bindings
            _shipShakeNormalised = shipFeedbackData.ShipShakeNormalised;
            _boostThrustProgress = shipFeedbackData.BoostThrustProgressNormalised;

            if (_publisher == null) return;

            // discrete stimuli as EventFrames (short replay + id dedup handled by the publisher)
            if (shipFeedbackData.CollisionStartedThisFrame)
                _publisher.FireEvent(
                    "body.impact",
                    shipFeedbackData.CollisionImpactNormalised,
                    0.1f,
                    shipFeedbackData.CollisionDirection.ToNumerics());

            if (shipFeedbackData.BoostSpoolStartThisFrame)
                _publisher.FireEvent("boost.spool", 1f, shipFeedbackData.BoostSpoolTotalDurationSeconds);

            if (shipFeedbackData.BoostThrustStartThisFrame)
                _publisher.FireEvent("boost.thrust", 1f, shipFeedbackData.BoostThrustTotalDurationSeconds);
        }

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        private void OnDestroy() {
            StopServer();
        }

        private void OnGameSettingsApplied() {
            var enable = Preferences.Instance.GetBool("ogsTelemetryEnabled");
            var address = Preferences.Instance.GetString("ogsTelemetryMulticastAddress");
            var port = (int)Preferences.Instance.GetFloat("ogsTelemetryPort");

            // restart if the endpoint changed while running
            if (_isEnabled && (address != _multicastAddress || port != _multicastPort)) {
                FdConsole.Instance.LogMessage("Parameters changed, restarting OGS-TS Telemetry Server ...");
                StopServer();
            }

            _multicastAddress = address;
            _multicastPort = port;

            if (enable) StartServer();
            else StopServer();
        }

        [Button("Start Server")]
        [UsedImplicitly]
        private void StartServer() {
            if (_isEnabled) return;

            // manual (inspector) start before any settings apply: fall back to stored preferences
            if (string.IsNullOrEmpty(_multicastAddress)) {
                _multicastAddress = Preferences.Instance.GetString("ogsTelemetryMulticastAddress");
                _multicastPort = (int)Preferences.Instance.GetFloat("ogsTelemetryPort");
            }

            FdConsole.Instance.LogMessage(
                $"Starting OGS-TS Telemetry Server (udp multicast {_multicastAddress}:{_multicastPort}) ... ");

            try {
                _sender = new TelemetrySender(_multicastAddress, _multicastPort);
                _publisher = new TelemetryPublisher(_sender, Application.productName, MotionHz);
                DeclareAndBind();
                _isEnabled = true;
            }
            catch (Exception e) {
                Debug.LogError($"Failed to start OGS-TS telemetry server: {e.Message}");
                _publisher?.Dispose();
                _publisher = null;
                _sender?.Dispose();
                _sender = null;
            }
        }

        [Button("Stop Server")]
        [UsedImplicitly]
        private void StopServer() {
            if (!_isEnabled && _publisher == null) return;

            FdConsole.Instance.LogMessage("Shutting down OGS-TS Telemetry Server");
            _isEnabled = false;
            _publisher?.Dispose();
            _publisher = null;
            _sender?.Dispose();
            _sender = null;
            _lastTypeLabel = null;
            _lastSampleTime = double.NaN;
            _motionData = null;
            _instrumentData = null;
        }

        /**
         * Two-phase wiring: declare the primary object + channel table (becomes the periodic
         * DiscoveryFrame), then bind getters sampled on the publisher's tick (become MotionFrame
         * values). Ratified CHANNELS.md names are used where the concept matches; everything else
         * uses the game's own names.
         */
        private void DeclareAndBind() {
            var shipName = FdPlayer.FindLocalShipPlayer?.ShipPhysics.ShipProfile?.shipModel ?? "";
            _lastTypeLabel = shipName;
            _publisher.DeclareObject("player", shipName);
            _publisher.DeclarePause();

            // pose: absolute world position + orientation. Unity's world frame (left-handed,
            // Z-forward, Y-up) is the OGS-TS frame - values pass through unmapped.
            _publisher.BindPose(
                () => ToCore(_instrumentData.WorldPosition),
                () => ToCore(Quaternion.Euler(_motionData.WorldRotationEuler)));

            _publisher.BindBodyDynamics(() => (_specificForce, _angularVelocityC4));

            // instruments
            var speed = _publisher.DeclareChannel("vehicle.speed", Unit.MetrePerSecond,
                "Speed over ground", 0);
            _publisher.BindChannelRaw(speed, () => _instrumentData.Speed);

            var gForce = _publisher.DeclareChannel("ship.gForce", Unit.MetrePerSecondSquared,
                "Magnitude of the G-force at the pilot's seat");
            _publisher.BindChannel(gForce, () => _instrumentData.GForce, Unit.GForce);

            var altitude = _publisher.DeclareChannel("ship.altitude", Unit.Metre,
                "Altitude above mean sea level (clamped when out of range of the terrain)");
            _publisher.BindChannelRaw(altitude, () => SanitiseHeight(_instrumentData.Altitude));

            var heightFromGround = _publisher.DeclareChannel("ship.heightFromGround", Unit.Metre,
                "Height above the terrain (clamped when out of range of the terrain)");
            _publisher.BindChannelRaw(heightFromGround, () => SanitiseHeight(_instrumentData.ShipHeightFromGround));

            var maxSpeed = _publisher.DeclareChannel("ship.maxSpeed", Unit.MetrePerSecond,
                "Maximum speed permitted by the current ship configuration");
            _publisher.BindChannelRaw(maxSpeed, () => _motionData.MaxSpeed);

            var proximityWarningSeconds = _publisher.DeclareChannel("ship.proximityWarningSeconds", Unit.Second,
                "Seconds until a potential collision if no vector change is made", 0);
            _publisher.BindChannelRaw(proximityWarningSeconds, () => _instrumentData.ProximityWarningSeconds);

            // controls (signs verified against torque application in ShipPhysics: +pitch nose-up,
            // +yaw nose-right, +roll right-side-down - the standard's C-5 control-input sense)
            var throttle = _publisher.DeclareChannel("engine.throttle", Unit.Scalar,
                "Main engine throttle, -1 (full reverse) to 1 (full forward)", -1, 1);
            _publisher.BindChannelRaw(throttle, () => _instrumentData.ThrottlePositionNormalised);

            var pitch = _publisher.DeclareChannel("ctrl.elevator", Unit.Scalar,
                "Pitch input, + = nose-up", -1, 1);
            _publisher.BindChannelRaw(pitch, () => _instrumentData.PitchPositionNormalised);

            var yaw = _publisher.DeclareChannel("ctrl.rudder", Unit.Scalar,
                "Yaw input, + = nose-right", -1, 1);
            _publisher.BindChannelRaw(yaw, () => _instrumentData.YawPositionNormalised);

            var roll = _publisher.DeclareChannel("ctrl.aileron", Unit.Scalar,
                "Roll input, + = right-side-down", -1, 1);
            _publisher.BindChannelRaw(roll, () => _instrumentData.RollPositionNormalised);

            var lateralH = _publisher.DeclareChannel("input.lateralH", Unit.Scalar,
                "Horizontal thruster input, -1 to 1 (+ = right)", -1, 1);
            _publisher.BindChannelRaw(lateralH, () => _instrumentData.LateralHPositionNormalised);

            var lateralV = _publisher.DeclareChannel("input.lateralV", Unit.Scalar,
                "Vertical thruster input, -1 to 1 (+ = up)", -1, 1);
            _publisher.BindChannelRaw(lateralV, () => _instrumentData.LateralVPositionNormalised);

            // boost
            var boostCapacitor = _publisher.DeclareChannel("boost.capacitor", Unit.Scalar,
                "Boost capacitor charge, 0 to 1", 0, 1);
            _publisher.BindChannel(boostCapacitor, () => _instrumentData.BoostCapacitorPercent, Unit.Percent);

            var boostThrustProgress = _publisher.DeclareChannel("boost.thrustProgress", Unit.Scalar,
                "Progress through the active boost, 0 to 1", 0, 1);
            _publisher.BindChannelRaw(boostThrustProgress, () => _boostThrustProgress);

            // feedback
            var shipShake = _publisher.DeclareChannel("ship.shake", Unit.Scalar,
                "Ship shake intensity, 0 to 1", 0, 1);
            _publisher.BindChannelRaw(shipShake, () => _shipShakeNormalised);

            // booleans
            BindBoolean("lights.headlights", "Ship lights on", () => _instrumentData.LightsActive);
            BindBoolean("ship.underWater", "Currently submerged", () => _instrumentData.UnderWater);
            BindBoolean("ship.proximityWarning", "Collision predicted on the current vector",
                () => _instrumentData.ProximityWarning);
            BindBoolean("aids.velocityLimiter", "Velocity limiter active",
                () => _instrumentData.VelocityLimiterActive);
            BindBoolean("aids.vectorFlightAssist", "Vector (translational) flight assist active",
                () => _instrumentData.VectorFlightAssistActive);
            BindBoolean("aids.rotationalFlightAssist", "Rotational flight assist active",
                () => _instrumentData.RotationalFlightAssistActive);
            BindBoolean("boost.timerReady", "Boost cycle complete (not recharging)",
                () => _instrumentData.BoostTimerReady);
            BindBoolean("boost.chargeReady", "Boost capacitor ready to fire",
                () => _instrumentData.BoostChargeReady);
        }

        private void BindBoolean(string name, string description, Func<bool> source) {
            _publisher.BindBooleanChannel(_publisher.DeclareBooleanChannel(name, description), source);
        }

        // feedback-driven channels cached per physics step (only used for the shake/boost progress
        // channels - discrete stimuli go through FireEvent instead)
        private float _shipShakeNormalised;
        private float _boostThrustProgress;

        // altitude / height from ground are Infinity away from any terrain (space!) - clamp to the
        // float range so the wire stays finite
        private static float SanitiseHeight(float height) {
            if (float.IsInfinity(height)) return float.MaxValue;
            return height;
        }

        private static CoreVector3 ToCore(Vector3 v) => new(v.x, v.y, v.z);
        private static CoreQuaternion ToCore(Quaternion q) => new(q.x, q.y, q.z, q.w);
    }
}
