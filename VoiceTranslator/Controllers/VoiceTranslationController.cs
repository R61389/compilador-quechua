using System;
using System.Collections.Generic;
using CompiladorQuechua.Models;
using CompiladorQuechua.Services;

namespace CompiladorQuechua.Controllers
{
    public class VoiceTranslationController : IDisposable
    {
        private readonly ISpeechRecognitionService _speechService;
        private readonly ITranslationService _translationService;
        private readonly List<TranslationEntry> _history = new();
        private bool _disposed;

        public event EventHandler<TranslationResultEventArgs>? TranslationCompleted;
        public event EventHandler<EventArgs>? ListeningStarted;
        public event EventHandler<EventArgs>? ListeningStopped;
        public event EventHandler<VoiceErrorEventArgs>? ErrorOccurred;

        public bool IsListening => _speechService.IsListening;
        public IReadOnlyList<TranslationEntry> History => _history.AsReadOnly();
        public int TotalWordsTranslated => _translationService.TotalWordsTranslated;

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

        public void StartListening()
        {
            try { _speechService.StartListening(); }
            catch (Exception ex) { ErrorOccurred?.Invoke(this, new VoiceErrorEventArgs(ex)); }
        }

        public void StopListening() => _speechService.StopListening();
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
                ErrorOccurred?.Invoke(this, new VoiceErrorEventArgs(ex));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _speechService.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class TranslationResultEventArgs : EventArgs
    {
        public TranslationEntry Entry { get; }
        public TranslationResultEventArgs(TranslationEntry entry) => Entry = entry;
    }

    /// <summary>Argumentos de error del pipeline de voz (evita conflicto con System.IO.ErrorEventArgs).</summary>
    public class VoiceErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public VoiceErrorEventArgs(Exception ex) => Exception = ex;
    }
}
