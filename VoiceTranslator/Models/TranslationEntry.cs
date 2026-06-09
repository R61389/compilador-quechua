using System;

namespace CompiladorQuechua.Models
{
    /// <summary>Entrada en el historial de traducciones voz → quechua.</summary>
    public class TranslationEntry
    {
        /// <summary>Marca de tiempo cuando se realizó la traducción.</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>Texto original reconocido en español.</summary>
        public string SpanishText { get; set; } = string.Empty;

        /// <summary>Texto traducido al quechua boliviano.</summary>
        public string QuechuaText { get; set; } = string.Empty;

        /// <summary>Número de palabras en el texto traducido.</summary>
        public int WordCount { get; set; }

        /// <summary>Nivel de confianza del reconocimiento de voz (0.0 - 1.0).</summary>
        public double ConfidenceScore { get; set; }

        /// <summary>Representación legible de la entrada de traducción.</summary>
        public override string ToString() =>
            $"[{Timestamp:HH:mm:ss}] ES: {SpanishText} → QU: {QuechuaText}";
    }
}
