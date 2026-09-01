using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全螢幕淡入淡出。掛在一張蓋滿畫面的 Image 上。
/// </summary>
public class ScreenFader : MonoBehaviour
{
    [LabelText("蓋幕 Image")] [Required]
    [Tooltip("要蓋滿整個畫面，並且排在對話框之上、特效字之下")]
    [SerializeField] private Image _image;

    [LabelText("預設秒數")] [SuffixLabel("秒", true)] [SerializeField]
    private float _defaultDuration = 0.5f;

    [LabelText("預設顏色")] [SerializeField]
    private Color _defaultColor = Color.black;

    private Coroutine _fadeCoroutine;

    public bool IsFading { get; private set; }

    void Awake()
    {
        // SetAlpha(1f); // 起始為透明
    }

    /// <param name="fadeOut">true = 蓋起來（變黑），false = 恢復顯示</param>
    /// <param name="duration">秒數，負值代表使用預設值</param>
    public void Fade(bool fadeOut, float duration = -1f, Color? color = null)
    {
        if (_image == null)
        {
            Debug.LogWarning("ScreenFader：沒有指定 Image。");
            return;
        }

        if (duration < 0f) duration = _defaultDuration;

        Color target = color ?? _defaultColor;
        target.a = _image.color.a; // 只換顏色，透明度交給動畫
        _image.color = target;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(fadeOut ? 1f : 0f, duration));
    }

    IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        IsFading = true;

        float startAlpha = _image.color.a;

        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
        }
        else
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
                yield return null;
            }

            SetAlpha(targetAlpha);
        }

        IsFading = false;
        _fadeCoroutine = null;
    }

    void SetAlpha(float alpha)
    {
        if (_image == null) return;

        Color color = _image.color;
        color.a = Mathf.Clamp01(alpha);
        _image.color = color;

        // 全透明時不擋點擊
        _image.raycastTarget = false;
    }
}
