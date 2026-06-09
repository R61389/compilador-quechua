using System;

namespace CompiladorQuechua.Services
{
    /// <summary>
    /// Contrato para el servicio de reconocimiento de voz continuo en español.
    /// </summary>
    public interface ISpeechRecognitionService : IDisposable
    {
        /// <summary>Se dispara cuando el motor reconoce texto hablado.</summary>
        event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;

        /// <summary>Se dispara cuando el reconocimiento inicia exitosamente.</summary>
        event EventHandler<EventArgs> RecognitionStarted;

        /// <summary>Se dispara cuando el reconocimiento se detiene.</summary>
        event EventHandler<EventArgs> RecognitionStopped;

        /// <summary>Indica si el servicio está escuchando activamente.</summary>
        bool IsListening { get; }

        /// <summary>Inicia la escucha continua del micrófono.</summary>
        void StartListening();

        /// <summary>Detiene la escucha del micrófono.</summary>
        void StopListening();
    }

    /// <summary>Argumentos del evento de voz reconocida.</summary>
    public class SpeechRecognizedEventArgs : EventArgs
    {
        /// <summary>Texto reconocido por el motor de voz.</summary>
        public string RecognizedText { get; }

        /// <summary>Nivel de confianza de 0.0 a 1.0.</summary>
        public float Confidence { get; }

        /// <summary>Inicializa los argumentos del evento.</summary>
        public SpeechRecognizedEventArgs(string text, float confidence)
        {
            RecognizedText = text;
            Confidence = confidence;
        }
    }
}
