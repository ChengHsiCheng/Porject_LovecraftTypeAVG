using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>畫面效果的種類，對應劇本的 @dizzy / @ghost / @glitch / @dream / @dark。</summary>
public enum ScreenEffectKind
{
    Dizzy,
    Ghost,
    Glitch,
    Dream,
    Dark
}

/// <summary>
/// 畫面效果的入口。負責把 0~1 的強度隨時間寫進各自的 Volume。
/// 三種效果各有自己的協程，可以同時進行（例如暈眩＋重影）。
/// </summary>
public class ScreenEffectDirector : MonoBehaviour
{
    [LabelText("Volume")] [Required]
    [Tooltip("場景上的 Global Volume，Profile 裡要有對應的 override")]
    [SerializeField] private Volume _volume;

    [BoxGroup("預設值")] [LabelText("漸強／漸退秒數")] [SuffixLabel("秒", true)] [SerializeField]
    private float _defaultDuration = 1f;

    [BoxGroup("預設值")] [LabelText("錯誤效果的預設秒數")] [SuffixLabel("秒", true)]
    [Tooltip("@glitch 專用。故障感要短促，所以跟其他效果分開")] [SerializeField]
    private float _glitchDefaultDuration = 0.2f;

    [BoxGroup("預設值")] [LabelText("預設強度")] [Range(0f, 1f)] [SerializeField]
    private float _defaultStrength = 0.5f;

    private DizzyVolumeComponent _dizzy;
    private GhostVolumeComponent _ghost;
    private GlitchVolumeComponent _glitch;
    private DreamVolumeComponent _dream;
    private DarkVolumeComponent _dark;

    private readonly Channel[] _channels = new Channel[5];

    /// <summary>任一效果的強度還在變化中。</summary>
    public bool IsBusy
    {
        get
        {
            foreach (Channel channel in _channels)
            {
                if (channel != null && channel.IsBusy) return true;
            }

            return false;
        }
    }

    [ShowInInspector] [ReadOnly] [LabelText("暈眩")] [PropertyRange(0f, 1f)]
    public float DizzyStrength => GetChannel(ScreenEffectKind.Dizzy).Strength;

    [ShowInInspector] [ReadOnly] [LabelText("重影")] [PropertyRange(0f, 1f)]
    public float GhostStrength => GetChannel(ScreenEffectKind.Ghost).Strength;

    [ShowInInspector] [ReadOnly] [LabelText("錯誤")] [PropertyRange(0f, 1f)]
    public float GlitchStrength => GetChannel(ScreenEffectKind.Glitch).Strength;

    [ShowInInspector] [ReadOnly] [LabelText("夢境")] [PropertyRange(0f, 1f)]
    public float DreamStrength => GetChannel(ScreenEffectKind.Dream).Strength;

    [ShowInInspector] [ReadOnly] [LabelText("壓黑")] [PropertyRange(0f, 1f)]
    public float DarkStrength => GetChannel(ScreenEffectKind.Dark).Strength;

    void Awake()
    {
        if (_volume == null)
        {
            Debug.LogWarning("ScreenEffectDirector：沒有指定 Volume。");
            return;
        }

        // 用 profile（執行期複本）而不是 sharedProfile，才不會改到資產本身
        VolumeProfile profile = _volume.profile;

        if (!profile.TryGet(out _dizzy)) Warn("暈眩 Dizzy");
        if (!profile.TryGet(out _ghost)) Warn("重影 Ghost");
        if (!profile.TryGet(out _glitch)) Warn("錯誤 Glitch");
        if (!profile.TryGet(out _dream)) Warn("夢境 Dream");
        if (!profile.TryGet(out _dark)) Warn("壓黑 Dark");

        foreach (ScreenEffectKind kind in Enum.GetValues(typeof(ScreenEffectKind)))
            GetChannel(kind).Apply(0f);
    }

    static void Warn(string overrideName)
    {
        Debug.LogWarning($"ScreenEffectDirector：Volume Profile 裡沒有「{overrideName}」。");
    }

    Channel GetChannel(ScreenEffectKind kind)
    {
        int index = (int)kind;

        if (_channels[index] == null)
            _channels[index] = new Channel(this, kind);

        return _channels[index];
    }

    /// <summary>@dizzy:on ／ @ghost:off 之類。strength、duration 傳負值就用預設值。</summary>
    public void SetEffect(ScreenEffectKind kind, bool on, float strength = -1f, float duration = -1f)
    {
        if (strength < 0f) strength = _defaultStrength;
        if (duration < 0f) duration = GetDefaultDuration(kind);

        Channel channel = GetChannel(kind);
        channel.Run(channel.FadeRoutine(on ? strength : 0f, duration));
    }

