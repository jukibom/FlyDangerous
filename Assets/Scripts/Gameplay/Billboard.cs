using System.Collections;
using Misc;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace Gameplay {
    public class Billboard : MonoBehaviour {
        private static readonly int BillboardTexture = Shader.PropertyToID("_BillboardTexture");
        private static readonly int Tint = Shader.PropertyToID("_Tint");
        private static readonly int ScrollSpeed = Shader.PropertyToID("_ScrollSpeed");

        [SerializeField] private Text billboardText;
        [SerializeField] private Camera renderTextureCamera;
        [SerializeField] private MeshRenderer screen;
        [SerializeField] private string initialBillboardText;
        [SerializeField] private string initialBillboardTexture;

        [ColorUsage(true, true)] [SerializeField]
        private Color initialBillboardTint = Color.white;

        [SerializeField] private float initialBillboardColorIntensity;
        [SerializeField] private float initialBillboardScrollSpeed;

        private RenderTexture _renderTexture;
        private Color _billboardTint;
        private float _billboardScrollSpeed;
        private float _billboardColorIntensity;

        private RenderTexture TextRenderTexture {
            get {
                if (_renderTexture == null) {
                    _renderTexture = new RenderTexture(720, 360, 0, GraphicsFormat.R8G8B8A8_UNorm);
                    _renderTexture.wrapMode = TextureWrapMode.Repeat;
                }

                return _renderTexture;
            }
        }

        private Color BillboardTint {
            get => _billboardTint;
            set {
                _billboardTint = value;
                screen.material.SetColor(Tint, _billboardTint);
            }
        }

        private float BillboardColorIntensity {
            get => _billboardColorIntensity;
            set {
                _billboardColorIntensity = value;
                BillboardTint = new Color(BillboardTint.r * value, BillboardTint.g * value, BillboardTint.b * value);
            }
        }

        private float BillboardScrollSpeed {
            get => _billboardScrollSpeed;
            set {
                _billboardScrollSpeed = value;
                screen.material.SetFloat(ScrollSpeed, _billboardScrollSpeed);
            }
        }

        private void OnEnable() {
            // we never want this camera to auto-render as we only need it when text changes!
            renderTextureCamera.enabled = false;
            if (initialBillboardText != "") DrawText(initialBillboardText);
            if (initialBillboardTexture != "") DrawTexture(initialBillboardTexture);
            BillboardTint = initialBillboardTint;
            BillboardColorIntensity = initialBillboardColorIntensity;
            BillboardScrollSpeed = initialBillboardScrollSpeed;
        }

        private void OnDisable() {
            if (_renderTexture != null) {
                _renderTexture.Release();
                _renderTexture = null;
            }
        }

        private void DrawTexture(string textureResource) {
            var texture = Resources.Load<Texture2D>($"billboards/{textureResource}");
            screen.material.SetTexture(BillboardTexture, texture);
        }

        private void DrawText(string text) {
            // Allow the whole object to be enabled by waiting a frame.
            IEnumerator SetTextAfterFrame() {
                yield return YieldExtensions.WaitForFixedFrames(1);
                billboardText.text = text;
                renderTextureCamera.targetTexture = TextRenderTexture;
                screen.material.SetTexture(BillboardTexture, TextRenderTexture);
                renderTextureCamera.Render();
            }

            StartCoroutine(SetTextAfterFrame());
        }
    }
}