using System.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

/// <summary>
/// 特效字（擬聲字）演出。生成一份 TMP 文字，依預設做大小、位移、透明度動畫，播完銷毀。
/// 容器要放在黑幕（ScreenFader）之上，黑畫面裡才看得到。
/// </summary>
public class FxTextPlayer : MonoBehaviour
{
    [LabelText("動畫預設資料庫")] [Required] [SerializeField]
    private FxTextDatabase _database;

    [LabelText("文字範本")] [Required] [SerializeField]
    private TMP_Text _textPrefab;

    [LabelText("容器")] [Required]
    [Tooltip("要放在黑幕（ScreenFader）之上，黑畫面裡才看得到特效字")]
    [SerializeField] private RectTransform _container;

    private int _playingCount;

    public bool IsBusy => _playingCount > 0;

    /// <param name="animKey">預設名稱，空字串 = 用資料表第一筆</param>
    /// <param name="duration">秒數，負值 = 用預設值</param>
    [Button("▶ 測試播放（Play 模式）")]
    public void Play(string text, string animKey, float duration = -1f)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("FxTextPlayer：沒有要顯示的文字。");
            return;
        }

        if (_textPrefab == null || _container == null)
        {
            Debug.LogWarning("FxTextPlayer：沒有指定文字 Prefab 或容器。");
            return;
        }

        if (_database == null || !_database.TryGetPreset(animKey, out FxTextDatabase.FxTextPreset preset))
        {
            Debug.LogWarning($"找不到特效字預設：{animKey}");
            return;
        }

        if (!preset.PerCharacter)
        {
            StartCoroutine(PlayRoutine(text, preset, duration, 0));
            return;
        }

        // 一個字一個字，依序延遲送出
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i])) continue;

            StartCoroutine(PlayRoutine(text[i].ToString(), preset, duration, i));
        }
    }

    IEnumerator PlayRoutine(string text, FxTextDatabase.FxTextPreset preset, float duration, int index)
    {
        _playingCount++; // 先計數，延遲中的字也算還在播

        if (duration < 0f) duration = preset.Duration;

        Vector2 startPosition = preset.StartPosition;

        if (preset.PerCharacter)
        {
            startPosition += preset.CharOffset * index;
            startPosition += new Vector2(
                Random.Range(-preset.CharRandomOffset.x, preset.CharRandomOffset.x),
                Random.Range(-preset.CharRandomOffset.y, preset.CharRandomOffset.y));

            if (preset.CharDelay > 0f && index > 0)
                yield return new WaitForSeconds(preset.CharDelay * index);
        }

        TMP_Text instance = Instantiate(_textPrefab, _container);
        RectTransform rect = instance.rectTransform;

        instance.text = text;
        instance.raycastTarget = false;
        instance.enableAutoSizing = false;
        rect.anchoredPosition = startPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float eased = preset.Curve.Evaluate(t);

            instance.fontSize = Mathf.LerpUnclamped(preset.StartSize, preset.EndSize, eased);
            rect.anchoredPosition = startPosition + preset.MoveOffset * eased;

            instance.color = WithAlpha(preset.Color, GetAlpha(elapsed, duration, preset));

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(instance.gameObject);

        _playingCount--;
    }

    static float GetAlpha(float elapsed, float duration, FxTextDatabase.FxTextPreset preset)
    {
        float alpha = 1f;

        if (preset.FadeInDuration > 0f)
            alpha = Mathf.Min(alpha, elapsed / preset.FadeInDuration);

        if (preset.FadeOutDuration > 0f)
        {
            float remaining = duration - elapsed;
            alpha = Mathf.Min(alpha, remaining / preset.FadeOutDuration);
        }

        return Mathf.Clamp01(alpha);
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