    /// <summary>@xxx:pulse —— 衝到指定強度再退回 0。</summary>
    public void PulseEffect(ScreenEffectKind kind, float strength = -1f, float duration = -1f)
    {
        if (strength < 0f) strength = _defaultStrength;
        if (duration < 0f) duration = GetDefaultDuration(kind);

        Channel channel = GetChannel(kind);
        channel.Run(channel.PulseRoutine(strength, duration));
    }

    /// <summary>重影的層數與偏移可以由劇本臨時覆蓋（@ghost:on offset:20,6 count:3）。</summary>
    public void SetGhostShape(Vector2? offset, int count)
    {
        if (_ghost == null) return;

        if (offset.HasValue)
        {
            _ghost.offset.overrideState = true;
            _ghost.offset.value = offset.Value;
        }

        if (count > 0)
        {
            _ghost.count.overrideState = true;
            _ghost.count.value = count;
        }
    }

    float GetDefaultDuration(ScreenEffectKind kind)
    {
        return kind == ScreenEffectKind.Glitch ? _glitchDefaultDuration : _defaultDuration;
    }

    void ApplyToVolume(ScreenEffectKind kind, float value)
    {
        switch (kind)
        {
            case ScreenEffectKind.Dizzy:
                SetIntensity(_dizzy != null ? _dizzy.intensity : null, value);
                break;

            case ScreenEffectKind.Ghost:
                SetIntensity(_ghost != null ? _ghost.intensity : null, value);
                break;

            case ScreenEffectKind.Glitch:
                SetIntensity(_glitch != null ? _glitch.intensity : null, value);
                break;

            case ScreenEffectKind.Dream:
                SetIntensity(_dream != null ? _dream.intensity : null, value);
                break;

            case ScreenEffectKind.Dark:
                SetIntensity(_dark != null ? _dark.intensity : null, value);
                break;
        }
    }

    static void SetIntensity(ClampedFloatParameter parameter, float value)
    {
        if (parameter == null) return;

        parameter.overrideState = true;
        parameter.value = value;
    }

    /// <summary>一種效果一條通道，各自獨立跑，互不打斷。</summary>
    class Channel
    {
        readonly ScreenEffectDirector _owner;
        readonly ScreenEffectKind _kind;

        Coroutine _routine;

        public float Strength { get; private set; }
        public bool IsBusy { get; private set; }

        public Channel(ScreenEffectDirector owner, ScreenEffectKind kind)
        {
            _owner = owner;
            _kind = kind;
        }

        public void Run(IEnumerator routine)
        {
            if (_routine != null) _owner.StopCoroutine(_routine);

            _routine = _owner.StartCoroutine(Wrap(routine));
        }

        IEnumerator Wrap(IEnumerator routine)
        {
            IsBusy = true;

            yield return routine;

            IsBusy = false;
            _routine = null;
        }

        public IEnumerator FadeRoutine(float target, float duration)
        {
            float start = Strength;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Apply(Mathf.Lerp(start, target, elapsed / duration));
                yield return null;
            }

            Apply(target);
        }

        public IEnumerator PulseRoutine(float strength, float duration)
        {
            float half = duration * 0.5f;

            yield return FadeRoutine(strength, half);
            yield return FadeRoutine(0f, half);
        }

        public void Apply(float value)
        {
            Strength = Mathf.Clamp01(value);
            _owner.ApplyToVolume(_kind, Strength);
        }
    }

    // ────────── 測試 ──────────

    [TitleGroup("測試", "Play 模式下可用")]
    [HorizontalGroup("測試/Kind")] [LabelText("效果")] [LabelWidth(40)] [SerializeField]
    private ScreenEffectKind _previewKind = ScreenEffectKind.Dizzy;

    [HorizontalGroup("測試/Kind")] [LabelText("強度")] [LabelWidth(40)] [Range(0f, 1f)] [SerializeField]
    private float _previewStrength = 0.6f;

    [TitleGroup("測試")]
    [HorizontalGroup("測試/Buttons")]
    [Button("▶ 開", ButtonSizes.Medium)] [GUIColor(0.5f, 0.85f, 1f)]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    void PreviewOn()
    {
        SetEffect(_previewKind, true, _previewStrength);
    }

    [HorizontalGroup("測試/Buttons")]
    [Button("▶ 關", ButtonSizes.Medium)]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    void PreviewOff()
    {
        SetEffect(_previewKind, false);
    }

    [HorizontalGroup("測試/Buttons")]
    [Button("▶ 一下", ButtonSizes.Medium)]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    void PreviewPulse()
    {
        PulseEffect(_previewKind, _previewStrength);
    }

    [HorizontalGroup("測試/Buttons")]
    [Button("▶ 全關", ButtonSizes.Medium)]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    void PreviewOffAll()
    {
        foreach (ScreenEffectKind kind in Enum.GetValues(typeof(ScreenEffectKind)))
            SetEffect(kind, false);
    }
}
