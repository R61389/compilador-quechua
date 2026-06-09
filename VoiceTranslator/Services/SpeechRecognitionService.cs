using System;
using System.Globalization;
using System.Linq;
using System.Speech.Recognition;
using System.Threading;

namespace CompiladorQuechua.Services
{
    public class SpeechRecognitionService : ISpeechRecognitionService
    {
        private SpeechRecognitionEngine? _engine;
        private bool _isListening;
        private bool _disposed;
        private readonly SynchronizationContext _syncContext;

        public event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;
        public event EventHandler<EventArgs>? RecognitionStarted;
        public event EventHandler<EventArgs>? RecognitionStopped;

        public bool IsListening => _isListening;

        public SpeechRecognitionService()
        {
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        private void InitializeEngine()
        {
            // Obtener todos los reconocedores instalados en Windows
            var installed = SpeechRecognitionEngine.InstalledRecognizers();

            RecognizerInfo? recognizer = null;

            // Prioridad: es-BO → es-ES → cualquier español → el primero disponible
            var priorities = new[] { "es-BO", "es-ES", "es-MX", "es-AR", "es-PE", "es-US" };
            foreach (var lang in priorities)
            {
                recognizer = installed.FirstOrDefault(r =>
                    r.Culture.Name.Equals(lang, StringComparison.OrdinalIgnoreCase));
                if (recognizer != null) break;
            }

            // Si no hay español, buscar cualquier español por nombre
            if (recognizer == null)
                recognizer = installed.FirstOrDefault(r =>
                    r.Culture.TwoLetterISOLanguageName.Equals("es", StringComparison.OrdinalIgnoreCase));

            // Último fallback: usar el reconocedor por defecto del sistema
            if (recognizer == null)
            {
                // Crear con la cultura del sistema
                _engine = new SpeechRecognitionEngine(
                    CultureInfo.CurrentUICulture);
            }
            else
            {
                _engine = new SpeechRecognitionEngine(recognizer.Culture);
            }

            var grammar = new DictationGrammar { Name = "Dictado", Enabled = true };
            _engine.LoadGrammar(grammar);
            _engine.SetInputToDefaultAudioDevice();
            _engine.SpeechRecognized += OnSpeechRecognized;
            _engine.SpeechRecognitionRejected += OnSpeechRejected;
            _engine.RecognizeCompleted += OnRecognizeCompleted;
            _engine.EndSilenceTimeout = TimeSpan.FromMilliseconds(600);
            _engine.EndSilenceTimeoutAmbiguous = TimeSpan.FromMilliseconds(600);
        }

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
                // Mensaje de error amigable con instrucciones para instalar español
                var installed = SpeechRecognitionEngine.InstalledRecognizers();
                var list = installed.Count > 0
                    ? string.Join(", ", installed.Select(r => r.Culture.Name))
                    : "(ninguno)";

                throw new InvalidOperationException(
                    $"No se pudo iniciar el reconocimiento de voz.\n\n" +
                    $"Reconocedores instalados en tu Windows: {list}\n\n" +
                    $"Para instalar español:\n" +
                    $"  Configuración → Hora e idioma → Voz\n" +
                    $"  → Agregar idioma → Español\n" +
                    $"  → Instalar 'Reconocimiento de voz'\n\n" +
                    $"Detalle técnico: {ex.Message}", ex);
            }
        }

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

        private void OnSpeechRejected(object? sender, SpeechRecognitionRejectedEventArgs e) { }

        private void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
        {
            if (_isListening && !e.Cancelled && e.Error == null)
            {
                try { _engine?.RecognizeAsync(RecognizeMode.Multiple); }
                catch { }
            }
        }

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
