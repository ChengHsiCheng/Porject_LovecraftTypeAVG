using System;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DialogSystem : MonoBehaviour
{
    [SerializeField] private TextAsset _inkAsset;
    private Story _inkStory;

    [FoldoutGroup("GameObject")] [SerializeField]
    private TMP_Text _dialogueText;

    [FoldoutGroup("GameObject")] [SerializeField]
    private TMP_Text _nameText;

    [FoldoutGroup("GameObject")] [SerializeField]
    private Image _characterImage;

    [SerializeField] DialogChoiceButton _choiceButton;
    [SerializeField] GameObject _choicePanel;

    [SerializeField] private float _typeSpeed = 0.05f; // 調快一點，0.2 體感較慢

    private List<DialogChoiceButton> _choices = new List<DialogChoiceButton>();
    private bool _onTyping;
    private bool _skip;
    private Coroutine _typingCoroutine; // 儲存協程引用

    [SerializeField] private List<DialogCharacterData> _characterData;

    void Awake()
    {
        _inkStory = new Story(_inkAsset.text);

        _inkStory.BindExternalFunction("SetInterfere", (bool value) => { Debug.Log("SetInterfere : " + value); });
        _inkStory.BindExternalFunction("CheckUsedItem",
            (string itemName) =>
            {
                Debug.Log("CheckUsedItem : " + itemName);
                
                if (itemName == "FireBall")
                    return true;

                return false;
            });
    }

    [Button]
    void ResetInkStory()
    {
        _inkStory = new Story(_inkAsset.text);
    }

    [Button]
    void NextDialog()
    {
        if (_onTyping)
        {
            _skip = true;
            return;
        }

        // 如果目前有選項，必須先選選項，不能直接下一步
        if (_choices.Count > 0) return;

        if (!_inkStory.canContinue)
        {
            if (_inkStory.currentChoices.Count > 0)
                CreatChoices(_inkStory);
            return;
        }

        string text = _inkStory.Continue();
        HandleTags(_inkStory.currentTags);

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(PlayDialogText(text));
    }

    public IEnumerator PlayDialogText(string text)
    {
        _onTyping = true;
        _skip = false;

        _dialogueText.text = text;
        _dialogueText.maxVisibleCharacters = 0;
        _dialogueText.ForceMeshUpdate(); // 重要：確保文字資訊已更新

        // 使用 TMP 的字元統計，這會排除標籤代碼
        int totalVisibleCharacters = _dialogueText.textInfo.characterCount;

        for (int i = 0; i <= totalVisibleCharacters; i++)
        {
            if (_skip)
            {
                _dialogueText.maxVisibleCharacters = totalVisibleCharacters;
                break;
            }

            _dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(_typeSpeed);
        }

        _onTyping = false;
        _skip = false;
        _typingCoroutine = null;
    }

    void CreatChoices(Story story)
    {
        _choicePanel.SetActive(true);
        for (int i = 0; i < story.currentChoices.Count; ++i)
        {
            Choice choice = story.currentChoices[i];
            var button = Instantiate(_choiceButton, _choicePanel.transform);
            button.SetButton(choice.text, i);
            button.OnClick += Choices;
            button.gameObject.SetActive(true);
            _choices.Add(button);
        }
    }

    void Choices(int index)
    {
        _inkStory.ChooseChoiceIndex(index);

        // 倒序刪除子物件
        for (int i = _choices.Count - 1; i >= 0; i--)
        {
            _choices[i].OnClick -= Choices;
            Destroy(_choices[i].gameObject);
        }

        _choices.Clear();
        _choicePanel.SetActive(false);

        NextDialog();
    }

    void HandleTags(List<string> tags)
    {
        foreach (string tag in tags)
        {
            string key = tag.Trim();
            var data = _characterData.FirstOrDefault(d => d.Key == key);

            if (data == null)
            {
                _nameText.gameObject.SetActive(false);
                _characterImage.gameObject.SetActive(false);
                return;
            }

            if (String.IsNullOrEmpty(data.CharacterName))
            {
                _nameText.gameObject.SetActive(false);
            }
            else
            {
                _nameText.gameObject.SetActive(true);
                _nameText.text = data.CharacterName;
            }

            if (data.CharacterSprite == null)
            {
                _characterImage.gameObject.SetActive(false);
            }
            else
            {
                _characterImage.gameObject.SetActive(true);
                _characterImage.sprite = data.CharacterSprite;
            }
        }
    }
}

[System.Serializable]
internal class DialogCharacterData
{
    [field: SerializeField] public string Key { get; private set; }
    [field: SerializeField] public string CharacterName { get; private set; }
    [field: SerializeField] public Sprite CharacterSprite { get; private set; }
}