using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using MapMagic.Core;
using UnityEngine;
#if !NO_PAID_ASSETS
using GPUInstancer;
#endif

namespace Core {
    public class World : MonoBehaviour {
        private void OnEnable() {
            FloatingOrigin.OnFloatingOriginCorrection += PerformCorrection;
        }

        private void OnDisable() {
            FloatingOrigin.OnFloatingOriginCorrection -= PerformCorrection;
        }

#if !NO_PAID_ASSETS
        private readonly List<GPUInstancerDetailManager> _detailManagers = new();
        private GPUInstancerTreeManager _gpuInstancerTreeManager;

        [CanBeNull]
        private GPUInstancerTreeManager GPUInstancerTreeManager {
            get {
                if (_gpuInstancerTreeManager == null) _gpuInstancerTreeManager = FindObjectOfType<GPUInstancerTreeManager>();

                return _gpuInstancerTreeManager;
            }
        }

        private MapMagicObject _mapMagicObject;

        [CanBeNull]
        private MapMagicObject MapMagicObject {
            get {
                if (_mapMagicObject == null) _mapMagicObject = FindObjectOfType<MapMagicObject>();

                return _mapMagicObject;
            }
        }
#endif

        private void PerformCorrection(Vector3 offset) {
            foreach (Transform child in transform) child.position -= offset;

#if NO_PAID_ASSETS
        }
#else
            StartCoroutine(HandleGPUCulling());
        }

        /**
         * On floating point correction, GPU culling cannot work as it relies on the depth buffer.
         * The depth buffer takes a frame to calculate so for this frame we're going to take the GPU
         * performance hit and disable culling until the next render update.
         */
        private IEnumerator HandleGPUCulling() {
            // Store and restore whatever the settings in the editor were
            var shouldFrustumCullTrees = false;
            var shouldOcclusionCullTrees = false;
            var shouldFrustumCullDetails = false;
            var shouldOcclusionCullDetails = false;

            if (GPUInstancerTreeManager) {
                shouldFrustumCullTrees = GPUInstancerTreeManager.isFrustumCulling;
                shouldOcclusionCullTrees = GPUInstancerTreeManager.isOcclusionCulling;
                GPUInstancerTreeManager.isFrustumCulling = false;
                GPUInstancerTreeManager.isOcclusionCulling = false;
            }

            var mapMagicObject = MapMagicObject;
            if (mapMagicObject != null) {
                mapMagicObject.GetComponentsInChildren(false, _detailManagers);
                foreach (var gpuInstancerDetailManager in _detailManagers) {
                    shouldFrustumCullDetails = gpuInstancerDetailManager.isFrustumCulling;
                    shouldOcclusionCullDetails = gpuInstancerDetailManager.isOcclusionCulling;
                    gpuInstancerDetailManager.isFrustumCulling = false;
                    gpuInstancerDetailManager.isOcclusionCulling = false;
                }
            }

            yield return new WaitForEndOfFrame();

            if (GPUInstancerTreeManager) {
                GPUInstancerTreeManager.isFrustumCulling = shouldFrustumCullTrees;
                GPUInstancerTreeManager.isOcclusionCulling = shouldOcclusionCullTrees;
            }

            foreach (var gpuInstancerDetailManager in _detailManagers) {
                gpuInstancerDetailManager.isFrustumCulling = shouldFrustumCullDetails;
                gpuInstancerDetailManager.isOcclusionCulling = shouldOcclusionCullDetails;
            }
        }
#endif
    }
}