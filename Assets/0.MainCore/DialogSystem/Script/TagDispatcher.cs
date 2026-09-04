using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 解析並執行劇本的演出指示，分兩種來源：
///   #tag       這一句話的屬性（誰說的、怎麼說）
///   @指令行     時間軸上的事件（畫面、聲音）
/// 之後要加新指令就在 Dispatch 的 switch 增加 case。
/// </summary>
public class TagDispatcher : MonoBehaviour
{
    [Title("演出模組", "沒接的指令會印警告，不會中斷遊戲")]
    [LabelText("fade（淡入淡出）")] [Required] [SerializeField]
    private ScreenFader _screenFader;

    [LabelText("bg／speaker（背景與名字）")] [Required] [SerializeField]
    private StageDirector _stageDirector;

    [LabelText("fx（特效字）")] [Required] [SerializeField]
    private FxTextPlayer _fxTextPlayer;

    [LabelText("sprite（立繪）")] [Required] [SerializeField]
    private CharacterSpriteLayer _spriteLayer;

    [LabelText("clear（清空對話框）")] [Required] [SerializeField]
    private DialogSystem _dialogSystem;


    [LabelText("畫面效果（dizzy／ghost／glitch／dream／dark）")] [SerializeField]
    private ScreenEffectDirector _screenEffect;

    private bool _waitForFade;
    private bool _waitForStage;
    private bool _waitForFx;
    private bool _waitForSprite;
    private bool _waitForScreenEffect;

    /// <summary>對話系統會等到這裡不忙才顯示文字。</summary>
    public bool IsBusy =>
        (_waitForFade && _screenFader != null && _screenFader.IsFading) ||
        (_waitForStage && _stageDirector != null && _stageDirector.IsBusy) ||
        (_waitForFx && _fxTextPlayer != null && _fxTextPlayer.IsBusy) ||
        (_waitForSprite && _spriteLayer != null && _spriteLayer.IsBusy) ||
        (_waitForScreenEffect && _screenEffect != null && _screenEffect.IsBusy);

    /// <summary>指令行的前綴。</summary>
    public const char CommandPrefix = '@';

    public static bool IsCommandLine(string line)
    {
        return !string.IsNullOrEmpty(line) && line[0] == CommandPrefix;
    }

    /// <summary>處理一句話帶的 #tag。</summary>
    public void HandleTags(List<string> tags)
    {
        ClearWait();

        if (tags == null) return;

        foreach (string raw in tags)
        {
            TagCommand tag = TagCommand.Parse(raw);

            if (tag.Command != string.Empty && IsEventCommand(tag.Command))
                Debug.LogWarning($"「{tag.Command}」是演出指令，請改寫成單獨一行的 @{tag.Command}");

            Dispatch(tag);
        }
    }

    /// <summary>執行一行 @ 指令。傳入的字串可含前綴。</summary>
    public void ExecuteCommand(string line)
    {
        ClearWait();

        if (string.IsNullOrEmpty(line)) return;

        if (IsCommandLine(line))
            line = line.Substring(1);

        TagCommand command = TagCommand.Parse(line);

        if (!IsEventCommand(command.Command) && command.Command != string.Empty)
            Debug.LogWarning($"「{command.Command}」是這句話的屬性，請改寫成 #{command.Command} 放在文字上方");

        Dispatch(command);
    }

    /// <summary>true = 時間軸事件（用 @），false = 句子屬性（用 #）。</summary>
    static bool IsEventCommand(string command)
    {
        switch (command)
        {
            case "speaker":
                return false;
            default:
                return true;
        }
    }

    void ClearWait()
    {
        _waitForFade = false;
        _waitForStage = false;
        _waitForFx = false;
        _waitForSprite = false;
        _waitForScreenEffect = false;
    }

    void Dispatch(TagCommand tag)
    {
        switch (tag.Command)
        {
            case "":
                break;

            case "fade":
                HandleFade(tag);
                break;

            case "speaker":
                HandleSpeaker(tag);
                break;

            case "bg":
                HandleBackground(tag);
                break;

            case "fx":
                HandleFxText(tag);
                break;

            case "clear":
                HandleClear(tag);
                break;

            case "dizzy":
                HandleScreenEffect(tag, ScreenEffectKind.Dizzy);
                break;

            case "ghost":
                HandleScreenEffect(tag, ScreenEffectKind.Ghost);
                break;

            case "glitch":
                HandleScreenEffect(tag, ScreenEffectKind.Glitch);
                break;

            case "dream":
                HandleScreenEffect(tag, ScreenEffectKind.Dream);
                break;

            case "dark":
                HandleScreenEffect(tag, ScreenEffectKind.Dark);
                break;

            case "sprite":
                HandleSprite(tag);
                break;

            default:
                Debug.LogWarning($"尚未支援的 tag：{tag.Command}");
                break;
        }
    }

    void HandleFade(TagCommand tag)
    {
        if (_screenFader == null)
        {
            Debug.LogWarning("TagDispatcher：沒有指定 ScreenFader。");
            return;
        }

        bool fadeOut;

        switch (tag.Value)
        {
            case "out":
                fadeOut = true;
                break;
            case "in":
                fadeOut = false;
                break;
            default:
                Debug.LogWarning($"fade 只接受 out 或 in，收到：{tag.Value}");
                return;
        }

        float duration = tag.GetFloat("dur", -1f); // 負值 = 用 ScreenFader 的預設秒數
        Color? color = ParseColor(tag.GetOption("color"));

        _screenFader.Fade(fadeOut, duration, color);

        // 預設等淡入淡出跑完才顯示這句文字，加 nowait 就不等
        if (!tag.HasOption("nowait"))
            _waitForFade = true;
    }

