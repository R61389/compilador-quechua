using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;

namespace CompiladorQuechua.Services
{
    /// <summary>
    /// Reconocimiento de voz usando la API moderna de Windows (WinRT).
    /// Usa Windows.Media.SpeechRecognition que sí soporta es-MX instalado
    /// vía Add-WindowsCapability, a diferencia del antiguo System.Speech (SAPI).
    /// </summary>
    public class SpeechRecognitionService : ISpeechRecognitionService
    {
        private SpeechRecognizer? _recognizer;
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

        public void StartListening()
        {
            if (_isListening) return;
            // Lanzar en un hilo separado para no bloquear la UI
            Task.Run(async () =>
            {
                try
                {
                    await InitializeAndStartAsync();
                }
                catch (Exception ex)
                {
                    _syncContext.Post(_ =>
                    {
                        throw new InvalidOperationException(BuildErrorMessage(ex), ex);
                    }, null);
                }
            });
        }

        private async Task InitializeAndStartAsync()
        {
            // Elegir idioma: es-MX primero, luego cualquier español
            var language = ChooseLanguage();

            _recognizer = new SpeechRecognizer(language);

            // Gramática de dictado libre
            var dictation = new SpeechRecognitionTopicConstraint(
                SpeechRecognitionScenario.Dictation, "dictation");
            _recognizer.Constraints.Add(dictation);

            var compileResult = await _recognizer.CompileConstraintsAsync();
            if (compileResult.Status != SpeechRecognitionResultStatus.Success)
                throw new InvalidOperationException(
                    $"No se pudo compilar la gramática: {compileResult.Status}");

            // Suscribir al evento de resultado continuo
            _recognizer.ContinuousRecognitionSession.ResultGenerated += OnResultGenerated;
            _recognizer.ContinuousRecognitionSession.Completed      += OnSessionCompleted;

            await _recognizer.ContinuousRecognitionSession.StartAsync();

            _isListening = true;
            _syncContext.Post(_ => RecognitionStarted?.Invoke(this, EventArgs.Empty), null);
        }

        public void StopListening()
        {
            if (!_isListening) return;
            _isListening = false;
            Task.Run(async () =>
            {
                try
                {
                    if (_recognizer != null)
                        await _recognizer.ContinuousRecognitionSession.StopAsync();
                }
                catch { /* ignorar errores al detener */ }
                _syncContext.Post(_ => RecognitionStopped?.Invoke(this, EventArgs.Empty), null);
            });
        }

        private void OnResultGenerated(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            var text = args.Result?.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            var confidence = args.Result!.RawConfidence;
            var evtArgs = new SpeechRecognizedEventArgs(text, (float)confidence);
            _syncContext.Post(_ => SpeechRecognized?.Invoke(this, evtArgs), null);
        }

        private void OnSessionCompleted(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionCompletedEventArgs args)
        {
            // Reiniciar si la sesión terminó inesperadamente y aún estamos escuchando
            if (_isListening && args.Status != SpeechRecognitionResultStatus.Success)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(500);
                    if (_isListening && _recognizer != null)
                    {
                        try
                        {
                            await _recognizer.ContinuousRecognitionSession.StartAsync();
                        }
                        catch { /* si falla, detenerse limpiamente */ }
                    }
                });
            }
        }

        private static Language ChooseLanguage()
        {
            // Prioridad: es-MX → es-ES → es-BO → cualquier español
            var candidates = new[] { "es-MX", "es-ES", "es-BO", "es-AR", "es-US", "es" };
            var available  = SpeechRecognizer.SupportedTopicLanguages;

            foreach (var tag in candidates)
            {
                var match = available.FirstOrDefault(l =>
                    l.LanguageTag.Equals(tag, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }

            // Fallback: primer idioma disponible
            return available.FirstOrDefault() ?? new Language("es-MX");
        }

        private static string BuildErrorMessage(Exception ex)
        {
            var available = SpeechRecognizer.SupportedTopicLanguages;
            var list = available.Count > 0
                ? string.Join(", ", available.Select(l => l.LanguageTag))
                : "(ninguno)";

            return $"No se pudo iniciar el reconocimiento de voz.\n\n" +
                   $"Idiomas disponibles en la API moderna: {list}\n\n" +
                   $"Si la lista está vacía, ejecuta en PowerShell (administrador):\n" +
                   $"  Add-WindowsCapability -Online -Name \"Language.Speech~~~es-MX~0.0.1.0\"\n" +
                   $"Luego reinicia el PC.\n\n" +
                   $"Detalle: {ex.Message}";
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopListening();
            _recognizer?.Dispose();
            _recognizer = null;
            GC.SuppressFinalize(this);
        }
    }
}
