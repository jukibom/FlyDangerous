using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Core.Player;
using Misc;
using UnityEngine;
using UnityEngine.VFX;

public class SpaceDust : MonoBehaviour {
    [SerializeField]
    private bool forceOn;
    private VisualEffect _vfx;
    private ShipPlayer _player;
    private Transform _playerShipTransform;
    private Rigidbody _playerShipRigidbody;

    private void OnEnable() {
        _vfx = GetComponent<VisualEffect>(); 
        _player = FdPlayer.FindLocalShipPlayer;
        if (_player) {
            _playerShipTransform = _player.GetComponent<Transform>();
            _playerShipRigidbody = _player.GetComponent<Rigidbody>();
        }
    }

    void Update() {
        if (!forceOn) {
            _vfx.enabled = Preferences.Instance.GetBool("showSpaceDust");
        }

        if (_player) {
            _vfx.SetVector3("_playerVelocity",
                _playerShipTransform.InverseTransformDirection(_playerShipRigidbody.velocity));
            _vfx.SetVector3("_playerVelocity", _playerShipRigidbody.velocity);
            _vfx.SetFloat("_alphaMultiplier", MathfExtensions.Remap(0.1f, 1, 0, 0.4f, _player.VelocityNormalised));
        }

        // lock the transform in world space so we don't rotate with the ship
        transform.rotation = Quaternion.identity;
    }
}
