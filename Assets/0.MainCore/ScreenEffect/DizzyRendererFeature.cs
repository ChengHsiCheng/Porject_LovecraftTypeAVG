using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 讀取 DizzyVolumeComponent 的數值，套到全螢幕 shader 上。
/// 加在 Renderer2D 的 Renderer Features 清單裡。
/// </summary>
public class DizzyRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader _shader;

    [SerializeField]
    private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private Material _material;
    private DizzyPass _pass;

    public override void Create()
    {
        if (_shader == null) return;

        _material = CoreUtils.CreateEngineMaterial(_shader);
        _pass = new DizzyPass(_material) { renderPassEvent = _renderPassEvent };
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

    class DizzyPass : ScriptableRenderPass
    {
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int DistortionId = Shader.PropertyToID("_Distortion");
        static readonly int WaveFrequencyId = Shader.PropertyToID("_WaveFrequency");
        static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
        static readonly int SwirlId = Shader.PropertyToID("_Swirl");
        static readonly int SwirlSpeedId = Shader.PropertyToID("_SwirlSpeed");
        static readonly int ChromaticId = Shader.PropertyToID("_Chromatic");
        static readonly int VignetteId = Shader.PropertyToID("_Vignette");

        readonly Material _material;
        readonly ProfilingSampler _sampler = new ProfilingSampler("Dizzy");

        RTHandle _tempTarget;

        public DizzyPass(Material material)
        {
            _material = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(ref _tempTarget, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_DizzyTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _tempTarget == null) return;

            DizzyVolumeComponent dizzy = VolumeManager.instance.stack.GetComponent<DizzyVolumeComponent>();

            if (dizzy == null || !dizzy.IsActive()) return;

            _material.SetFloat(IntensityId, dizzy.intensity.value);
            _material.SetFloat(DistortionId, dizzy.distortion.value);
            _material.SetFloat(WaveFrequencyId, dizzy.waveFrequency.value);
            _material.SetFloat(WaveSpeedId, dizzy.waveSpeed.value);
            _material.SetFloat(SwirlId, dizzy.swirl.value);
            _material.SetFloat(SwirlSpeedId, dizzy.swirlSpeed.value);
            _material.SetFloat(ChromaticId, dizzy.chromatic.value);
            _material.SetFloat(VignetteId, dizzy.vignette.value);

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, _sampler))
            {
                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // 畫面 → 暫存（套 shader）→ 畫回畫面
                //
                // 這裡刻意用 CommandBuffer.Blit 而不是 ScriptableRenderPass.Blit：
                // 後者內部走 Blitter.BlitCameraTexture，會把來源綁到 _BlitTexture 並用程序式頂點，
                // 跟這個 shader 的 _MainTex ＋ 傳統頂點對不上，畫面會全黑。
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
