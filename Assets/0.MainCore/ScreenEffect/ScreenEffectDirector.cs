using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 畫面效果的入口。目前只有暈眩（@dizzy），之後的震動、閃白也掛這裡。
/// 負責把 0~1 的強度隨時間寫進 DizzyVolumeComponent。
/// </summary>
public class ScreenEffectDirector : MonoBehaviour
{
    [LabelText("Volume")] [Required]
    [Tooltip("場景上的 Global Volume，Profile 裡要有「暈眩 Dizzy」")]
    [SerializeField] private Volume _volume;

    [BoxGroup("預設值")] [LabelText("漸強／漸退秒數")] [SuffixLabel("秒", true)] [SerializeField]
    private float _defaultDuration = 1f;

    [BoxGroup("預設值")] [LabelText("預設強度")] [Range(0f, 1f)] [SerializeField]
    private float _defaultStrength = 0.5f;

    private DizzyVolumeComponent _dizzy;
    private Coroutine _routine;

    [ShowInInspector] [ReadOnly] [LabelText("目前暈眩強度")] [PropertyRange(0f, 1f)]
    public float DizzyStrength { get; private set; }

    /// <summary>強度還在變化中（漸強、漸退、一次性脈動）。</summary>
    public bool IsBusy { get; private set; }

    void Awake()
    {
        if (_volume == null)
        {
            Debug.LogWarning("ScreenEffectDirector：沒有指定 Volume。");
            return;
        }

        // 用 profile（執行期複本）而不是 sharedProfile，才不會改到資產本身
        if (!_volume.profile.TryGet(out _dizzy))
        {
            Debug.LogWarning("ScreenEffectDirector：Volume Profile 裡沒有「暈眩 Dizzy」。");
            return;
        }

        ApplyStrength(0f);
    }

    /// <summary>@dizzy:on ／ @dizzy:off。strength、duration 傳負值就用預設值。</summary>
    public void SetDizzy(bool on, float strength = -1f, float duration = -1f)
    {
        if (strength < 0f) strength = _defaultStrength;
        if (duration < 0f) duration = _defaultDuration;

        StartRoutine(FadeRoutine(on ? strength : 0f, duration));
    }

    /// <summary>@dizzy:pulse —— 衝到指定強度再退回 0。</summary>
    public void PulseDizzy(float strength = -1f, float duration = -1f)
    {
        if (strength < 0f) strength = _defaultStrength;
        if (duration < 0f) duration = _defaultDuration;

        StartRoutine(PulseRoutine(strength, duration));
    }

    void StartRoutine(IEnumerator routine)
    {
        if (_routine != null) StopCoroutine(_routine);

        _routine = StartCoroutine(RunRoutine(routine));
    }

    /// <summary>統一在這裡開關 IsBusy，避免中途出現一格的空窗。</summary>
    IEnumerator RunRoutine(IEnumerator routine)
    {
        IsBusy = true;

        yield return routine;

        IsBusy = false;
        _routine = null;
    }

    IEnumerator FadeRoutine(float target, float duration)
    {
        float start = DizzyStrength;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ApplyStrength(Mathf.Lerp(start, target, elapsed / duration));
            yield return null;
        }

        ApplyStrength(target);
    }

    IEnumerator PulseRoutine(float strength, float duration)
    {
        float half = duration * 0.5f;

        yield return FadeRoutine(strength, half);
        yield return FadeRoutine(0f, half);
    }

    void ApplyStrength(float value)
    {
        DizzyStrength = Mathf.Clamp01(value);

        if (_dizzy == null) return;

        _dizzy.intensity.overrideState = true;
        _dizzy.intensity.value = DizzyStrength;
    }

    // ────────── 測試 ──────────

    [TitleGroup("測試", "Play 模式下可用")]
    [HorizontalGroup("測試/Buttons")]
    [Button("▶ 暈眩開", ButtonSizes.Medium)] [GUIColor(0.5f, 0.85f, 1f)]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    void PreviewOn()
    {
        SetDizzy(true);
    }

    [HorizontalGroup("測試/Buttons")]
    [Button("▶ 暈眩關", ButtonSizes.Medium)]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    void PreviewOff()
    {
        SetDizzy(false);
    }

    [HorizontalGroup("測試/Buttons")]
    [Button("▶ 暈一下", ButtonSizes.Medium)]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    void PreviewPulse()
    {
        PulseDizzy();
    }
}
