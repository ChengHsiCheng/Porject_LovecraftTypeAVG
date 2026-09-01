using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// ink tag 的解析結果。
/// 格式：指令:值 選項:值 旗標
/// 例：fade:out dur:1.2 nowait
/// </summary>
public struct TagCommand
{
    public string Command;
    public string Value;
    public Dictionary<string, string> Options;

    public static TagCommand Parse(string raw)
    {
        TagCommand result = new TagCommand
        {
            Command = string.Empty,
            Value = string.Empty,
            Options = new Dictionary<string, string>()
        };

        if (string.IsNullOrWhiteSpace(raw))
            return result;

        List<string> tokens = SplitTokens(raw.Trim());
        bool isFirst = true;

        foreach (string token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token)) continue;

            SplitToken(token, out string key, out string value);

            if (isFirst)
            {
                result.Command = key;
                result.Value = value;
                isFirst = false;
            }
            else
            {
                result.Options[key] = value; // 沒有值的旗標（例如 nowait）值會是空字串
            }
        }

        return result;
    }

    /// <summary>用空白切開，但引號內的空白不切（例：fx:"滴滴 滴滴" anim:rise）。</summary>
    static List<string> SplitTokens(string raw)
    {
        List<string> tokens = new List<string>();
        System.Text.StringBuilder current = new System.Text.StringBuilder();
        bool inQuote = false;

        foreach (char c in raw)
        {
            if (c == '"')
            {
                inQuote = !inQuote;
                continue;
            }

            if (!inQuote && (c == ' ' || c == '\t'))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }

    static void SplitToken(string token, out string key, out string value)
    {
        int index = token.IndexOf(':');

        if (index < 0)
        {
            key = token.Trim().ToLowerInvariant();
            value = string.Empty;
            return;
        }

        key = token.Substring(0, index).Trim().ToLowerInvariant();
        value = token.Substring(index + 1).Trim();
    }

    public bool HasOption(string key)
    {
        return Options != null && Options.ContainsKey(key);
    }

    public string GetOption(string key, string defaultValue = "")
    {
        if (Options != null && Options.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value))
            return value;

        return defaultValue;
    }

    public float GetFloat(string key, float defaultValue)
    {
        string raw = GetOption(key);

        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            return value;

        return defaultValue;
    }
}