    void HandleSpeaker(TagCommand tag)
    {
        if (_stageDirector == null)
        {
            Debug.LogWarning("TagDispatcher：沒有指定 StageDirector。");
            return;
        }

        _stageDirector.SetSpeaker(tag.Value, tag.GetOption("as"));

        // 立繪跟著亮暗：說話者正常，其他人縮小壓暗
        if (_spriteLayer != null)
            _spriteLayer.SetActiveSpeaker(tag.Value);
    }

    /// <summary>@sprite:momoka face:smile at:left ／ @sprite:momoka out ／ @sprite:* out</summary>
    void HandleSprite(TagCommand tag)
    {
        if (_spriteLayer == null)
        {
            Debug.LogWarning("TagDispatcher：沒有指定 CharacterSpriteLayer。");
            return;
        }

        float duration = tag.GetFloat("dur", -1f);
        bool leaving = tag.HasOption("out");

        // in:cut / out:cut = 不做淡入淡出
        bool useFade = (leaving ? tag.GetOption("out", "fade") : tag.GetOption("in", "fade")) != "cut";

        if (leaving)
            _spriteLayer.Hide(tag.Value, useFade, duration);
        else
            _spriteLayer.Show(tag.Value, tag.GetOption("face"), tag.GetOption("at"), useFade, duration);

        if (useFade && !tag.HasOption("nowait"))
            _waitForSprite = true;
    }

    void HandleBackground(TagCommand tag)
    {
        if (_stageDirector == null)
        {
            Debug.LogWarning("TagDispatcher：沒有指定 StageDirector。");
            return;
        }

        // in: 沒寫預設直接切換
        bool useFade = tag.GetOption("in", "cut") == "fade";
        float duration = tag.GetFloat("dur", -1f);

        _stageDirector.SetBackground(tag.Value, useFade, duration);

        if (useFade && !tag.HasOption("nowait"))
            _waitForStage = true;
    }

    void HandleFxText(TagCommand tag)
    {
        if (_fxTextPlayer == null)
        {
            Debug.LogWarning("TagDispatcher：沒有指定 FxTextPlayer。");
            return;
        }

        _fxTextPlayer.Play(tag.Value, tag.GetOption("anim"), tag.GetFloat("dur", -1f));

        if (!tag.HasOption("nowait"))
            _waitForFx = true;
    }

    /// <summary>@clear（文字＋名字）、@clear:text、@clear:name</summary>
    void HandleClear(TagCommand tag)
    {
        bool clearText = tag.Value != "name";
        bool clearName = tag.Value != "text";

        if (clearText)
        {
            if (_dialogSystem == null)
                Debug.LogWarning("TagDispatcher：沒有指定 DialogSystem。");
            else
                _dialogSystem.ClearDialogueText();
        }

        if (clearName && _stageDirector != null)
            _stageDirector.SetSpeaker(string.Empty, string.Empty);
    }

    /// <summary>
    /// @dizzy / @ghost / @glitch，三個指令共用同一套語法：
    ///   :on strength:0.6 dur:2 ／ :off ／ :pulse
    /// on、off 是持續狀態，預設不擋住對話；pulse 是一次性的，預設會等它跑完。
    /// </summary>
    void HandleScreenEffect(TagCommand tag, ScreenEffectKind kind)
    {
        if (_screenEffect == null)
        {
            Debug.LogWarning("TagDispatcher：沒有指定 ScreenEffectDirector。");
            return;
        }

        float strength = tag.GetFloat("strength", -1f);
        float duration = tag.GetFloat("dur", -1f);

        // 重影可以臨時指定殘影的偏移與層數
        if (kind == ScreenEffectKind.Ghost && (tag.HasOption("offset") || tag.HasOption("count")))
            _screenEffect.SetGhostShape(ParseOffset(tag.GetOption("offset")), (int)tag.GetFloat("count", 0f));

        switch (tag.Value)
        {
            case "on":
                _screenEffect.SetEffect(kind, true, strength, duration);
                break;

            case "off":
                _screenEffect.SetEffect(kind, false, strength, duration);
                break;

            case "pulse":
                _screenEffect.PulseEffect(kind, strength, duration);

                if (!tag.HasOption("nowait"))
                    _waitForScreenEffect = true;

                return;

            default:
                Debug.LogWarning($"{tag.Command} 只接受 on / off / pulse，收到：{tag.Value}");
                return;
        }

        // 持續狀態預設不擋對話，要等就明寫 wait
        if (tag.HasOption("wait"))
            _waitForScreenEffect = true;
    }

    /// <summary>offset:20 兩軸相同；offset:20,6 分別是 X 與 Y。</summary>
    static Vector2? ParseOffset(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        string[] parts = value.Split(',');

        if (!float.TryParse(parts[0], out float x))
        {
            Debug.LogWarning($"看不懂的 offset：{value}");
            return null;
        }

        float y = x;

        if (parts.Length > 1 && !float.TryParse(parts[1], out y))
            y = x;

        return new Vector2(x, y);
    }

    static Color? ParseColor(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        switch (value.ToLowerInvariant())
        {
            case "black": return Color.black;
            case "white": return Color.white;
            case "red": return Color.red;
        }

        if (ColorUtility.TryParseHtmlString(value, out Color parsed))
            return parsed;

        Debug.LogWarning($"看不懂的顏色：{value}");
        return null;
    }
}
