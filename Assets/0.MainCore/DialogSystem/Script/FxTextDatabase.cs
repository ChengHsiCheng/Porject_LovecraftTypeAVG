using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 特效字（擬聲字）的動畫預設。要新的效果就加一筆，劇本直接寫 anim:新名字。
/// </summary>
[CreateAssetMenu(fileName = "FxTextDatabase", menuName = "Dialog/Fx Text Database")]
public class FxTextDatabase : ScriptableObject
{
    [LabelText("動畫預設")]
    [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "Key", DefaultExpandedState = true)]
    [SerializeField]
    private List<FxTextPreset> _presets = new List<FxTextPreset>();

    public bool TryGetPreset(string key, out FxTextPreset preset)
    {
        preset = null;

        if (_presets.Count == 0) return false;

        // 沒指定就用第一筆當預設
        if (string.IsNullOrEmpty(key))
        {
            preset = _presets[0];
            return true;
        }

        foreach (FxTextPreset entry in _presets)
        {
            if (entry == null || entry.Key != key) continue;

            preset = entry;
            return true;
        }

        return false;
    }

    [Serializable]
    public class FxTextPreset
    {
        [HorizontalGroup("Head")]
        [LabelText("名稱")] [LabelWidth(40)]
        [Tooltip("劇本裡寫 anim:這個名字")]
        public string Key = "rise";

        [HorizontalGroup("Head", Width = 90)]
        [Button("▶ 預覽", ButtonSizes.Small)]
        [GUIColor(0.5f, 0.85f, 1f)]
        void Preview()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("預覽要在 Play 模式下才看得到。");
                return;
            }

            FxTextPlayer player = UnityEngine.Object.FindAnyObjectByType<FxTextPlayer>();

            if (player == null)
            {
                Debug.LogWarning("場景裡找不到 FxTextPlayer。");
                return;
            }

            player.Play(PreviewText, Key);
        }

        [LabelText("預覽用文字")] [LabelWidth(80)]
        public string PreviewText = "滴滴滴滴";

        [Space(6)]
        [BoxGroup("Time", ShowLabel = false)]
        [LabelText("每個字的動畫長度")] [SuffixLabel("秒", true)] [MinValue(0.05f)]
        public float Duration = 1f;

        [BoxGroup("Time", ShowLabel = false)]
        [LabelText("時間曲線")]
        [Tooltip("左邊是開始、右邊是結束。中間拉高 = 一開始衝很快後面慢下來")]
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [BoxGroup("Time", ShowLabel = false)]
        [HorizontalGroup("Time/Fade")]
        [LabelText("淡入")] [SuffixLabel("秒", true)] [MinValue(0f)]
        public float FadeInDuration = 0.1f;

        [HorizontalGroup("Time/Fade")]
        [LabelText("淡出")] [SuffixLabel("秒", true)] [MinValue(0f)]
        public float FadeOutDuration = 0.3f;

        [BoxGroup("Look", ShowLabel = false)]
        [HorizontalGroup("Look/Size")]
        [LabelText("起始字級")] [MinValue(1f)]
        public float StartSize = 120f;

        [HorizontalGroup("Look/Size")]
        [LabelText("結束字級")] [MinValue(1f)]
        public float EndSize = 40f;

        [BoxGroup("Look", ShowLabel = false)]
        [LabelText("顏色")]
        public Color Color = Color.white;

        [BoxGroup("Move", ShowLabel = false)]
        [LabelText("起點")]
        [Tooltip("相對於 FX 容器中心。(0,0) 就是畫面正中央")]
        public Vector2 StartPosition = Vector2.zero;

        [BoxGroup("Move", ShowLabel = false)]
        [LabelText("飛行位移")]
        [Tooltip("往哪裡飛。y 正值 = 往上")]
        public Vector2 MoveOffset = new Vector2(0f, 300f);

        [BoxGroup("Char", ShowLabel = false)]
        [LabelText("一個字一個字")]
        [Tooltip("取消勾選 = 整串文字一起飛")]
        public bool PerCharacter = true;

        [BoxGroup("Char", ShowLabel = false)]
        [ShowIf("PerCharacter")]
        [LabelText("每個字的間隔")] [SuffixLabel("秒", true)] [MinValue(0f)]
        public float CharDelay = 0.08f;

        [BoxGroup("Char", ShowLabel = false)]
        [ShowIf("PerCharacter")]
        [LabelText("每個字排開的方向")]
        [Tooltip("依序累加。(60,0) = 往右排成一列；(0,0) = 全部疊在同一點")]
        public Vector2 CharOffset = new Vector2(60f, 0f);

        [BoxGroup("Char", ShowLabel = false)]
        [ShowIf("PerCharacter")]
        [LabelText("每個字的隨機偏移")]
        [Tooltip("想要亂一點就調大")]
        public Vector2 CharRandomOffset = Vector2.zero;
    }
}
