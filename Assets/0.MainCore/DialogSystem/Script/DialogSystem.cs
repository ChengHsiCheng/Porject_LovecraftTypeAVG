using System.Collections;
using Ink.Runtime;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogSystem : MonoBehaviour
{
    [BoxGroup("參照")] [LabelText("ink 劇本（.json）")] [Required]
    [Tooltip("要拖編譯後的 .json，不是 .ink")]
    [SerializeField] private TextAsset _inkAsset;

    [BoxGroup("參照")] [LabelText("對話文字")] [Required] [SerializeField]
    private TMP_Text _dialogueText;

    [BoxGroup("參照")] [LabelText("指令分派器")] [Required] [SerializeField]
    private TagDispatcher _tagDispatcher;

    [BoxGroup("文字")] [LabelText("每個字的間隔")] [SuffixLabel("秒", true)]
    [Range(0f, 0.3f)] [SerializeField]
    private float _typeSpeed = 0.03f;

    [BoxGroup("輸入")] [LabelText("按住 Ctrl 快轉的間隔")] [SuffixLabel("秒", true)]
    [Tooltip("等同於每隔這麼久自動點一次")] [SerializeField]
    private float _autoAdvanceInterval = 0.2f;

    [FoldoutGroup("進階")] [LabelText("連續指令上限")]
    [Tooltip("連續這麼多行 @ 指令都沒遇到文字就中斷，避免無限迴圈")] [SerializeField]
    private int _maxCommandsPerStep = 50;

    private Story _inkStory;
    private bool _onTyping;
    private bool _skip;
    private bool _advancing;
    private float _autoAdvanceTimer;

    void Start()
    {
        ResetInkStory();
    }

    void Update()
    {
        if (IsAdvancePressed())
        {
            NextDialog();
            _autoAdvanceTimer = _autoAdvanceInterval;
            return;
        }

        if (!IsFastForwardHeld() || _inkStory == null || !_inkStory.canContinue)
        {
            // 放開後把計時器補滿，下次按住時立刻觸發第一次
            _autoAdvanceTimer = _autoAdvanceInterval;
            return;
        }

        _autoAdvanceTimer += Time.deltaTime;

        if (_autoAdvanceTimer >= _autoAdvanceInterval)
        {
            _autoAdvanceTimer = 0f;
            NextDialog();
        }
    }

    /// <summary>按住 Ctrl 自動推進。</summary>
    bool IsFastForwardHeld()
    {
        var keyboard = Keyboard.current;

        if (keyboard == null)
            return false;

        return keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
    }

    bool IsAdvancePressed()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;

        return false;
    }

    [Button]
    void ResetInkStory()
    {
        _inkStory = new Story(_inkAsset.text);
        _dialogueText.text = string.Empty;
        _onTyping = false;
        _skip = false;

        // NextDialog();
    }

    /// <summary>清空對話框文字（@clear）。</summary>
    public void ClearDialogueText()
    {
        if (_dialogueText == null) return;

        _dialogueText.text = string.Empty;
        _dialogueText.maxVisibleCharacters = 0;
    }

    [Button]
    void NextDialog()
    {
        // 打字中再按一次 = 直接全部顯示
        if (_onTyping)
        {
            _skip = true;
            return;
        }

        if (_advancing) return; // 指令執行中，忽略輸入

        if (!_inkStory.canContinue)
        {
            Debug.Log("對話結束");
            return;
        }

        StartCoroutine(AdvanceRoutine());
    }

    /// <summary>
    /// 推進到下一句「文字」。
    /// 途中的 @ 指令會依序執行完（不需要點擊），會等待的指令跑完才往下。
    /// </summary>
    IEnumerator AdvanceRoutine()
    {
        _advancing = true;

        int commandCount = 0;

        while (true)
        {
            string line = _inkStory.Continue().Trim();

            if (_tagDispatcher != null)
                _tagDispatcher.HandleTags(_inkStory.currentTags);

            bool isCommand = TagDispatcher.IsCommandLine(line);

            if (!isCommand && line.Length > 0)
            {
                _advancing = false;
                yield return PlayDialogText(line);
                yield break;
            }

            if (isCommand)
            {
                if (_tagDispatcher != null)
                {
                    _tagDispatcher.ExecuteCommand(line);

                    while (_tagDispatcher.IsBusy)
                        yield return null;
                }

                commandCount++;

                if (commandCount >= _maxCommandsPerStep)
                {
                    Debug.LogError($"連續 {_maxCommandsPerStep} 行指令都沒有文字，中斷以免無限迴圈。");
                    break;
                }
            }

            if (!_inkStory.canContinue)
            {
                Debug.Log("對話結束");
                break;
            }
        }

        _advancing = false;
    }

    IEnumerator PlayDialogText(string text)
    {
        _onTyping = true;
        _skip = false;

        _dialogueText.text = text;
        _dialogueText.maxVisibleCharacters = 0;
        _dialogueText.ForceMeshUpdate(); // 先更新，才拿得到正確的字元數

        // 等演出（例如淡入淡出）跑完才開始打字；打字中的跳過鍵也能跳過這段等待
        while (_tagDispatcher != null && _tagDispatcher.IsBusy && !_skip)
            yield return null;

        int totalVisibleCharacters = _dialogueText.textInfo.characterCount;

        for (int i = 1; i <= totalVisibleCharacters; i++)
        {
            if (_skip)
                break;

            _dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(_typeSpeed);
        }

        _dialogueText.maxVisibleCharacters = totalVisibleCharacters;
        _onTyping = false;
        _skip = false;
    }
}
