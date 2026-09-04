using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 讀取 GhostVolumeComponent 的數值，套到全螢幕 shader 上。
/// 加在 Renderer2D 的 Renderer Features 清單裡。
/// </summary>
public class GhostRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader _shader;

    [SerializeField]
    private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private Material _material;
    private GhostPass _pass;

    public override void Create()
    {
        if (_shader == null) return;

        _material = CoreUtils.CreateEngineMaterial(_shader);
        _pass = new GhostPass(_material) { renderPassEvent = _renderPassEvent };
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

    class GhostPass : ScriptableRenderPass
    {
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int OffsetId = Shader.PropertyToID("_Offset");
        static readonly int CountId = Shader.PropertyToID("_Count");
        static readonly int FalloffId = Shader.PropertyToID("_Falloff");
        static readonly int DriftSpeedId = Shader.PropertyToID("_DriftSpeed");
        static readonly int ColorShiftId = Shader.PropertyToID("_ColorShift");

        readonly Material _material;
        readonly ProfilingSampler _sampler = new ProfilingSampler("Ghost");

        RTHandle _tempTarget;

        public GhostPass(Material material)
        {
            _material = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(ref _tempTarget, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_GhostTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _tempTarget == null) return;

            GhostVolumeComponent ghost = VolumeManager.instance.stack.GetComponent<GhostVolumeComponent>();

            if (ghost == null || !ghost.IsActive()) return;

            _material.SetFloat(IntensityId, ghost.intensity.value);
            _material.SetVector(OffsetId, ghost.offset.value);
            _material.SetFloat(CountId, ghost.count.value);
            _material.SetFloat(FalloffId, ghost.falloff.value);
            _material.SetFloat(DriftSpeedId, ghost.driftSpeed.value);
            _material.SetFloat(ColorShiftId, ghost.colorShift.value);

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
