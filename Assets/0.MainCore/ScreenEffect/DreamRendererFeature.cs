using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 讀取 DreamVolumeComponent 的數值，套到全螢幕 shader 上。
/// 加在 Renderer2D 的 Renderer Features 清單裡。
/// </summary>
public class DreamRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader _shader;

    [SerializeField]
    private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private Material _material;
    private DreamPass _pass;

    public override void Create()
    {
        if (_shader == null) return;

        _material = CoreUtils.CreateEngineMaterial(_shader);
        _pass = new DreamPass(_material) { renderPassEvent = _renderPassEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass == null || _material == null) return;

        CameraType cameraType = renderingData.cameraData.cameraType;

        if (cameraType != CameraType.Game && cameraType != CameraType.SceneView) return;

        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
        CoreUtils.Destroy(_material);
    }

    class DreamPass : ScriptableRenderPass
    {
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int BlurId = Shader.PropertyToID("_Blur");
        static readonly int GlowId = Shader.PropertyToID("_Glow");
        static readonly int GlowThresholdId = Shader.PropertyToID("_GlowThreshold");
        static readonly int DesaturateId = Shader.PropertyToID("_Desaturate");
        static readonly int TintId = Shader.PropertyToID("_Tint");
        static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
        static readonly int BreathSpeedId = Shader.PropertyToID("_BreathSpeed");

        readonly Material _material;
        readonly ProfilingSampler _sampler = new ProfilingSampler("Dream");

        RTHandle _tempTarget;

        public DreamPass(Material material)
        {
            _material = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(ref _tempTarget, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_DreamTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _tempTarget == null) return;

            DreamVolumeComponent dream = VolumeManager.instance.stack.GetComponent<DreamVolumeComponent>();

            if (dream == null || !dream.IsActive()) return;

            _material.SetFloat(IntensityId, dream.intensity.value);
            _material.SetFloat(BlurId, dream.blur.value);
            _material.SetFloat(GlowId, dream.glow.value);
            _material.SetFloat(GlowThresholdId, dream.glowThreshold.value);
            _material.SetFloat(DesaturateId, dream.desaturate.value);
            _material.SetColor(TintId, dream.tint.value);
            _material.SetFloat(EdgeSoftnessId, dream.edgeSoftness.value);
            _material.SetFloat(BreathSpeedId, dream.breathSpeed.value);

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, _sampler))
            {
                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                cmd.Blit(source, _tempTarget, _material, 0);
                cmd.Blit(_tempTarget, source);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _tempTarget?.Release();
            _tempTarget = null;
        }
    }
}
