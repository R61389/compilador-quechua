using System;
using CompiladorQuechua.Models;

namespace CompiladorQuechua.Services
{
    /// <summary>
    /// Servicio de traducción que orquesta el motor gramatical quechua.
    /// Integra el pipeline del compilador: análisis léxico → traducción → resultado.
    /// </summary>
    public class TranslationService : ITranslationService
    {
        private readonly IQuechuaGrammarEngine _grammarEngine;
        private int _totalWordsTranslated;

        /// <inheritdoc/>
        public int TotalWordsTranslated => _totalWordsTranslated;

        /// <summary>Crea una instancia del servicio con el motor gramatical indicado.</summary>
        /// <param name="grammarEngine">Motor gramatical quechua. No puede ser null.</param>
        public TranslationService(IQuechuaGrammarEngine grammarEngine)
        {
            _grammarEngine = grammarEngine ?? throw new ArgumentNullException(nameof(grammarEngine));
        }

        /// <inheritdoc/>
        public TranslationEntry Translate(string spanishText)
        {
            if (string.IsNullOrWhiteSpace(spanishText))
                return new TranslationEntry { SpanishText = string.Empty, QuechuaText = string.Empty };

            var quechuaText = _grammarEngine.Translate(spanishText.Trim());
            var wordCount = _grammarEngine.CountTranslatedWords(quechuaText);
            _totalWordsTranslated += wordCount;

            return new TranslationEntry
            {
                Timestamp = DateTime.Now,
                SpanishText = spanishText,
                QuechuaText = quechuaText,
                WordCount = wordCount
            };
        }
    }
}
