using CompiladorQuechua.Models;

namespace CompiladorQuechua.Services
{
    /// <summary>
    /// Contrato para el servicio de traducción español → quechua boliviano.
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>Traduce texto español a quechua y retorna una entrada de historial.</summary>
        /// <param name="spanishText">Texto en español a traducir.</param>
        /// <returns>Entrada con el resultado de la traducción.</returns>
        TranslationEntry Translate(string spanishText);

        /// <summary>Total acumulado de palabras traducidas en la sesión actual.</summary>
        int TotalWordsTranslated { get; }
    }
}
