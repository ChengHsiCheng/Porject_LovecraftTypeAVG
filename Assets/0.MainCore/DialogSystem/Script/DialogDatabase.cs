using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 劇本用的查表資料。ink 只寫 key，實際的名字與圖片在這裡對應。
/// </summary>
[CreateAssetMenu(fileName = "DialogDatabase", menuName = "Dialog/Dialog Database")]
public class DialogDatabase : ScriptableObject
{
    [TableList] [SerializeField] private List<CharacterEntry> _characters = new List<CharacterEntry>();
    [TableList] [SerializeField] private List<BackgroundEntry> _backgrounds = new List<BackgroundEntry>();

    [LabelText("立繪（key = 角色_表情）")]
    [TableList] [SerializeField] private List<SpriteEntry> _sprites = new List<SpriteEntry>();

    public bool TryGetCharacterName(string key, out string displayName)
    {
        displayName = string.Empty;

        if (string.IsNullOrEmpty(key)) return false;

        foreach (CharacterEntry entry in _characters)
        {
            if (entry == null || entry.Key != key) continue;

            displayName = entry.DisplayName;
            return true;
        }

        return false;
    }

    public bool TryGetBackground(string key, out Sprite sprite)
    {
        sprite = null;

        if (string.IsNullOrEmpty(key)) return false;

        foreach (BackgroundEntry entry in _backgrounds)
        {
            if (entry == null || entry.Key != key) continue;

            sprite = entry.Sprite;
            return true;
        }

        return false;
    }

    /// <param name="key">角色_表情，例 momoka_smile</param>
    public bool TryGetSprite(string key, out Sprite sprite)
    {
        sprite = null;

        if (string.IsNullOrEmpty(key)) return false;

        foreach (SpriteEntry entry in _sprites)
        {
            if (entry == null || entry.Key != key) continue;

            sprite = entry.Sprite;
            return true;
        }

        return false;
    }

    [Serializable]
    public class SpriteEntry
    {
        [field: SerializeField] public string Key { get; private set; }
        [field: SerializeField] [field: PreviewField] public Sprite Sprite { get; private set; }
    }

    [Serializable]
    public class CharacterEntry
    {
        [field: SerializeField] public string Key { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
    }

    [Serializable]
    public class BackgroundEntry
    {
        [field: SerializeField] public string Key { get; private set; }
        [field: SerializeField] [field: PreviewField] public Sprite Sprite { get; private set; }
    }
}
