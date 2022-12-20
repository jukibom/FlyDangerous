using System.Collections;
using Misc;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace Gameplay {
    public class Billboard : MonoBehaviour {
        private static readonly int BillboardTexture = Shader.PropertyToID("_BillboardTexture");

        [SerializeField] private Text billboardText;
        [SerializeField] private Camera renderTextureCamera;
        [SerializeField] private MeshRenderer screen;

        [SerializeField] private string testBillboardText;

        private RenderTexture _renderTexture;

        private RenderTexture RenderTexture {
            get {
                if (_renderTexture == null) _renderTexture = new RenderTexture(720, 360, 0, GraphicsFormat.R8G8B8A8_UNorm);

                return _renderTexture;
            }
        }

        private void OnEnable() {
            // we never want this camera to auto-render!
            renderTextureCamera.enabled = false;

            SetText(testBillboardText);
        }

        private void OnDisable() {
            if (_renderTexture != null) {
                _renderTexture.Release();
                _renderTexture = null;
            }
        }

        private void SetText(string text) {
            // Allow the whole object to be enabled by waiting a frame.
            IEnumerator SetTextAfterFrame() {
                yield return YieldExtensions.WaitForFixedFrames(1);
                billboardText.text = text;
                renderTextureCamera.targetTexture = RenderTexture;
                screen.material.SetTexture(BillboardTexture, RenderTexture);
                renderTextureCamera.Render();
            }

            if (text != "") StartCoroutine(SetTextAfterFrame());
        }
    }
}