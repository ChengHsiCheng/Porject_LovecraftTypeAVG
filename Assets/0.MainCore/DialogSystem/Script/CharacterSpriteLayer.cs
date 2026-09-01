using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 立繪層。管理誰在畫面上、站在哪個位置，以及：
///   1. 說話者打亮，其他人縮小壓暗
///   2. 同一個位置有多人時排前後，說話者被推到最前，其他人往後擠（更小、更暗）
/// 一個角色同時只會有一份立繪。
/// </summary>
public class CharacterSpriteLayer : MonoBehaviour
{
    [BoxGroup("參照")] [LabelText("資料庫")] [Required] [SerializeField]
    private DialogDatabase _database;

    [BoxGroup("參照")] [LabelText("立繪範本")] [Required]
    [Tooltip("立繪用的 Image 範本，建議只留 Image ＋ Preserve Aspect")]
    [SerializeField] private Image _spritePrefab;

    [BoxGroup("位置錨點")] [HideLabel]
    [Tooltip("劇本用 at:名稱 指定。錨點物件的位置與大小決定立繪站在哪")]
    [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "Key", DefaultExpandedState = true)]
    [SerializeField]
    private List<PositionAnchor> _anchors = new List<PositionAnchor>();

    [FoldoutGroup("進退場")] [LabelText("淡入淡出秒數")] [SuffixLabel("秒", true)] [SerializeField]
    private float _defaultFadeDuration = 0.3f;

    [FoldoutGroup("說話者強調")]
    [HorizontalGroup("說話者強調/Scale")] [LabelText("說話者縮放")] [LabelWidth(90)]
    [Range(0.5f, 1.5f)] [SerializeField]
    private float _activeScale = 1f;

    [HorizontalGroup("說話者強調/Scale")] [LabelText("非說話者")] [LabelWidth(70)]
    [Range(0.5f, 1.5f)] [SerializeField]
    private float _inactiveScale = 0.92f;

    [FoldoutGroup("說話者強調")]
    [HorizontalGroup("說話者強調/Color")] [LabelText("說話者顏色")] [LabelWidth(90)] [SerializeField]
    private Color _activeColor = Color.white;

    [HorizontalGroup("說話者強調/Color")] [LabelText("非說話者")] [LabelWidth(70)] [SerializeField]
    private Color _inactiveColor = new Color(0.55f, 0.55f, 0.6f, 1f);

    [FoldoutGroup("說話者強調")] [LabelText("亮暗切換秒數")] [SuffixLabel("秒", true)]
    [Range(0f, 1f)] [SerializeField]
    private float _highlightDuration = 0.15f;

    [FoldoutGroup("說話者強調")] [LabelText("旁白時全部壓暗")]
    [Tooltip("關閉時，旁白會維持上一句的亮暗狀態")] [SerializeField]
    private bool _dimAllOnNarration;

    [FoldoutGroup("同位置前後")]
    [LabelText("縮放倍率")] [Tooltip("同位置站多人時，每往後一層再縮小一次。0.9 = 再縮 10%")]
    [Range(0.5f, 1f)] [SerializeField]
    private float _backScaleStep = 0.9f;

    [FoldoutGroup("同位置前後")] [LabelText("壓暗倍率")]
    [Tooltip("0.7 = 每往後一層再暗 30%")]
    [Range(0.2f, 1f)] [SerializeField]
    private float _backColorStep = 0.7f;

    [FoldoutGroup("同位置前後")] [LabelText("位移（預設）")]
    [Tooltip("讓後面的人露出來一點。左右要往不同方向偏時，到「位置錨點」裡各自覆蓋")]
    [SerializeField]
    private Vector2 _backOffset = new Vector2(60f, -20f);

    private readonly Dictionary<string, Slot> _slots = new Dictionary<string, Slot>();
    private readonly List<string> _order = new List<string>(); // 出場順序，決定同位置的先後
    private string _activeSpeaker = string.Empty;
    private int _busyCount;

    public bool IsBusy => _busyCount > 0;

    // ────────── 調整用（只在 Play 模式有作用）──────────

    [TitleGroup("測試", "調整參數會即時反映在畫面上", TitleAlignments.Left)]
    [ShowInInspector] [ReadOnly] [LabelText("畫面上的立繪")]
    [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true, DefaultExpandedState = true)]
    [InfoBox("Play 模式下才能測試。", InfoMessageType.Warning, "@!UnityEngine.Application.isPlaying")]
    private List<string> DebugSlots
    {
        get
        {
            List<string> lines = new List<string>();

            foreach (string key in _order)
            {
                if (!_slots.TryGetValue(key, out Slot slot)) continue;

                string mark = key == _activeSpeaker ? "◀ 說話中" : string.Empty;
                lines.Add($"{key}　│　{slot.Position}　│　第 {slot.Depth + 1} 層　{mark}");
            }

            return lines;
        }
    }

    [TitleGroup("測試")]
    [HorizontalGroup("測試/Input")] [LabelText("角色")] [LabelWidth(36)] [SerializeField]
    private string _previewCharacter = "momoka";

    [HorizontalGroup("測試/Input")] [LabelText("表情")] [LabelWidth(36)] [SerializeField]
    private string _previewFace = "normal";

    [HorizontalGroup("測試/Input")] [LabelText("位置")] [LabelWidth(36)] [SerializeField]
    private string _previewPosition = "left";

    [TitleGroup("測試")]
    [HorizontalGroup("測試/Buttons")]
    [Button("▶ 顯示", ButtonSizes.Medium)] [GUIColor(0.5f, 0.85f, 1f)]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    void PreviewShow()
    {
        Show(_previewCharacter, _previewFace, _previewPosition, true, -1f);
    }

    [HorizontalGroup("測試/Buttons")]
    [Button("▶ 設為說話者", ButtonSizes.Medium)] [GUIColor(0.5f, 0.85f, 1f)]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    void PreviewSpeaker()
    {
        SetActiveSpeaker(_previewCharacter);
    }

    [HorizontalGroup("測試/Buttons")]
    [Button("▶ 全部退場", ButtonSizes.Medium)]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    void PreviewHideAll()
    {
        Hide("*", true, -1f);
    }

    /// <summary>Inspector 一改數值就立刻套用到畫面上，方便邊調邊看。</summary>
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (_slots.Count == 0) return;

        RefreshLayout(true);
    }

    /// <summary>@sprite:momoka face:smile at:left</summary>
    public void Show(string characterKey, string face, string position, bool useFade, float duration)
    {
        if (string.IsNullOrEmpty(characterKey)) return;

        if (_spritePrefab == null)
        {
            Debug.LogWarning("CharacterSpriteLayer：沒有指定立繪 Prefab。");
            return;
        }

        if (string.IsNullOrEmpty(face)) face = "normal";

        string spriteKey = $"{characterKey}_{face}";

        if (_database == null || !_database.TryGetSprite(spriteKey, out Sprite sprite))
        {
            Debug.LogWarning($"資料表裡找不到立繪：{spriteKey}");
            return;
        }

        if (duration < 0f) duration = _defaultFadeDuration;

        bool isNew = !_slots.TryGetValue(characterKey, out Slot slot);

        if (isNew)
        {
            slot = new Slot { Image = Instantiate(_spritePrefab) };
            slot.Image.raycastTarget = false;
            _slots[characterKey] = slot;
            _order.Add(characterKey);
        }

        slot.Image.sprite = sprite;

        // 沒指定位置就沿用原本站的地方
        if (!string.IsNullOrEmpty(position) || isNew)
            MoveToAnchor(slot, position);

        RefreshLayout(isNew);

        if (isNew && useFade)
            StartCoroutine(FadeInRoutine(slot, duration));
        else if (isNew)
            SetAlpha(slot, 1f);
    }

    /// <summary>@sprite:momoka out ／ @sprite:* out</summary>
    public void Hide(string characterKey, bool useFade, float duration)
    {
        if (duration < 0f) duration = _defaultFadeDuration;

        if (characterKey == "*")
        {
            foreach (string key in new List<string>(_order))
                HideOne(key, useFade, duration);
        }
        else
        {
            HideOne(characterKey, useFade, duration);
        }

        RefreshLayout(false);
    }

    void HideOne(string characterKey, bool useFade, float duration)
    {
        if (!_slots.TryGetValue(characterKey, out Slot slot)) return;

        _slots.Remove(characterKey);
        _order.Remove(characterKey);

        if (useFade)
            StartCoroutine(FadeOutRoutine(slot, duration));
        else
            Destroy(slot.Image.gameObject);
    }

    /// <summary>由 #speaker 呼叫：說話者打亮並推到最前，其他人壓暗往後。</summary>
    public void SetActiveSpeaker(string characterKey)
    {
        _activeSpeaker = characterKey ?? string.Empty;
        RefreshLayout(false);
    }

    /// <summary>重算每個位置的前後順序與外觀。</summary>
    void RefreshLayout(bool immediate)
    {
        // 依位置分組，說話者排最前，其餘維持出場順序
        Dictionary<string, List<string>> groups = new Dictionary<string, List<string>>();

        foreach (string key in _order)
        {
            if (!_slots.TryGetValue(key, out Slot slot)) continue;

            string position = slot.Position ?? string.Empty;

            if (!groups.TryGetValue(position, out List<string> group))
            {
                group = new List<string>();
                groups[position] = group;
            }

            if (key == _activeSpeaker)
                group.Insert(0, key);
            else
                group.Add(key);
        }

        bool narration = string.IsNullOrEmpty(_activeSpeaker);

        foreach (List<string> group in groups.Values)
        {
            for (int depth = 0; depth < group.Count; depth++)
            {
                string key = group[depth];
                Slot slot = _slots[key];

                bool active = narration ? !_dimAllOnNarration : key == _activeSpeaker;
                slot.Depth = depth;

                // 最前面的在最上層：depth 0 的 sibling index 最大
                slot.Image.rectTransform.SetSiblingIndex(group.Count - 1 - depth);

                ApplyState(slot, active, depth, immediate);
            }
        }
    }

    void ApplyState(Slot slot, bool active, int depth, bool immediate)
    {
        float scale = active ? _activeScale : _inactiveScale;
        Color color = active ? _activeColor : _inactiveColor;
        Vector2 offset = Vector2.zero;

        // 往後擠：越後面越小、越暗、越偏移
        for (int i = 0; i < depth; i++)
        {
            scale *= _backScaleStep;
            color = new Color(color.r * _backColorStep, color.g * _backColorStep, color.b * _backColorStep, color.a);
        }

        offset = GetBackOffset(slot.Position) * depth;

        if (slot.StateCoroutine != null)
            StopCoroutine(slot.StateCoroutine);

        if (immediate || _highlightDuration <= 0f)
        {
            slot.Image.rectTransform.localScale = Vector3.one * scale;
            slot.Image.rectTransform.anchoredPosition = offset;
            SetColorKeepAlpha(slot, color);
            return;
        }

        slot.StateCoroutine = StartCoroutine(StateRoutine(slot, scale, color, offset));
    }

    IEnumerator StateRoutine(Slot slot, float targetScale, Color targetColor, Vector2 targetOffset)
    {
        RectTransform rect = slot.Image.rectTransform;

        float startScale = rect.localScale.x;
        Vector2 startOffset = rect.anchoredPosition;
        Color startColor = slot.Image.color;

        float elapsed = 0f;

        while (elapsed < _highlightDuration)
        {
            if (slot.Image == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _highlightDuration);

            rect.localScale = Vector3.one * Mathf.Lerp(startScale, targetScale, t);
            rect.anchoredPosition = Vector2.Lerp(startOffset, targetOffset, t);

            Color color = Color.Lerp(startColor, targetColor, t);
            color.a = slot.Image.color.a; // 透明度交給淡入淡出管
            slot.Image.color = color;

            yield return null;
        }

        rect.localScale = Vector3.one * targetScale;
        rect.anchoredPosition = targetOffset;
        SetColorKeepAlpha(slot, targetColor);
        slot.StateCoroutine = null;
    }

    void MoveToAnchor(Slot slot, string position)
    {
        RectTransform anchor = FindAnchor(position);
        string resolved = position;

        if (anchor == null)
        {
            if (!string.IsNullOrEmpty(position))
                Debug.LogWarning($"找不到位置：{position}");

            if (_anchors.Count == 0)
            {
                Debug.LogWarning("CharacterSpriteLayer：一個位置錨點都沒設定。");
                return;
            }

            anchor = _anchors[0].Anchor;
            resolved = _anchors[0].Key;
        }

        slot.Position = resolved;
        slot.Image.rectTransform.SetParent(anchor, false);
        slot.Image.rectTransform.anchoredPosition = Vector2.zero;
    }

    /// <summary>該位置有自訂就用自訂的，否則用預設值。</summary>
    Vector2 GetBackOffset(string position)
    {
        if (string.IsNullOrEmpty(position)) return _backOffset;

        foreach (PositionAnchor entry in _anchors)
        {
            if (entry == null || entry.Key != position) continue;

            return entry.OverrideBackOffset ? entry.BackOffset : _backOffset;
        }

        return _backOffset;
    }

    RectTransform FindAnchor(string position)
    {
        if (string.IsNullOrEmpty(position)) return null;

        foreach (PositionAnchor entry in _anchors)
        {
            if (entry == null || entry.Key != position) continue;

            return entry.Anchor;
        }

        return null;
    }

    IEnumerator FadeInRoutine(Slot slot, float duration)
    {
        _busyCount++;

        yield return FadeRoutine(slot, 0f, 1f, duration);

        _busyCount--;
    }

    IEnumerator FadeOutRoutine(Slot slot, float duration)
    {
        _busyCount++;

        yield return FadeRoutine(slot, slot.Image.color.a, 0f, duration);

        if (slot.Image != null)
            Destroy(slot.Image.gameObject);

        _busyCount--;
    }

    IEnumerator FadeRoutine(Slot slot, float from, float to, float duration)
    {
        SetAlpha(slot, from);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (slot.Image == null) yield break;

            elapsed += Time.deltaTime;
            SetAlpha(slot, Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetAlpha(slot, to);
    }

    void SetAlpha(Slot slot, float alpha)
    {
        if (slot.Image == null) return;

        Color color = slot.Image.color;
        color.a = Mathf.Clamp01(alpha);
        slot.Image.color = color;
    }

    void SetColorKeepAlpha(Slot slot, Color color)
    {
        color.a = slot.Image.color.a;
        slot.Image.color = color;
    }

    class Slot
    {
        public Image Image;
        public string Position;
        public int Depth;
        public Coroutine StateCoroutine;
    }

    [Serializable]
    public class PositionAnchor
    {
        [field: SerializeField] [field: LabelText("名稱")]
        [field: Tooltip("劇本寫 at:這個名字，例 left / center / right")]
        public string Key { get; private set; } = "center";

        [field: SerializeField] [field: LabelText("錨點物件")]
        public RectTransform Anchor { get; private set; }

        [field: SerializeField] [field: LabelText("自訂往後位移")]
        [field: Tooltip("勾選後這個位置改用下面的位移，例如左邊往左偏、右邊往右偏")]
        public bool OverrideBackOffset { get; private set; }

        [field: SerializeField] [field: LabelText("往後位移")]
        [field: ShowIf("OverrideBackOffset")]
        public Vector2 BackOffset { get; private set; } = new Vector2(-60f, -20f);
    }
}
