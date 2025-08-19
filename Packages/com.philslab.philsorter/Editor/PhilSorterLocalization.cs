using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public static class PhilSorterLocalization
{
    private static Dictionary<string, string> strings = new Dictionary<string, string>();
    private static string currentLanguage = "en";
    private static bool loaded = false;

    public static void SetLanguage(string lang)
    {
        if (lang != currentLanguage)
        {
            currentLanguage = lang;
            loaded = false;
            LoadStrings();
        }
    }

    public static string Get(string key, params object[] args)
    {
        if (!loaded) LoadStrings();
        if (strings.TryGetValue(key, out var value))
        {
            if (args != null && args.Length > 0)
                return string.Format(value, args);
            return value;
        }
        return key;
    }

    private static void LoadStrings()
    {
        strings.Clear();
        string locPath = $"Packages/com.philslab.philsorter/Editor/Localization/{currentLanguage}.json";
        if (!File.Exists(locPath))
        {
            locPath = $"Packages/com.philslab.philsorter/Editor/Localization/en.json";
        }
        if (File.Exists(locPath))
        {
            var json = File.ReadAllText(locPath);
            // Simple regex to parse flat key-value JSON (no nesting)
            var matches = Regex.Matches(json, @"""(.*?)""\s*:\s*""(.*?)""");
            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    strings[match.Groups[1].Value] = match.Groups[2].Value.Replace("\\n", "\n");
                }
            }
        }
        loaded = true;
    }

    public static string[] SupportedLanguages = new string[] { "en", "ja" };
    public static string[] SupportedLanguageLabels = new string[] { "English", "日本語" };
    public static string CurrentLanguage => currentLanguage;
}
