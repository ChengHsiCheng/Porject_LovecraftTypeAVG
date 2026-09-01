using System.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 畫面演出：名字欄與背景。
/// </summary>
public class StageDirector : MonoBehaviour
{
    [BoxGroup("參照")] [LabelText("資料庫")] [Required] [SerializeField]
    private DialogDatabase _database;

    [BoxGroup("參照")] [LabelText("名字欄")] [Required] [SerializeField]
    private TMP_Text _nameText;

    [BoxGroup("參照")] [LabelText("背景 Image")] [Required] [SerializeField]
    private Image _backgroundImage;

    [BoxGroup("背景")] [LabelText("in:fade 的預設秒數")] [SuffixLabel("秒", true)] [SerializeField]
    private float _backgroundFadeDuration = 0.5f;

    private Coroutine _backgroundCoroutine;

    public bool IsBusy { get; private set; }

    /// <summary>#speaker:yuan as:？？？</summary>
    public void SetSpeaker(string key, string overrideName)
    {
        if (_nameText == null) return;

        // 沒指定角色也沒指定顯示文字 = 旁白，藏起來
        if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(overrideName))
        {
            _nameText.gameObject.SetActive(false);
            return;
        }

        string displayName = overrideName;

        if (string.IsNullOrEmpty(displayName))
        {
            if (_database == null || !_database.TryGetCharacterName(key, out displayName))
            {
                Debug.LogWarning($"資料表裡找不到角色：{key}");
                displayName = key; // 先照 key 顯示，方便發現漏填
            }
        }

        _nameText.gameObject.SetActive(true);
        _nameText.text = displayName;
    }

    /// <summary>#bg:cafe_night in:fade dur:1</summary>
    public void SetBackground(string key, bool useFade, float duration)
    {
        if (_backgroundImage == null) return;

        if (_database == null || !_database.TryGetBackground(key, out Sprite sprite))
        {
            Debug.LogWarning($"資料表裡找不到背景：{key}");
            return;
        }

        if (duration < 0f) duration = _backgroundFadeDuration;

        if (_backgroundCoroutine != null)
        {
            StopCoroutine(_backgroundCoroutine);
            IsBusy = false;
        }

        if (!useFade || duration <= 0f)
        {
            _backgroundImage.sprite = sprite;
            SetBackgroundAlpha(1f);
            return;
        }

        _backgroundCoroutine = StartCoroutine(FadeBackgroundRoutine(sprite, duration));
    }

    /// <summary>淡出 → 換圖 → 淡入。用單張 Image 就能做，不需要兩層背景。</summary>
    IEnumerator FadeBackgroundRoutine(Sprite sprite, float duration)
    {
        IsBusy = true;

        float half = duration * 0.5f;

        yield return FadeAlpha(_backgroundImage.color.a, 0f, half);

        _backgroundImage.sprite = sprite;

        yield return FadeAlpha(0f, 1f, half);

        IsBusy = false;
        _backgroundCoroutine = null;
    }

    IEnumerator FadeAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetBackgroundAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetBackgroundAlpha(to);
    }

    void SetBackgroundAlpha(float alpha)
    {
        Color color = _backgroundImage.color;
        color.a = Mathf.Clamp01(alpha);
        _backgroundImage.color = color;
    }
}
