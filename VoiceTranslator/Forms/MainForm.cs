using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CompiladorQuechua.Controllers;
using CompiladorQuechua.Models;
using CompiladorQuechua.Services;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace CompiladorQuechua.Forms
{
    /// <summary>
    /// Ventana principal de la aplicación Compilador Quechua Boliviano.
    /// Orquesta las pestañas: Compilador, Traductor de Voz, Historial y Acerca de.
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly VoiceTranslationController _controller;
        private bool _pulseState = false;

        /// <summary>Crea la forma principal inicializando todos los servicios.</summary>
        public MainForm()
        {
            InitializeComponent();

            // Composición del árbol de dependencias (poor-man's DI)
            IQuechuaGrammarEngine grammarEngine = new QuechuaGrammarEngine();
            ITranslationService translationService = new TranslationService(grammarEngine);
            ISpeechRecognitionService speechService = new SpeechRecognitionService();

            _controller = new VoiceTranslationController(speechService, translationService);

            _controller.TranslationCompleted += Controller_TranslationCompleted;
            _controller.ListeningStarted     += Controller_ListeningStarted;
            _controller.ListeningStopped     += Controller_ListeningStopped;
            _controller.ErrorOccurred        += Controller_ErrorOccurred;

            this.FormClosing += MainForm_FormClosing;
        }

        // ----------------------------------------------------------------
        // Controller event handlers
        // ----------------------------------------------------------------

        private void Controller_TranslationCompleted(object? sender, TranslationResultEventArgs e)
        {
            var entry = e.Entry;

            // Actualizar paneles de voz
            _rtbSpanish.Text = entry.SpanishText;
            _rtbQuechua.Text = entry.QuechuaText;
            _lblConfidence.Text = $"Confianza del reconocimiento: {entry.ConfidenceScore:P0}";

            // Agregar al ListView del historial
            var item = new ListViewItem(entry.Timestamp.ToString("HH:mm:ss"));
            item.SubItems.Add(entry.SpanishText);
            item.SubItems.Add(entry.QuechuaText);
            item.SubItems.Add(entry.WordCount.ToString());
            item.SubItems.Add(entry.ConfidenceScore.ToString("P0"));
            _lvHistory.Items.Insert(0, item);

            // Actualizar contador de palabras
            _statusWords.Text = $"Palabras traducidas: {_controller.TotalWordsTranslated}";
        }

        private void Controller_ListeningStarted(object? sender, EventArgs e)
        {
            _btnStartVoice.Enabled = false;
            _btnStopVoice.Enabled = true;
            SetMicStatus(active: true);
            _pulseTimer.Start();
            _statusLabel.Text = "Escuchando en español boliviano…";
        }

        private void Controller_ListeningStopped(object? sender, EventArgs e)
        {
            _btnStartVoice.Enabled = true;
            _btnStopVoice.Enabled = false;
            SetMicStatus(active: false);
            _pulseTimer.Stop();
            _statusLabel.Text = "Traducción detenida.";
        }

        private void Controller_ErrorOccurred(object? sender, ErrorEventArgs e)
        {
            _pulseTimer.Stop();
            SetMicStatus(active: false);
            _btnStartVoice.Enabled = true;
            _btnStopVoice.Enabled = false;
            _statusLabel.Text = "Error en el reconocimiento de voz.";
            MessageBox.Show(e.Exception.Message, "Error – Reconocimiento de voz",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ----------------------------------------------------------------
        // Voice tab button handlers
        // ----------------------------------------------------------------

        private void BtnStartVoice_Click(object? sender, EventArgs e)
        {
            _controller.StartListening();
        }

        private void BtnStopVoice_Click(object? sender, EventArgs e)
        {
            _controller.StopListening();
        }

        private void BtnTranslateText_Click(object? sender, EventArgs e)
        {
            TranslateManualText();
        }

        private void TxtManualInput_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                e.Handled = true;
                TranslateManualText();
            }
        }

        private void TranslateManualText()
        {
            var text = _txtManualInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            // Reutilizar el servicio directamente (sin micrófono)
            IQuechuaGrammarEngine engine = new QuechuaGrammarEngine();
            ITranslationService svc = new TranslationService(engine);
            var entry = svc.Translate(text);
            entry.ConfidenceScore = 1.0;

            _rtbSpanish.Text = entry.SpanishText;
            _rtbQuechua.Text = entry.QuechuaText;
            _lblConfidence.Text = "Confianza del reconocimiento: entrada manual";

            var item = new ListViewItem(entry.Timestamp.ToString("HH:mm:ss"));
            item.SubItems.Add(entry.SpanishText);
            item.SubItems.Add(entry.QuechuaText);
            item.SubItems.Add(entry.WordCount.ToString());
            item.SubItems.Add("Manual");
            _lvHistory.Items.Insert(0, item);

            _txtManualInput.Clear();
            _statusLabel.Text = $"Texto traducido: {entry.WordCount} palabras.";
            _statusWords.Text = $"Palabras traducidas: {_lvHistory.Items.Count * 1}";
        }

        // ----------------------------------------------------------------
        // Compiler tab
        // ----------------------------------------------------------------

        private void BtnCompile_Click(object? sender, EventArgs e)
        {
            var code = _rtbCompilerInput.Text;
            if (string.IsNullOrWhiteSpace(code))
            {
                _rtbCompilerOutput.Text = "Error: no hay código fuente.";
                return;
            }

            // Escribir código a un archivo temporal y llamar a quechuac.exe
            try
            {
                var tmpDir = Path.GetTempPath();
                var srcFile = Path.Combine(tmpDir, "quechua_temp.qch");
                File.WriteAllText(srcFile, code, Encoding.UTF8);

                // Buscar quechuac.exe en el directorio de la aplicación o en el repo
                var exePath = FindQuechuacExe();

                if (exePath == null || !File.Exists(exePath))
                {
                    _rtbCompilerOutput.Text =
                        "quechuac.exe no encontrado.\r\n\r\n" +
                        "Compila el compilador primero ejecutando:\r\n" +
                        "    make   (Linux/macOS)\r\n" +
                        "    compilar.bat   (Windows)\r\n\r\n" +
                        "Luego vuelve a intentarlo.";
                    return;
                }

                var psi = new ProcessStartInfo(exePath, $"\"{srcFile}\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    WorkingDirectory       = Path.GetDirectoryName(exePath) ?? tmpDir
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    _rtbCompilerOutput.Text = "No se pudo iniciar el proceso de compilación.";
                    return;
                }

                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                var sb = new StringBuilder();
                sb.AppendLine($"=== Compilación ===  código de salida: {proc.ExitCode}");
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(stdout)) sb.AppendLine(stdout);
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    sb.AppendLine("--- Errores / Advertencias ---");
                    sb.AppendLine(stderr);
                }

                _rtbCompilerOutput.Text = sb.ToString();
                _statusLabel.Text = proc.ExitCode == 0
                    ? "Compilación exitosa."
                    : $"Compilación finalizada con errores (código {proc.ExitCode}).";
            }
            catch (Exception ex)
            {
                _rtbCompilerOutput.Text = $"Error al compilar:\r\n{ex.Message}";
            }
        }

        private static string? FindQuechuacExe()
        {
            // Intentar varios lugares comunes
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quechuac.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "quechuac.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "quechuac.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "quechuac.exe"),
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        // ----------------------------------------------------------------
        // History tab
        // ----------------------------------------------------------------

        private void BtnExportTxt_Click(object? sender, EventArgs e)
        {
            if (_controller.History.Count == 0)
            {
                MessageBox.Show("El historial está vacío.", "Exportar TXT",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title = "Exportar historial como TXT",
                Filter = "Archivo de texto (*.txt)|*.txt",
                FileName = $"historial_quechua_{DateTime.Now:yyyyMMdd_HHmm}.txt"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("HISTORIAL DE TRADUCCIONES – COMPILADOR QUECHUA BOLIVIANO");
                sb.AppendLine(new string('=', 60));
                sb.AppendLine($"Exportado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                sb.AppendLine(new string('=', 60));
                sb.AppendLine();

                foreach (var entry in _controller.History)
                {
                    sb.AppendLine($"[{entry.Timestamp:HH:mm:ss}]");
                    sb.AppendLine($"  ES: {entry.SpanishText}");
                    sb.AppendLine($"  QU: {entry.QuechuaText}");
                    sb.AppendLine($"  Palabras: {entry.WordCount}   Confianza: {entry.ConfidenceScore:P0}");
                    sb.AppendLine();
                }

                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                _statusLabel.Text = $"Historial exportado: {dlg.FileName}";
                MessageBox.Show($"Historial guardado en:\n{dlg.FileName}", "Exportar TXT",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportPdf_Click(object? sender, EventArgs e)
        {
            if (_controller.History.Count == 0)
            {
                MessageBox.Show("El historial está vacío.", "Exportar PDF",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title = "Exportar historial como PDF",
                Filter = "Documento PDF (*.pdf)|*.pdf",
                FileName = $"historial_quechua_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                ExportToPdf(dlg.FileName);
                _statusLabel.Text = $"PDF exportado: {dlg.FileName}";
                MessageBox.Show($"PDF guardado en:\n{dlg.FileName}", "Exportar PDF",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar PDF:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToPdf(string filePath)
        {
            using var doc = new Document(PageSize.A4, 40f, 40f, 60f, 40f);
            using var writer = PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
            doc.Open();

            // Fonts
            var fontTitle  = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16f, new BaseColor(0, 122, 204));
            var fontSub    = FontFactory.GetFont(FontFactory.HELVETICA, 10f, BaseColor.GRAY);
            var fontHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, BaseColor.WHITE);
            var fontCell   = FontFactory.GetFont(FontFactory.HELVETICA, 9f, BaseColor.DARK_GRAY);
            var fontEs     = FontFactory.GetFont(FontFactory.HELVETICA, 9f, BaseColor.DARK_GRAY);
            var fontQu     = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9f, new BaseColor(0, 140, 80));

            // Title
            doc.Add(new Paragraph("Compilador Quechua Boliviano", fontTitle));
            doc.Add(new Paragraph("Historial de Traducciones de Voz – Español → Quechua", fontSub));
            doc.Add(new Paragraph($"Exportado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", fontSub));
            doc.Add(new Paragraph(" "));

            // Table
            var table = new PdfPTable(5) { WidthPercentage = 100f };
            table.SetWidths(new float[] { 8f, 28f, 28f, 8f, 8f });

            var headerBg = new BaseColor(45, 45, 48);

            void AddHeaderCell(string text)
            {
                var cell = new PdfPCell(new Phrase(text, fontHeader))
                {
                    BackgroundColor = headerBg,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5f
                };
                table.AddCell(cell);
            }

            AddHeaderCell("Hora");
            AddHeaderCell("Español");
            AddHeaderCell("Quechua");
            AddHeaderCell("Palabras");
            AddHeaderCell("Confianza");

            var rowBg1 = new BaseColor(240, 240, 245);
            var rowBg2 = BaseColor.WHITE;
            int row = 0;

            foreach (var entry in _controller.History)
            {
                var bg = (row++ % 2 == 0) ? rowBg1 : rowBg2;

                void AddCell(string txt, iTextSharp.text.Font f, int align = Element.ALIGN_LEFT)
                {
                    var cell = new PdfPCell(new Phrase(txt, f))
                    {
                        BackgroundColor = bg,
                        HorizontalAlignment = align,
                        Padding = 4f
                    };
                    table.AddCell(cell);
                }

                AddCell(entry.Timestamp.ToString("HH:mm:ss"), fontCell, Element.ALIGN_CENTER);
                AddCell(entry.SpanishText, fontEs);
                AddCell(entry.QuechuaText, fontQu);
                AddCell(entry.WordCount.ToString(), fontCell, Element.ALIGN_CENTER);
                AddCell(entry.ConfidenceScore.ToString("P0"), fontCell, Element.ALIGN_CENTER);
            }

            doc.Add(table);
            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph($"Total entradas: {_controller.History.Count}   |   " +
                                  $"Total palabras: {_controller.TotalWordsTranslated}", fontSub));
            doc.Close();
        }

        private void BtnClearHistory_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "¿Desea eliminar todo el historial de esta sesión?",
                "Limpiar historial",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _controller.ClearHistory();
            _lvHistory.Items.Clear();
            _statusLabel.Text = "Historial limpiado.";
        }

        // ----------------------------------------------------------------
        // Mic indicator
        // ----------------------------------------------------------------

        private void SetMicStatus(bool active)
        {
            var color = active
                ? System.Drawing.Color.FromArgb(0, 177, 106)
                : System.Drawing.Color.FromArgb(220, 53, 69);

            _pnlMicIndicator.BackColor = color;
            _lblMicStatus.ForeColor = color;
            _lblMicStatus.Text = active ? "MICRÓFONO: ACTIVO" : "MICRÓFONO: INACTIVO";
        }

        private void PulseTimer_Tick(object? sender, EventArgs e)
        {
            _pulseState = !_pulseState;
            _pnlMicIndicator.BackColor = _pulseState
                ? System.Drawing.Color.FromArgb(0, 230, 130)
                : System.Drawing.Color.FromArgb(0, 100, 60);
        }

        // ----------------------------------------------------------------
        // Form closing
        // ----------------------------------------------------------------

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _pulseTimer.Stop();
            _controller.Dispose();
        }
    }
}
