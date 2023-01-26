using System.Collections;
using Misc;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace Gameplay {
    public class Billboard : MonoBehaviour {
        private static readonly int BillboardTextureProperty = Shader.PropertyToID("_BillboardTexture");
        private static readonly int TintProperty = Shader.PropertyToID("_Tint");
        private static readonly int ScrollSpeedProperty = Shader.PropertyToID("_ScrollSpeed");
        private static readonly int Dithering = Shader.PropertyToID("_UseDithering");

        [SerializeField] private Text billboardText;
        [SerializeField] private Camera renderTextureCamera;
        [SerializeField] private MeshRenderer screen;
        [SerializeField] private Material billboardScreenTransparent;
        [SerializeField] private Material billboardScreenDithered;

        private RenderTexture _renderTexture;
        private string _textureResource;
        private string _customMessage;
        private Color _tint;
        private float _intensity;
        private float _scrollSpeed;
        private bool _useDithering;

        private RenderTexture TextRenderTexture {
            get {
                if (_renderTexture == null) {
                    _renderTexture = new RenderTexture(720, 360, 0, GraphicsFormat.R8G8B8A8_UNorm);
                    _renderTexture.wrapMode = TextureWrapMode.Repeat;
                }

                return _renderTexture;
            }
        }

        public string TextureResource {
            get => _textureResource;
            set {
                _textureResource = value;
                if (_textureResource != "") DrawTexture(_textureResource);
            }
        }

        public string CustomMessage {
            get => _customMessage;
            set {
                _customMessage = value;
                if (_customMessage != "") DrawText(_customMessage);
            }
        }

        public Color Tint {
            get => _tint;
            set {
                _tint = value;
                DrawTint();
            }
        }

        public float ColorIntensity {
            get => _intensity;
            set {
                _intensity = value;
                DrawTint();
            }
        }

        public float ScrollSpeed {
            get => _scrollSpeed;
            set {
                _scrollSpeed = value;
                screen.material.SetFloat(ScrollSpeedProperty, _scrollSpeed);
            }
        }

        public bool UseDithering {
            get => _useDithering;
            set {
                _useDithering = value;
                screen.material = _useDithering ? billboardScreenDithered : billboardScreenTransparent;
            }
        }

        private void OnEnable() {
            // we never want this camera to auto-render as we only need it when text changes!
            renderTextureCamera.enabled = false;
        }

        private void OnDisable() {
            if (_renderTexture != null) {
                _renderTexture.Release();
                _renderTexture = null;
            }
        }

        private void DrawTexture(string textureResource) {
            renderTextureCamera.gameObject.SetActive(false);
            var texture = Resources.Load<Texture2D>($"billboards/{textureResource}");
            screen.material.SetTexture(BillboardTextureProperty, texture);
        }

        private void DrawText(string text) {
            renderTextureCamera.gameObject.SetActive(true);

            void SetTextWithRenderTexture() {
                billboardText.text = text;
                renderTextureCamera.targetTexture = TextRenderTexture;
                screen.material.SetTexture(BillboardTextureProperty, TextRenderTexture);
                renderTextureCamera.Render();
            }

            // Allow the whole object to be enabled by waiting a frame.
            IEnumerator SetTextAfterFrame() {
                yield return YieldExtensions.WaitForFixedFrames(1);
                SetTextWithRenderTexture();
            }

            StartCoroutine(SetTextAfterFrame());

            // live updates in editor (no coroutines!)
            if (Application.isEditor) SetTextWithRenderTexture();
        }

        private void DrawTint() {
            var color = new Color(Tint.r * _intensity, Tint.g * _intensity, Tint.b * _intensity);
            screen.material.SetColor(TintProperty, color);
        }
    }
}