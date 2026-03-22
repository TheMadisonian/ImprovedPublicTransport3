using System.Collections.Generic;

namespace ImprovedPublicTransport.TranslationFramework
{
    public interface ILanguage
    {
        bool HasTranslation(string id);

        string GetTranslation(string id);

        string LocaleName();
    }
}