using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 讀取 GlitchVolumeComponent 的數值，套到全螢幕 shader 上。
/// 加在 Renderer2D 的 Renderer Features 清單裡。
/// </summary>
public class GlitchRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader _shader;

    [SerializeField]
    private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private Material _material;
    private GlitchPass _pass;

    public override void Create()
    {
        if (_shader == null) return;

        _material = CoreUtils.CreateEngineMaterial(_shader);
        _pass = new GlitchPass(_material) { renderPassEvent = _renderPassEvent };
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

    class GlitchPass : ScriptableRenderPass
    {
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int DisplacementId = Shader.PropertyToID("_Displacement");
        static readonly int BlockDensityId = Shader.PropertyToID("_BlockDensity");
        static readonly int BlockAmountId = Shader.PropertyToID("_BlockAmount");
        static readonly int ColorSplitId = Shader.PropertyToID("_ColorSplit");
        static readonly int NoiseId = Shader.PropertyToID("_Noise");
        static readonly int SpeedId = Shader.PropertyToID("_Speed");

        readonly Material _material;
        readonly ProfilingSampler _sampler = new ProfilingSampler("Glitch");

        RTHandle _tempTarget;

        public GlitchPass(Material material)
        {
            _material = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(ref _tempTarget, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_GlitchTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _tempTarget == null) return;

            GlitchVolumeComponent glitch = VolumeManager.instance.stack.GetComponent<GlitchVolumeComponent>();

            if (glitch == null || !glitch.IsActive()) return;

            _material.SetFloat(IntensityId, glitch.intensity.value);
            _material.SetFloat(DisplacementId, glitch.displacement.value);
            _material.SetFloat(BlockDensityId, glitch.blockDensity.value);
            _material.SetFloat(BlockAmountId, glitch.blockAmount.value);
            _material.SetFloat(ColorSplitId, glitch.colorSplit.value);
            _material.SetFloat(NoiseId, glitch.noise.value);
            _material.SetFloat(SpeedId, glitch.speed.value);

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
