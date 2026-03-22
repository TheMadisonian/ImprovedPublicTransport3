using ImprovedPublicTransport.LanguageFormat;
using ImprovedPublicTransport.TranslationFramework;

namespace ImprovedPublicTransport
{
    public static class Localization
    {
        private static readonly LocalizationManager LocalizationManager = 
            new LocalizationManager(typeof(ImprovedPublicTransportMod), new PlainTextLanguageDeserializer());

        public static string Get(string translationId)
        {
                // First try the mod's localization manager (most reliable for mod keys)
            try
            {
                LocalizationManager.EnsureFallbackLanguageLoaded();
                var translated = LocalizationManager.GetTranslation(translationId);
                if (translated != translationId)
                    return translated;
            }
            catch { }

            // Then try Colossal's built-in locale
            try
            {
                var c = ColossalFramework.Globalization.Locale.Get(translationId);
                if (c != translationId)
                    return c;
            }
            catch { }

            // Last-resort: try reading directly from Locale/en.txt
            try
            {
                var fileTranslated = LocalizationManager.TryGetTranslationFromLocaleFile(translationId);
                if (!string.IsNullOrEmpty(fileTranslated))
                {
                    ImprovedPublicTransport.Util.Utils.LogWarning($"Loaded fallback translation for '{translationId}' from en.txt");
                    return fileTranslated;
                }
            }
            catch { }

            ImprovedPublicTransport.Util.Utils.LogWarning($"Missing translation for '{translationId}'");
            return translationId;
        }
    }
}