#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PhilSorter.Localization
{
    public sealed class PhilSorterL10n
    {
        private readonly Func<string> _localizationDirectoryPathProvider;
        private readonly string _defaultLocale;
        private readonly string _prefsKey;
        
        private Dictionary<string, string> _strings = new Dictionary<string, string>();
        private string _currentLocale;
        private bool _loaded = false;
        private static readonly string[] SupportedLanguageCodes = { "en", "ja" };
        private static readonly string[] SupportedLanguageLabels = { "English", "日本語" };

        public PhilSorterL10n(Func<string> localizationDirectoryPathProvider, string defaultLocale, string prefsKey)
        {
            _localizationDirectoryPathProvider = localizationDirectoryPathProvider;
            _defaultLocale = defaultLocale;
            _prefsKey = prefsKey;
            _currentLocale = EditorPrefs.GetString(_prefsKey, _defaultLocale);
            LoadStrings();
        }

        public string CurrentLocale
        {
            get => _currentLocale;
            set
            {
                if (_currentLocale != value && SupportedLanguageCodes.Contains(value))
                {
                    _currentLocale = value;
                    _loaded = false;
                    EditorPrefs.SetString(_prefsKey, value);
                    LoadStrings();
                }
            }
        }

        public string Tr(string key)
        {
            if (!_loaded) LoadStrings();
            
            if (_strings.TryGetValue(key, out var value))
            {
                return value;
            }
            
            // Fallback: return the key itself if not found
            return key;
        }

        public string TryTr(string key)
        {
            if (!_loaded) LoadStrings();
            
            _strings.TryGetValue(key, out var value);
            return value;
        }

        public void DrawLanguagePicker()
        {
            int currentIndex = Array.IndexOf(SupportedLanguageCodes, _currentLocale);
            if (currentIndex < 0) currentIndex = 0;
            
            var newIndex = EditorGUILayout.Popup("Language", currentIndex, SupportedLanguageLabels);
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < SupportedLanguageCodes.Length)
            {
                CurrentLocale = SupportedLanguageCodes[newIndex];
            }
        }

        private void LoadStrings()
        {
            _strings.Clear();
            
            var localizationDirectoryPath = _localizationDirectoryPathProvider();
            Debug.Log($"[PhilSorter] Attempting to load localization from: {localizationDirectoryPath}");
            
            var currentPoFile = Path.Combine(localizationDirectoryPath, $"{_currentLocale}.po");
            Debug.Log($"[PhilSorter] Looking for localization file: {currentPoFile}");
            
            if (File.Exists(currentPoFile))
            {
                Debug.Log($"[PhilSorter] Loading localization file: {currentPoFile}");
                LoadPoFile(currentPoFile);
            }
            else if (_currentLocale != _defaultLocale)
            {
                // Fallback to default locale
                var defaultPoFile = Path.Combine(localizationDirectoryPath, $"{_defaultLocale}.po");
                Debug.Log($"[PhilSorter] Fallback to default locale file: {defaultPoFile}");
                if (File.Exists(defaultPoFile))
                {
                    LoadPoFile(defaultPoFile);
                }
                else
                {
                    Debug.LogWarning($"[PhilSorter] Could not find localization files in: {localizationDirectoryPath}");
                }
            }
            else
            {
                Debug.LogWarning($"[PhilSorter] Could not find localization file: {currentPoFile}");
            }
            
            Debug.Log($"[PhilSorter] Loaded {_strings.Count} localization strings");
            _loaded = true;
        }

        private void LoadPoFile(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                string currentMsgId = null;
                string currentMsgStr = null;
                bool inMsgId = false;
                bool inMsgStr = false;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    if (trimmedLine.StartsWith("msgid "))
                    {
                        // Save previous entry if we have one
                        if (!string.IsNullOrEmpty(currentMsgId) && !string.IsNullOrEmpty(currentMsgStr))
                        {
                            _strings[currentMsgId] = currentMsgStr.Replace("\\n", "\n");
                        }

                        // Start new msgid
                        currentMsgId = ExtractQuotedString(trimmedLine.Substring(6));
                        currentMsgStr = null;
                        inMsgId = true;
                        inMsgStr = false;
                    }
                    else if (trimmedLine.StartsWith("msgstr "))
                    {
                        currentMsgStr = ExtractQuotedString(trimmedLine.Substring(7));
                        inMsgId = false;
                        inMsgStr = true;
                    }
                    else if (trimmedLine.StartsWith("\"") && trimmedLine.EndsWith("\""))
                    {
                        // Continuation line
                        var content = ExtractQuotedString(trimmedLine);
                        if (inMsgId)
                        {
                            currentMsgId += content;
                        }
                        else if (inMsgStr)
                        {
                            currentMsgStr += content;
                        }
                    }
                }

                // Save the last entry
                if (!string.IsNullOrEmpty(currentMsgId) && !string.IsNullOrEmpty(currentMsgStr))
                {
                    _strings[currentMsgId] = currentMsgStr.Replace("\\n", "\n");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhilSorter] Failed to load localization file {filePath}: {e.Message}");
            }
        }

        private static string ExtractQuotedString(string input)
        {
            var trimmed = input.Trim();
            if (trimmed.StartsWith("\"") && trimmed.EndsWith("\"") && trimmed.Length >= 2)
            {
                return trimmed.Substring(1, trimmed.Length - 2);
            }
            return trimmed;
        }
    }
}
#endif
