using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 讀取 DarkVolumeComponent 的數值，套到全螢幕 shader 上。
/// 加在 Renderer2D 的 Renderer Features 清單裡。
/// </summary>
public class DarkRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader _shader;

    [SerializeField]
    private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private Material _material;
    private DarkPass _pass;

    public override void Create()
    {
        if (_shader == null) return;

        _material = CoreUtils.CreateEngineMaterial(_shader);
        _pass = new DarkPass(_material) { renderPassEvent = _renderPassEvent };
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

    class DarkPass : ScriptableRenderPass
    {
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int DarknessId = Shader.PropertyToID("_Darkness");
        static readonly int VignetteId = Shader.PropertyToID("_Vignette");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        readonly Material _material;
        readonly ProfilingSampler _sampler = new ProfilingSampler("Dark");

        RTHandle _tempTarget;

        public DarkPass(Material material)
        {
            _material = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(ref _tempTarget, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_DarkTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _tempTarget == null) return;

            DarkVolumeComponent dark = VolumeManager.instance.stack.GetComponent<DarkVolumeComponent>();

            if (dark == null || !dark.IsActive()) return;

            _material.SetFloat(IntensityId, dark.intensity.value);
            _material.SetFloat(DarknessId, dark.darkness.value);
            _material.SetFloat(VignetteId, dark.vignette.value);
            _material.SetColor(ColorId, dark.color.value);

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
