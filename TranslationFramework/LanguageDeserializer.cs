using System.IO;

namespace ImprovedPublicTransport.TranslationFramework
{
    public interface ILanguageDeserializer
    {
        ILanguage DeserialiseLanguage(string fileName);
    }
}