using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace VFX {
    public sealed class SkyboxDepthFogRendererFeature : ScriptableRendererFeature {

        [Serializable]
        public sealed class Settings {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

            [Tooltip("Shader used for the fullscreen fog pass.")]
            public Shader shader;

            [Min(0f)] public float fogStart = 100f;

            [Min(0.001f)] public float fogEnd = 1000f;

            [Range(0f, 1f)] public float fogIntensity = 1f;

            [Tooltip("Optional fallback cubemap if the active RenderSettings.skybox does not expose one.")]
            public Cubemap fallbackSkyCubemap;

            [Tooltip("Common skybox cubemap property names to try on RenderSettings.skybox.")]
            public string[] skyboxTexturePropertyNames = {
                "_Tex",
                "_Cubemap",
                "_Skybox",
                "_MainTex"
            };
        }

        public static SkyboxDepthFogRendererFeature Instance { get; private set; }
        
        public Settings settings = new();

        private SkyboxDepthFogPass _pass;
        private Material _material;

        private static readonly int SkyCubemapId = Shader.PropertyToID("_SkyCubemap");
        private static readonly int SkyRotationId = Shader.PropertyToID("_SkyRotation");
        private static readonly int FogStartId = Shader.PropertyToID("_FogStart");
        private static readonly int FogEndId = Shader.PropertyToID("_FogEnd");
        private static readonly int FogIntensityId = Shader.PropertyToID("_FogIntensity");
        private static readonly int Rotation = Shader.PropertyToID("_Rotation");
        private static readonly int SourceTextureId = Shader.PropertyToID("_SkyboxDepthFogSource");

        public void SetFogDistances(float fogStart, float fogEnd) {
            settings.fogStart = Mathf.Max(0f, fogStart);
            settings.fogEnd = Mathf.Max(settings.fogStart + 0.001f, fogEnd);
            _pass?.UpdateSettings();
        }

        public void SetFog(float fogStart, float fogEnd, float fogIntensity) {
            settings.fogStart = Mathf.Max(0f, fogStart);
            settings.fogEnd = Mathf.Max(settings.fogStart + 0.001f, fogEnd);
            settings.fogIntensity = Mathf.Clamp01(fogIntensity);
            _pass?.UpdateSettings();
        }
        
        public override void Create() {
            Instance = this;
            
            if (settings.shader == null)
                return;

            _material = CoreUtils.CreateEngineMaterial(settings.shader);
            _pass = new SkyboxDepthFogPass(_material, settings) {
                renderPassEvent = settings.renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (_material == null || _pass == null)
                return;

            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;

            _pass.UpdateSettings();
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing) {
            if (Instance == this)
                Instance = null;
            
            _pass?.Dispose();
            CoreUtils.Destroy(_material);
        }

        private sealed class SkyboxDepthFogPass : ScriptableRenderPass {
            private const string PassName = "Skybox Depth Fog";

            private readonly Material _material;
            private readonly Settings _settings;

            private sealed class PassData {
                public TextureHandle Source;
                public Material Material;
            }

            public SkyboxDepthFogPass(Material material, Settings settings) {
                _material = material;
                _settings = settings;

                ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
            }

            public void UpdateSettings() {
                _material.SetFloat(FogStartId, _settings.fogStart);
                _material.SetFloat(FogEndId, Mathf.Max(_settings.fogEnd, _settings.fogStart + 0.001f));
                _material.SetFloat(FogIntensityId, _settings.fogIntensity);

                Texture skyTexture = GetCurrentSkyboxTexture();

                if (skyTexture != null)
                    _material.SetTexture(SkyCubemapId, skyTexture);

                Material skybox = RenderSettings.skybox;

                if (skybox != null && skybox.HasFloat(Rotation))
                    _material.SetFloat(SkyRotationId, skybox.GetFloat(Rotation));
                else
                    _material.SetFloat(SkyRotationId, 0f);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                if (resourceData.isActiveTargetBackBuffer)
                    return;

                TextureHandle source = resourceData.activeColorTexture;

                TextureDesc descriptor = source.GetDescriptor(renderGraph);
                descriptor.name = "_SkyboxDepthFogColor";
                descriptor.clearBuffer = false;
                descriptor.depthBufferBits = 0;

                TextureHandle destination = renderGraph.CreateTexture(descriptor);

                using (IRasterRenderGraphBuilder builder =
                       renderGraph.AddRasterRenderPass(PassName, out PassData passData)) {
                    passData.Source = source;
                    passData.Material = _material;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.AllowGlobalStateModification(true);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) => {
                        context.cmd.SetGlobalTexture(SourceTextureId, data.Source);
                        context.cmd.DrawProcedural(
                            Matrix4x4.identity,
                            data.Material,
                            0,
                            MeshTopology.Triangles,
                            3,
                            1
                        );
                    });
                }

                resourceData.cameraColor = destination;
            }

            public void Dispose() {
            }

            private Texture GetCurrentSkyboxTexture() {
                Material skybox = RenderSettings.skybox;

                if (skybox != null && _settings.skyboxTexturePropertyNames != null) {
                    foreach (string propertyName in _settings.skyboxTexturePropertyNames) {
                        if (string.IsNullOrWhiteSpace(propertyName))
                            continue;

                        if (!skybox.HasTexture(propertyName))
                            continue;

                        Texture texture = skybox.GetTexture(propertyName);

                        if (texture != null)
                            return texture;
                    }
                }

                return _settings.fallbackSkyCubemap;
            }
        }
    }
}