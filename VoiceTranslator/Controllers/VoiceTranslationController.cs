using System;
using System.Collections.Generic;
using CompiladorQuechua.Models;
using CompiladorQuechua.Services;

namespace CompiladorQuechua.Controllers
{
    /// <summary>
    /// Controlador principal que orquesta reconocimiento de voz → traducción → notificación UI.
    /// Implementa el patrón Observer para desacoplar la UI de la lógica de negocio.
    /// </summary>
    public class VoiceTranslationController : IDisposable
    {
        private readonly ISpeechRecognitionService _speechService;
        private readonly ITranslationService _translationService;
        private readonly List<TranslationEntry> _history = new();
        private bool _disposed;

        /// <summary>Se dispara cuando una traducción ha sido completada.</summary>
        public event EventHandler<TranslationResultEventArgs>? TranslationCompleted;

        /// <summary>Se dispara cuando el reconocimiento de voz ha comenzado.</summary>
        public event EventHandler<EventArgs>? ListeningStarted;

        /// <summary>Se dispara cuando el reconocimiento de voz se ha detenido.</summary>
        public event EventHandler<EventArgs>? ListeningStopped;

        /// <summary>Se dispara cuando ocurre un error en el pipeline.</summary>
        public event EventHandler<ErrorEventArgs>? ErrorOccurred;

        /// <summary>Indica si el micrófono está activo.</summary>
        public bool IsListening => _speechService.IsListening;

        /// <summary>Historial de traducciones de la sesión actual.</summary>
        public IReadOnlyList<TranslationEntry> History => _history.AsReadOnly();

        /// <summary>Total de palabras quechua traducidas en la sesión.</summary>
        public int TotalWordsTranslated => _translationService.TotalWordsTranslated;

        /// <summary>
        /// Crea el controlador inyectando los servicios requeridos.
        /// </summary>
        /// <param name="speechService">Servicio de reconocimiento de voz.</param>
        /// <param name="translationService">Servicio de traducción español→quechua.</param>
        public VoiceTranslationController(
            ISpeechRecognitionService speechService,
            ITranslationService translationService)
        {
            _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
            _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));

            _speechService.SpeechRecognized += OnSpeechRecognized;
            _speechService.RecognitionStarted += (s, e) => ListeningStarted?.Invoke(this, e);
            _speechService.RecognitionStopped += (s, e) => ListeningStopped?.Invoke(this, e);
        }

        /// <summary>Inicia la escucha del micrófono.</summary>
        public void StartListening()
        {
            try { _speechService.StartListening(); }
            catch (Exception ex) { ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex)); }
        }

        /// <summary>Detiene la escucha del micrófono.</summary>
        public void StopListening() => _speechService.StopListening();

        /// <summary>Elimina todas las entradas del historial de sesión.</summary>
        public void ClearHistory() => _history.Clear();

        private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            try
            {
                var entry = _translationService.Translate(e.RecognizedText);
                entry.ConfidenceScore = e.Confidence;
                _history.Add(entry);
                TranslationCompleted?.Invoke(this, new TranslationResultEventArgs(entry));
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _speechService.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>Argumentos del evento TranslationCompleted.</summary>
    public class TranslationResultEventArgs : EventArgs
    {
        /// <summary>Entrada de traducción resultante.</summary>
        public TranslationEntry Entry { get; }

        /// <summary>Crea los argumentos con la entrada de traducción.</summary>
        public TranslationResultEventArgs(TranslationEntry entry) => Entry = entry;
    }

    /// <summary>Argumentos del evento ErrorOccurred.</summary>
    public class ErrorEventArgs : EventArgs
    {
        /// <summary>Excepción que causó el error.</summary>
        public Exception Exception { get; }

        /// <summary>Crea los argumentos con la excepción.</summary>
        public ErrorEventArgs(Exception ex) => Exception = ex;
    }
}
