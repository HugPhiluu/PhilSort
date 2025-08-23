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

    private static string[] supportedLanguages;
    private static string[] supportedLanguageLabels;
    private static bool languagesScanned = false;

    public static string[] SupportedLanguages {
        get {
            EnsureLanguagesScanned();
            return supportedLanguages;
        }
    }
    public static string[] SupportedLanguageLabels {
        get {
            EnsureLanguagesScanned();
            return supportedLanguageLabels;
        }
    }
    public static string CurrentLanguage => currentLanguage;

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
        EnsureLanguagesScanned();
        string locPath = $"Packages/com.philslab.philsorter/Editor/Localization/{currentLanguage}.json";
        if (!File.Exists(locPath))
        {
            // fallback to en.json or first available
            locPath = $"Packages/com.philslab.philsorter/Editor/Localization/en.json";
            if (!File.Exists(locPath) && supportedLanguages != null && supportedLanguages.Length > 0)
            {
                locPath = $"Packages/com.philslab.philsorter/Editor/Localization/{supportedLanguages[0]}.json";
            }
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

    private static void EnsureLanguagesScanned()
    {
        if (languagesScanned) return;
        string locDir = "Packages/com.philslab.philsorter/Editor/Localization";
        if (!Directory.Exists(locDir))
        {
            supportedLanguages = new string[] { "en" };
            supportedLanguageLabels = new string[] { "English" };
            languagesScanned = true;
            return;
        }
        var files = Directory.GetFiles(locDir, "*.json");
        var langs = new List<string>();
        var labels = new List<string>();
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            langs.Add(name);
            labels.Add(GetLanguageLabel(name));
        }
        supportedLanguages = langs.ToArray();
        supportedLanguageLabels = labels.ToArray();
        languagesScanned = true;
    }

    private static string GetLanguageLabel(string code)
    {
        // Add more as needed
        switch (code)
        {
            case "en": return "English";
            case "ja": return "日本語";
            case "fr": return "Français";
            case "de": return "Deutsch";
            case "es": return "Español";
            case "zh": return "中文";
            case "ru": return "Русский";
            case "ko": return "한국어";
            default: return code;
        }
    }
}
