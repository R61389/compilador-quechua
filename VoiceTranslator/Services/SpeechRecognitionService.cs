using System;
using System.Globalization;
using System.Speech.Recognition;
using System.Threading;

namespace CompiladorQuechua.Services
{
    /// <summary>
    /// Servicio de reconocimiento de voz continuo en español boliviano.
    /// Utiliza System.Speech (Windows SAPI) como motor primario.
    /// </summary>
    public class SpeechRecognitionService : ISpeechRecognitionService
    {
        private SpeechRecognitionEngine? _engine;
        private bool _isListening;
        private bool _disposed;
        private readonly SynchronizationContext _syncContext;

        /// <inheritdoc/>
        public event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? RecognitionStarted;

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? RecognitionStopped;

        /// <inheritdoc/>
        public bool IsListening => _isListening;

        /// <summary>Inicializa el servicio capturando el contexto de sincronización actual.</summary>
        public SpeechRecognitionService()
        {
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        private void InitializeEngine()
        {
            // Intentar es-BO primero, luego es-ES, luego español neutral
            CultureInfo culture;
            try { culture = new CultureInfo("es-BO"); }
            catch
            {
                try { culture = new CultureInfo("es-ES"); }
                catch { culture = new CultureInfo("es"); }
            }

            _engine = new SpeechRecognitionEngine(culture);

            // Gramática de dictado libre para reconocimiento de voz continuo
            var grammar = new DictationGrammar
            {
                Name = "DictationES",
                Enabled = true
            };
            _engine.LoadGrammar(grammar);

            _engine.SetInputToDefaultAudioDevice();
            _engine.SpeechRecognized += OnSpeechRecognized;
            _engine.SpeechRecognitionRejected += OnSpeechRejected;
            _engine.RecognizeCompleted += OnRecognizeCompleted;

            // Ajuste para reconocimiento continuo
            _engine.InitialSilenceTimeout = TimeSpan.Zero;
            _engine.BabbleTimeout = TimeSpan.Zero;
            _engine.EndSilenceTimeout = TimeSpan.FromMilliseconds(500);
            _engine.EndSilenceTimeoutAmbiguous = TimeSpan.FromMilliseconds(500);
        }

        /// <inheritdoc/>
        public void StartListening()
        {
            if (_isListening) return;
            try
            {
                if (_engine == null) InitializeEngine();
                _engine!.RecognizeAsync(RecognizeMode.Multiple);
                _isListening = true;
                _syncContext.Post(_ => RecognitionStarted?.Invoke(this, EventArgs.Empty), null);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"No se pudo iniciar el reconocimiento de voz. " +
                    $"Verifique que su micrófono esté conectado y que el idioma español esté instalado en Windows.\n\n" +
                    $"Detalle: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public void StopListening()
        {
            if (!_isListening) return;
            _engine?.RecognizeAsyncStop();
            _isListening = false;
            _syncContext.Post(_ => RecognitionStopped?.Invoke(this, EventArgs.Empty), null);
        }

        private void OnSpeechRecognized(object? sender, System.Speech.Recognition.SpeechRecognizedEventArgs e)
        {
            if (e.Result == null || string.IsNullOrWhiteSpace(e.Result.Text)) return;
            var args = new SpeechRecognizedEventArgs(e.Result.Text, e.Result.Confidence);
            _syncContext.Post(_ => SpeechRecognized?.Invoke(this, args), null);
        }

        private void OnSpeechRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
        {
            // Rechazos no críticos — se ignoran silenciosamente
        }

        private void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
        {
            // Reiniciar reconocimiento si aún estamos escuchando y no hubo error ni cancelación
            if (_isListening && !e.Cancelled && e.Error == null)
            {
                try { _engine?.RecognizeAsync(RecognizeMode.Multiple); }
                catch { /* El engine puede haber sido dispuesto entre llamadas */ }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopListening();
            _engine?.Dispose();
            _engine = null;
            GC.SuppressFinalize(this);
        }
    }
}
