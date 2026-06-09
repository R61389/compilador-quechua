namespace CompiladorQuechua.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ── Top-level form ─────────────────────────────────────────
            this.SuspendLayout();
            this.Text = "Compilador Quechua Boliviano – Módulo de Traducción de Voz";
            this.Size = new System.Drawing.Size(1100, 760);
            this.MinimumSize = new System.Drawing.Size(900, 640);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular);

            // ── Header Panel ───────────────────────────────────────────
            _headerPanel = new System.Windows.Forms.Panel();
            _headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            _headerPanel.Height = 60;
            _headerPanel.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);

            _lblTitle = new System.Windows.Forms.Label();
            _lblTitle.Text = "🦙  Compilador Quechua Boliviano";
            _lblTitle.ForeColor = System.Drawing.Color.White;
            _lblTitle.Font = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold);
            _lblTitle.AutoSize = true;
            _lblTitle.Location = new System.Drawing.Point(16, 12);

            _lblSubtitle = new System.Windows.Forms.Label();
            _lblSubtitle.Text = "Módulo de Traducción de Voz en Tiempo Real  |  Español → Quechua Boliviano";
            _lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            _lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 8.5f);
            _lblSubtitle.AutoSize = true;
            _lblSubtitle.Location = new System.Drawing.Point(18, 40);

            _headerPanel.Controls.Add(_lblTitle);
            _headerPanel.Controls.Add(_lblSubtitle);

            // ── TabControl ─────────────────────────────────────────────
            _tabControl = new System.Windows.Forms.TabControl();
            _tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            _tabControl.Font = new System.Drawing.Font("Segoe UI", 9.5f);

            _tabCompiler   = new System.Windows.Forms.TabPage("  Compilador  ");
            _tabVoice      = new System.Windows.Forms.TabPage("  Traductor de Voz  ");
            _tabHistory    = new System.Windows.Forms.TabPage("  Historial  ");
            _tabAbout      = new System.Windows.Forms.TabPage("  Acerca de  ");

            _tabControl.TabPages.Add(_tabCompiler);
            _tabControl.TabPages.Add(_tabVoice);
            _tabControl.TabPages.Add(_tabHistory);
            _tabControl.TabPages.Add(_tabAbout);

            // ── Status strip ───────────────────────────────────────────
            _statusStrip = new System.Windows.Forms.StatusStrip();
            _statusStrip.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            _statusStrip.ForeColor = System.Drawing.Color.White;

            _statusLabel = new System.Windows.Forms.ToolStripStatusLabel("Listo");
            _statusLabel.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);

            _statusWords = new System.Windows.Forms.ToolStripStatusLabel("Palabras traducidas: 0");
            _statusWords.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            _statusWords.ForeColor = System.Drawing.Color.FromArgb(0, 122, 204);

            _statusStrip.Items.Add(_statusLabel);
            _statusStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            _statusStrip.Items.Add(_statusWords);

            // ── Pulse timer ────────────────────────────────────────────
            _pulseTimer = new System.Windows.Forms.Timer(components);
            _pulseTimer.Interval = 600;
            _pulseTimer.Tick += PulseTimer_Tick;

            // ── Assemble form ──────────────────────────────────────────
            this.Controls.Add(_tabControl);
            this.Controls.Add(_headerPanel);
            this.Controls.Add(_statusStrip);

            BuildCompilerTab();
            BuildVoiceTab();
            BuildHistoryTab();
            BuildAboutTab();

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // ----------------------------------------------------------------
        // Tab builder helpers – called from InitializeComponent
        // ----------------------------------------------------------------

        private void BuildCompilerTab()
        {
            _tabCompiler.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            _tabCompiler.Padding = new System.Windows.Forms.Padding(8);

            var split = new System.Windows.Forms.SplitContainer();
            split.Dock = System.Windows.Forms.DockStyle.Fill;
            split.Orientation = System.Windows.Forms.Orientation.Horizontal;
            split.SplitterDistance = 300;
            split.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);

            // Input pane
            var lblInput = MakeLabel("Código Fuente Quechua:", true);
            lblInput.Dock = System.Windows.Forms.DockStyle.Top;
            lblInput.Height = 24;

            _rtbCompilerInput = new System.Windows.Forms.RichTextBox();
            _rtbCompilerInput.Dock = System.Windows.Forms.DockStyle.Fill;
            _rtbCompilerInput.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            _rtbCompilerInput.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            _rtbCompilerInput.Font = new System.Drawing.Font("Consolas", 11f);
            _rtbCompilerInput.Text =
                "qallariy\r\n" +
                "    rimay \"allin p'unchay, pacha!\"\r\n" +
                "    yupay x tikraq 42\r\n" +
                "    sichus x aswan hatun 0\r\n" +
                "        rimay \"yupayqa allinmi\"\r\n" +
                "    mana chayqa\r\n" +
                "        rimay \"yupayqa manamin\"\r\n" +
                "tukukun\r\n";

            var pnlInput = new System.Windows.Forms.Panel();
            pnlInput.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlInput.Controls.Add(_rtbCompilerInput);
            pnlInput.Controls.Add(lblInput);

            split.Panel1.Controls.Add(pnlInput);

            // Output pane
            var lblOutput = MakeLabel("Salida del Compilador:", true);
            lblOutput.Dock = System.Windows.Forms.DockStyle.Top;
            lblOutput.Height = 24;

            _rtbCompilerOutput = new System.Windows.Forms.RichTextBox();
            _rtbCompilerOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            _rtbCompilerOutput.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
            _rtbCompilerOutput.ForeColor = System.Drawing.Color.FromArgb(0, 200, 100);
            _rtbCompilerOutput.Font = new System.Drawing.Font("Consolas", 10f);
            _rtbCompilerOutput.ReadOnly = true;

            _btnCompile = MakeButton("▶  Compilar", System.Drawing.Color.FromArgb(0, 122, 204));
            _btnCompile.Dock = System.Windows.Forms.DockStyle.Bottom;
            _btnCompile.Height = 38;
            _btnCompile.Click += BtnCompile_Click;

            var pnlOutput = new System.Windows.Forms.Panel();
            pnlOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlOutput.Controls.Add(_rtbCompilerOutput);
            pnlOutput.Controls.Add(lblOutput);
            pnlOutput.Controls.Add(_btnCompile);

            split.Panel2.Controls.Add(pnlOutput);

            _tabCompiler.Controls.Add(split);
        }

        private void BuildVoiceTab()
        {
            _tabVoice.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            _tabVoice.Padding = new System.Windows.Forms.Padding(12);

            // Mic status bar
            _pnlMicStatus = new System.Windows.Forms.Panel();
            _pnlMicStatus.Dock = System.Windows.Forms.DockStyle.Top;
            _pnlMicStatus.Height = 48;
            _pnlMicStatus.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            _pnlMicStatus.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);

            _pnlMicIndicator = new System.Windows.Forms.Panel();
            _pnlMicIndicator.Size = new System.Drawing.Size(18, 18);
            _pnlMicIndicator.Location = new System.Drawing.Point(12, 15);
            _pnlMicIndicator.BackColor = System.Drawing.Color.FromArgb(220, 53, 69);

            _lblMicStatus = new System.Windows.Forms.Label();
            _lblMicStatus.Text = "MICRÓFONO: INACTIVO";
            _lblMicStatus.ForeColor = System.Drawing.Color.FromArgb(220, 53, 69);
            _lblMicStatus.Font = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold);
            _lblMicStatus.AutoSize = true;
            _lblMicStatus.Location = new System.Drawing.Point(40, 13);

            _pnlMicStatus.Controls.Add(_pnlMicIndicator);
            _pnlMicStatus.Controls.Add(_lblMicStatus);

            // Spanish display
            var lblSpanishHeader = MakeLabel("ESPAÑOL  (reconocido):", true);
            lblSpanishHeader.Dock = System.Windows.Forms.DockStyle.Top;
            lblSpanishHeader.Height = 28;
            lblSpanishHeader.ForeColor = System.Drawing.Color.FromArgb(0, 122, 204);

            _rtbSpanish = new System.Windows.Forms.RichTextBox();
            _rtbSpanish.Dock = System.Windows.Forms.DockStyle.Fill;
            _rtbSpanish.ReadOnly = true;
            _rtbSpanish.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            _rtbSpanish.ForeColor = System.Drawing.Color.White;
            _rtbSpanish.Font = new System.Drawing.Font("Segoe UI", 16f);
            _rtbSpanish.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _rtbSpanish.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;

            var pnlSpanish = new System.Windows.Forms.Panel();
            pnlSpanish.Dock = System.Windows.Forms.DockStyle.Top;
            pnlSpanish.Height = 130;
            pnlSpanish.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            pnlSpanish.Padding = new System.Windows.Forms.Padding(8);
            pnlSpanish.Controls.Add(_rtbSpanish);
            pnlSpanish.Controls.Add(lblSpanishHeader);

            // Quechua display
            var lblQuechuaHeader = MakeLabel("QUECHUA BOLIVIANO  (traducción):", true);
            lblQuechuaHeader.Dock = System.Windows.Forms.DockStyle.Top;
            lblQuechuaHeader.Height = 28;
            lblQuechuaHeader.ForeColor = System.Drawing.Color.FromArgb(0, 177, 106);

            _rtbQuechua = new System.Windows.Forms.RichTextBox();
            _rtbQuechua.Dock = System.Windows.Forms.DockStyle.Fill;
            _rtbQuechua.ReadOnly = true;
            _rtbQuechua.BackColor = System.Drawing.Color.FromArgb(20, 35, 20);
            _rtbQuechua.ForeColor = System.Drawing.Color.FromArgb(0, 230, 130);
            _rtbQuechua.Font = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold);
            _rtbQuechua.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _rtbQuechua.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;

            var pnlQuechua = new System.Windows.Forms.Panel();
            pnlQuechua.Dock = System.Windows.Forms.DockStyle.Top;
            pnlQuechua.Height = 130;
            pnlQuechua.BackColor = System.Drawing.Color.FromArgb(20, 35, 20);
            pnlQuechua.Padding = new System.Windows.Forms.Padding(8);
            pnlQuechua.Controls.Add(_rtbQuechua);
            pnlQuechua.Controls.Add(lblQuechuaHeader);

            // Confidence bar
            _lblConfidence = MakeLabel("Confianza del reconocimiento: —", false);
            _lblConfidence.Dock = System.Windows.Forms.DockStyle.Top;
            _lblConfidence.Height = 24;
            _lblConfidence.ForeColor = System.Drawing.Color.FromArgb(150, 150, 150);

            // Buttons
            var pnlButtons = new System.Windows.Forms.Panel();
            pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlButtons.Height = 54;
            pnlButtons.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            pnlButtons.Padding = new System.Windows.Forms.Padding(4);

            _btnStartVoice = MakeButton("🎤  Iniciar Traducción en Vivo", System.Drawing.Color.FromArgb(0, 177, 106));
            _btnStartVoice.Size = new System.Drawing.Size(240, 40);
            _btnStartVoice.Location = new System.Drawing.Point(8, 8);
            _btnStartVoice.Click += BtnStartVoice_Click;

            _btnStopVoice = MakeButton("⏹  Detener Traducción", System.Drawing.Color.FromArgb(220, 53, 69));
            _btnStopVoice.Size = new System.Drawing.Size(220, 40);
            _btnStopVoice.Location = new System.Drawing.Point(260, 8);
            _btnStopVoice.Enabled = false;
            _btnStopVoice.Click += BtnStopVoice_Click;

            _btnTranslateText = MakeButton("💬  Traducir texto manualmente", System.Drawing.Color.FromArgb(80, 80, 180));
            _btnTranslateText.Size = new System.Drawing.Size(240, 40);
            _btnTranslateText.Location = new System.Drawing.Point(494, 8);
            _btnTranslateText.Click += BtnTranslateText_Click;

            pnlButtons.Controls.Add(_btnStartVoice);
            pnlButtons.Controls.Add(_btnStopVoice);
            pnlButtons.Controls.Add(_btnTranslateText);

            // Manual text input
            _txtManualInput = new System.Windows.Forms.TextBox();
            _txtManualInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            _txtManualInput.Height = 32;
            _txtManualInput.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            _txtManualInput.ForeColor = System.Drawing.Color.White;
            _txtManualInput.Font = new System.Drawing.Font("Segoe UI", 10f);
            _txtManualInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            _txtManualInput.PlaceholderText = "Escribe texto en español para traducir manualmente…";
            _txtManualInput.KeyPress += TxtManualInput_KeyPress;

            _tabVoice.Controls.Add(pnlQuechua);
            _tabVoice.Controls.Add(_lblConfidence);
            _tabVoice.Controls.Add(pnlSpanish);
            _tabVoice.Controls.Add(_pnlMicStatus);
            _tabVoice.Controls.Add(pnlButtons);
            _tabVoice.Controls.Add(_txtManualInput);
        }

        private void BuildHistoryTab()
        {
            _tabHistory.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);

            _lvHistory = new System.Windows.Forms.ListView();
            _lvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            _lvHistory.View = System.Windows.Forms.View.Details;
            _lvHistory.FullRowSelect = true;
            _lvHistory.GridLines = true;
            _lvHistory.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            _lvHistory.ForeColor = System.Drawing.Color.White;
            _lvHistory.Font = new System.Drawing.Font("Segoe UI", 9.5f);

            _lvHistory.Columns.Add("Hora",     80,  System.Windows.Forms.HorizontalAlignment.Left);
            _lvHistory.Columns.Add("Español",  350, System.Windows.Forms.HorizontalAlignment.Left);
            _lvHistory.Columns.Add("Quechua",  350, System.Windows.Forms.HorizontalAlignment.Left);
            _lvHistory.Columns.Add("Palabras", 80,  System.Windows.Forms.HorizontalAlignment.Center);
            _lvHistory.Columns.Add("Confianza",90,  System.Windows.Forms.HorizontalAlignment.Center);

            var pnlHistoryButtons = new System.Windows.Forms.Panel();
            pnlHistoryButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlHistoryButtons.Height = 50;
            pnlHistoryButtons.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);

            _btnExportTxt = MakeButton("📄  Exportar TXT", System.Drawing.Color.FromArgb(0, 122, 204));
            _btnExportTxt.Size = new System.Drawing.Size(160, 36);
            _btnExportTxt.Location = new System.Drawing.Point(8, 7);
            _btnExportTxt.Click += BtnExportTxt_Click;

            _btnExportPdf = MakeButton("📑  Exportar PDF", System.Drawing.Color.FromArgb(100, 50, 150));
            _btnExportPdf.Size = new System.Drawing.Size(160, 36);
            _btnExportPdf.Location = new System.Drawing.Point(178, 7);
            _btnExportPdf.Click += BtnExportPdf_Click;

            _btnClearHistory = MakeButton("🗑  Limpiar Historial", System.Drawing.Color.FromArgb(180, 60, 60));
            _btnClearHistory.Size = new System.Drawing.Size(180, 36);
            _btnClearHistory.Location = new System.Drawing.Point(348, 7);
            _btnClearHistory.Click += BtnClearHistory_Click;

            pnlHistoryButtons.Controls.Add(_btnExportTxt);
            pnlHistoryButtons.Controls.Add(_btnExportPdf);
            pnlHistoryButtons.Controls.Add(_btnClearHistory);

            _tabHistory.Controls.Add(_lvHistory);
            _tabHistory.Controls.Add(pnlHistoryButtons);
        }

        private void BuildAboutTab()
        {
            _tabAbout.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);

            var rtbAbout = new System.Windows.Forms.RichTextBox();
            rtbAbout.Dock = System.Windows.Forms.DockStyle.Fill;
            rtbAbout.ReadOnly = true;
            rtbAbout.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            rtbAbout.ForeColor = System.Drawing.Color.FromArgb(210, 210, 210);
            rtbAbout.Font = new System.Drawing.Font("Segoe UI", 11f);
            rtbAbout.BorderStyle = System.Windows.Forms.BorderStyle.None;
            rtbAbout.Padding = new System.Windows.Forms.Padding(20);
            rtbAbout.Text =
                "Compilador Quechua Boliviano\r\n" +
                "Módulo de Traducción de Voz en Tiempo Real\r\n\r\n" +
                "Este software combina un compilador de lenguaje de programación Quechua\r\n" +
                "con un módulo de traducción de voz español→quechua boliviano en tiempo real.\r\n\r\n" +
                "Componentes:\r\n" +
                "  • Compilador Quechua (C/NASM): lexer, parser, AST, IR, optimizador, codegen\r\n" +
                "  • Motor gramatical Quechua Boliviano (Qhichwa sureño)\r\n" +
                "  • Reconocimiento de voz continuo en español (Windows SAPI)\r\n" +
                "  • Interfaz gráfica Windows Forms (.NET 6)\r\n\r\n" +
                "Idioma destino: Quechua Boliviano (Qhichwa)\r\n" +
                "  Variante: Quechua sureño (Bolivia, Argentina, Perú meridional)\r\n" +
                "  Incluye terminología de la región: Cochabamba, Oruro, Potosí, La Paz\r\n\r\n" +
                "Reconocimiento de voz:\r\n" +
                "  Motor: Windows Speech API (SAPI 5)\r\n" +
                "  Idioma primario: es-BO (español boliviano)\r\n" +
                "  Fallback: es-ES, es (neutro)\r\n" +
                "  Modo: dictado continuo (DictationGrammar)\r\n\r\n" +
                "Exportación de historial:\r\n" +
                "  • TXT: archivo de texto plano\r\n" +
                "  • PDF: documento formateado (iTextSharp)\r\n\r\n" +
                "Compilador CLI:\r\n" +
                "  Ejecutable: quechuac.exe\r\n" +
                "  Genera código ensamblador x86 (NASM/MASM)\r\n" +
                "  Build: make / compilar.bat\r\n\r\n" +
                "Versión: 1.0.0  |  .NET 6.0-windows  |  Windows Forms\r\n";

            _tabAbout.Controls.Add(rtbAbout);
        }

        // ----------------------------------------------------------------
        // Helper factory methods
        // ----------------------------------------------------------------

        private static System.Windows.Forms.Label MakeLabel(string text, bool bold)
        {
            return new System.Windows.Forms.Label
            {
                Text = text,
                ForeColor = System.Drawing.Color.FromArgb(200, 200, 200),
                Font = new System.Drawing.Font("Segoe UI", 9.5f,
                    bold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular),
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new System.Windows.Forms.Padding(4, 0, 0, 0)
            };
        }

        private static System.Windows.Forms.Button MakeButton(string text, System.Drawing.Color backColor)
        {
            return new System.Windows.Forms.Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold),
                Cursor = System.Windows.Forms.Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        // ── Fields ─────────────────────────────────────────────────────
        private System.Windows.Forms.Panel _headerPanel = null!;
        private System.Windows.Forms.Label _lblTitle = null!;
        private System.Windows.Forms.Label _lblSubtitle = null!;
        private System.Windows.Forms.TabControl _tabControl = null!;
        private System.Windows.Forms.TabPage _tabCompiler = null!;
        private System.Windows.Forms.TabPage _tabVoice = null!;
        private System.Windows.Forms.TabPage _tabHistory = null!;
        private System.Windows.Forms.TabPage _tabAbout = null!;
        private System.Windows.Forms.StatusStrip _statusStrip = null!;
        private System.Windows.Forms.ToolStripStatusLabel _statusLabel = null!;
        private System.Windows.Forms.ToolStripStatusLabel _statusWords = null!;
        private System.Windows.Forms.Timer _pulseTimer = null!;

        // Compiler tab
        private System.Windows.Forms.RichTextBox _rtbCompilerInput = null!;
        private System.Windows.Forms.RichTextBox _rtbCompilerOutput = null!;
        private System.Windows.Forms.Button _btnCompile = null!;

        // Voice tab
        private System.Windows.Forms.Panel _pnlMicStatus = null!;
        private System.Windows.Forms.Panel _pnlMicIndicator = null!;
        private System.Windows.Forms.Label _lblMicStatus = null!;
        private System.Windows.Forms.RichTextBox _rtbSpanish = null!;
        private System.Windows.Forms.RichTextBox _rtbQuechua = null!;
        private System.Windows.Forms.Label _lblConfidence = null!;
        private System.Windows.Forms.Button _btnStartVoice = null!;
        private System.Windows.Forms.Button _btnStopVoice = null!;
        private System.Windows.Forms.Button _btnTranslateText = null!;
        private System.Windows.Forms.TextBox _txtManualInput = null!;

        // History tab
        private System.Windows.Forms.ListView _lvHistory = null!;
        private System.Windows.Forms.Button _btnExportTxt = null!;
        private System.Windows.Forms.Button _btnExportPdf = null!;
        private System.Windows.Forms.Button _btnClearHistory = null!;
    }
}
