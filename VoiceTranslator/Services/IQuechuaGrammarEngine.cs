namespace CompiladorQuechua.Services
{
    /// <summary>
    /// Contrato para el motor gramatical de traducción español → quechua boliviano.
    /// </summary>
    public interface IQuechuaGrammarEngine
    {
        /// <summary>Traduce un texto completo del español al quechua boliviano.</summary>
        /// <param name="spanishText">Texto en español a traducir.</param>
        /// <returns>Texto traducido al quechua boliviano.</returns>
        string Translate(string spanishText);

        /// <summary>Traduce una palabra individual del español al quechua.</summary>
        /// <param name="spanishWord">Palabra en español.</param>
        /// <returns>Palabra en quechua, o la original con [?] si no se encontró.</returns>
        string TranslateWord(string spanishWord);

        /// <summary>Aplica reglas gramaticales del quechua al texto ya traducido.</summary>
        /// <param name="translated">Texto ya traducido palabra por palabra.</param>
        /// <param name="originalSpanish">Texto original en español para contexto gramatical.</param>
        /// <returns>Texto con reglas gramaticales quechua aplicadas.</returns>
        string ApplyGrammarRules(string translated, string originalSpanish);

        /// <summary>Cuenta las palabras efectivamente traducidas en un texto quechua.</summary>
        /// <param name="text">Texto en quechua.</param>
        /// <returns>Número de palabras en el texto.</returns>
        int CountTranslatedWords(string text);
    }
}
