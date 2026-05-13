using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogChoiceButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _text;

    private int _choicesIndex;

    public event Action<int> OnClick;

    public void SetButton(string text, int choicesIndex)
    {
        _text.text = text;
        _choicesIndex = choicesIndex;
    }

    public void OnButtonClick()
    {
        OnClick?.Invoke(_choicesIndex);
    }
}
