using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Core.ShipModel.Feedback.interfaces;
using Core.ShipModel.ShipIndicator;
using JetBrains.Annotations;
using Misc;
using UnityEngine;

namespace Core.ShipModel.Feedback.socket {
    public class UDP : Singleton<UDP>, IShipMotion, IShipInstruments {

        private bool _isEnabled = true;
        private float _emitInterval = 0.02f;
        private int _broadcastPort = 11000;
        private IPAddress _broadcastIpAddress = IPAddress.Parse("127.0.0.1");


        private IShipMotionData _shipMotionData;
        private IShipInstrumentData _shipInstrumentData;
        
        
        [CanBeNull] private Socket _socket;
        [CanBeNull] private IPEndPoint _ipEndPoint;
        
        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        private void OnGameSettingsApplied() {
            Debug.Log("TODO: UDP CONFIG FUN TIMES");

            if (_isEnabled) {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _ipEndPoint = new IPEndPoint(_broadcastIpAddress, _broadcastPort);
            }

            else if (_socket != null) {
                _socket.Dispose();
                _socket = null;
            }
        }

        private void FixedUpdate() {
            // TODO: emit interval implementation
            if (!_isEnabled) return;

            if (_socket != null && _ipEndPoint != null) {
                byte[] test = Encoding.ASCII.GetBytes("OH GOD HERE WE GO\n");
                _socket.SendTo(test, _ipEndPoint);
            }
        }

        public void OnShipMotionUpdate(IShipMotionData shipMotionData) {
            _shipMotionData = shipMotionData;
        }

        public void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData) {
            _shipInstrumentData = shipInstrumentData;
        }
    }
}