using UnityEngine;
using PhilSorter.Localization;

namespace PhilSorter
{
    internal static class L10n
    {
        private const string PreferenceKey = "com.philslab.philsorter.lang";
        private const string LocalizationPath = "Packages/com.philslab.philsorter/Localization";

        public static PhilSorterL10n Localization { get; } = new PhilSorterL10n(LocalizationPath, "en", PreferenceKey);

        private static GUIContent tempContent;

        public static GUIContent Tr(string localizationKey)
            => TempContent(Localization.Tr(localizationKey));

        public static GUIContent Tr(string localizationKey, string fallback)
            => TempContent(Localization.TryTr(localizationKey) ?? fallback);

        public static string TrStr(string localizationKey) 
            => Localization.Tr(localizationKey);

        public static string TrStr(string localizationKey, string fallback)
            => Localization.TryTr(localizationKey) ?? fallback;

        public static GUIContent TempContent(string text)
        {
            if (tempContent == null)
            {
                tempContent = new GUIContent(text);
            }
            else
            {
                tempContent.text = text;
            }
            tempContent.image = null;
            tempContent.tooltip = null;
            return tempContent;
        }

        public static void DrawLanguagePicker()
        {
            Localization.DrawLanguagePicker();
        }
    }
}
