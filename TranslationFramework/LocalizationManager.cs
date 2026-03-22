using System;
using System.Collections.Generic;
using System.IO;
using ColossalFramework.Globalization;

namespace ImprovedPublicTransport.TranslationFramework
{
    /// <summary>
    /// Handles localisation for a mod.
    /// </summary>
    public class LocalizationManager
    {
        protected List<ILanguage> _languages = new List<ILanguage>();
        protected ILanguage _currentLanguage = null;
        protected bool _languagesLoaded = false;
        protected bool _loadLanguageAutomatically = true;
        private string fallbackLanguage;
        private ILanguageDeserializer languageDeserializer;
        private Type modType;

        public LocalizationManager(Type modType, ILanguageDeserializer languageDeserializer = null, bool loadLanguageAutomatically = true, 
            string fallbackLanguage = "en")
        {
            this.languageDeserializer = languageDeserializer ?? new DefaultLanguageDeserializer();
            this._loadLanguageAutomatically = loadLanguageAutomatically;
            this.fallbackLanguage = fallbackLanguage;
            this.modType = modType;
            LocaleManager.eventLocaleChanged += SetCurrentLanguage;
        }

        private void SetCurrentLanguage()
        {
            if (_languages == null || _languages.Count ==0 || !LocaleManager.exists)
            {
                return;
            }
            _currentLanguage = _languages.Find(l => l.LocaleName() == LocaleManager.instance.language) ??
                               _languages.Find(l => l.LocaleName() == fallbackLanguage);
        }


        /// <summary>
        /// Loads all languages up if not already loaded.
        /// </summary>
        public void LoadLanguages()
        {
            if (!_languagesLoaded && _loadLanguageAutomatically)
            {
                _languagesLoaded = true;
                RefreshLanguages();
                SetCurrentLanguage();
            }
        }

        /// <summary>
        /// Forces a reload of the languages, even if they're already loaded
        /// </summary>
        public void RefreshLanguages()
        {
            _languages.Clear();

            string basePath = Util.AssemblyPath(modType);

            // Only log debug info if verbose logging enabled
            bool verboseLogging = ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs;
            if (verboseLogging)
            {
                try
                {
                    ImprovedPublicTransport.Util.Utils.LogWarning($"LocalizationManager: basePath='{basePath}'");
                }
                catch { }
            }

            if (basePath != "")
            {
                string languagePath = basePath + System.IO.Path.DirectorySeparatorChar + "Translations";
                if (verboseLogging)
                {
                    try
                    {
                        ImprovedPublicTransport.Util.Utils.LogWarning($"LocalizationManager: languagePath='{languagePath}' Exists={System.IO.Directory.Exists(languagePath)}");
                    }
                    catch { }
                }

                if (Directory.Exists(languagePath))
                {
                    string[] languageFiles = Directory.GetFiles(languagePath, "*.txt", SearchOption.AllDirectories);
                    if (verboseLogging)
                    {
                        try
                        {
                            ImprovedPublicTransport.Util.Utils.LogWarning($"LocalizationManager: found {languageFiles.Length} language files");
                        }
                        catch { }
                    }

                    foreach (string languageFile in languageFiles)
                    {
                        ILanguage loadedLanguage = null;
                        try
                        {
                            loadedLanguage = languageDeserializer.DeserialiseLanguage(languageFile);
                            if (verboseLogging)
                            {
                                ImprovedPublicTransport.Util.Utils.LogWarning($"LocalizationManager: Loading localization file: {languageFile}. Detected locale name: {loadedLanguage?.LocaleName()}");
                            }
                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.LogError(
                                "Error happened when deserializing language file " + languageFile);
                            UnityEngine.Debug.LogException(e);
                        }
                        if (loadedLanguage != null)
                        {
                            _languages.Add(loadedLanguage);
                        }
                    }

                    if (verboseLogging)
                    {
                        try
                        {
                            ImprovedPublicTransport.Util.Utils.LogWarning($"LocalizationManager: loaded {_languages.Count} languages");
                        }
                        catch { }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Can't load any localisation files");
                }
            }
        }

        /// <summary>
        /// Ensure that a fallback language is available and selected if no current language has been set yet.
        /// This is useful when localization is requested early in the UI lifecycle before Colossal's LocaleManager
        /// has become available; it forces the manager to load language files and select the fallback (usually 'en').
        /// </summary>
        public void EnsureFallbackLanguageLoaded()
        {
            // Only load once - _languagesLoaded flag prevents repeated reloads
            if (!_languagesLoaded)
            {
                LoadLanguages();
            }

            // If no current language has been selected yet, choose the fallback language if present,
            // otherwise pick the first available language.
            if (_currentLanguage == null && _languages != null && _languages.Count > 0)
            {
                _currentLanguage = _languages.Find(l => l.LocaleName() == fallbackLanguage) ?? _languages[0];
            }
        }
        
        /// <summary>
        /// Returns whether you can translate into a specific translation ID
        /// </summary>
        /// <param name="translationId">The ID of the translation to check</param>
        /// <returns>Whether a translation into this ID is possible</returns>
        private bool HasTranslation(string translationId)
        {
            LoadLanguages();

            if (translationId == null)
            {
                return true;
            }

            return _currentLanguage != null && _currentLanguage.HasTranslation(translationId);
        }

        /// <summary>
        /// Gets a translation for a specific translation ID
        /// </summary>
        /// <param name="translationId">The ID to return the translation for</param>
        /// <returns>A translation of the translationId</returns>
        public string GetTranslation(string translationId)
        {
            LoadLanguages();

            if (translationId == null)
            {
                return "null";
            }

            string translatedText = translationId;

            if (_currentLanguage != null)
            {
                if (HasTranslation(translationId))
                {
                    translatedText = _currentLanguage.GetTranslation(translationId);
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Returned translation for language \"" + _currentLanguage.LocaleName() + "\" doesn't contain a suitable translation for \"" + translationId + "\"");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Can't get a translation for \"" + translationId + "\" as there is not a language defined");
            }

            return translatedText;
        }

        /// <summary>
        /// Try to read a translation directly from a Locale file (e.g., Locale/en.txt).
        /// This is a last-resort fallback for cases where the language manager hasn't
        /// selected a language or parsing failed earlier.
        /// </summary>
        public string TryGetTranslationFromLocaleFile(string translationId)
        {
            try
            {
                string basePath = Util.AssemblyPath(modType);
                if (string.IsNullOrEmpty(basePath)) return null;
                string enPath = System.IO.Path.Combine(basePath, "Translations");
                enPath = System.IO.Path.Combine(enPath, "en.txt");
                if (!System.IO.File.Exists(enPath)) return null;
                foreach (string raw in System.IO.File.ReadAllLines(enPath))
                {
                    if (raw == null) continue;
                    var str = raw.Trim();
                    if (str.Length == 0) continue;
                    int idx = str.IndexOf(' ');
                    if (idx <= 0) continue;
                    var key = str.Substring(0, idx);
                    if (key == translationId)
                    {
                        var val = str.Substring(idx + 1).Replace("\\n", "\n");
                        return val;
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("LocalizationManager: fallback read failed: " + e.Message);
            }
            return null;
        }
    }
}