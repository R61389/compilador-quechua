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

            // Layout principal: filas fijas para cada sección
            var table = new System.Windows.Forms.TableLayoutPanel();
            table.Dock = System.Windows.Forms.DockStyle.Fill;
            table.ColumnCount = 1;
            table.RowCount = 6;
            table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Percent, 100f));
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Absolute, 48f));   // 0: mic status
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Percent, 50f));    // 1: español
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Percent, 50f));    // 2: quechua
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Absolute, 24f));   // 3: confianza
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Absolute, 48f));   // 4: input manual
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Absolute, 58f));   // 5: botones

            // --- Fila 0: estado micrófono ---
            _pnlMicStatus = new System.Windows.Forms.Panel();
            _pnlMicStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlMicStatus.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);

            _pnlMicIndicator = new System.Windows.Forms.Panel();
            _pnlMicIndicator.Size = new System.Drawing.Size(18, 18);
            _pnlMicIndicator.Location = new System.Drawing.Point(12, 15);
            _pnlMicIndicator.BackColor = System.Drawing.Color.FromArgb(220, 53, 69);

            _lblMicStatus = new System.Windows.Forms.Label();
            _lblMicStatus.Text = "MICRÓFONO: INACTIVO";
            _lblMicStatus.ForeColor = System.Drawing.Color.FromArgb(220, 53, 69);
            _lblMicStatus.Font = new System.Drawing.Font("Segoe UI", 11f,
                System.Drawing.FontStyle.Bold);
            _lblMicStatus.AutoSize = true;
            _lblMicStatus.Location = new System.Drawing.Point(40, 13);

            _pnlMicStatus.Controls.Add(_pnlMicIndicator);
            _pnlMicStatus.Controls.Add(_lblMicStatus);
            table.Controls.Add(_pnlMicStatus, 0, 0);

            // --- Fila 1: español reconocido ---
            var pnlSpanish = new System.Windows.Forms.Panel();
            pnlSpanish.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlSpanish.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            pnlSpanish.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);

            var lblSpanishHeader = MakeLabel("ESPAÑOL  (reconocido):", true);
            lblSpanishHeader.Dock = System.Windows.Forms.DockStyle.Top;
            lblSpanishHeader.Height = 26;
            lblSpanishHeader.ForeColor = System.Drawing.Color.FromArgb(0, 122, 204);

            _rtbSpanish = new System.Windows.Forms.RichTextBox();
            _rtbSpanish.Dock = System.Windows.Forms.DockStyle.Fill;
            _rtbSpanish.ReadOnly = true;
            _rtbSpanish.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            _rtbSpanish.ForeColor = System.Drawing.Color.White;
            _rtbSpanish.Font = new System.Drawing.Font("Segoe UI", 15f);
            _rtbSpanish.BorderStyle = System.Windows.Forms.BorderStyle.None;

            pnlSpanish.Controls.Add(_rtbSpanish);
            pnlSpanish.Controls.Add(lblSpanishHeader);
            table.Controls.Add(pnlSpanish, 0, 1);

            // --- Fila 2: quechua traducido ---
            var pnlQuechua = new System.Windows.Forms.Panel();
            pnlQuechua.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlQuechua.BackColor = System.Drawing.Color.FromArgb(20, 35, 20);
            pnlQuechua.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);

            var lblQuechuaHeader = MakeLabel("QUECHUA BOLIVIANO  (traducción):", true);
            lblQuechuaHeader.Dock = System.Windows.Forms.DockStyle.Top;
            lblQuechuaHeader.Height = 26;
            lblQuechuaHeader.ForeColor = System.Drawing.Color.FromArgb(0, 177, 106);

            _rtbQuechua = new System.Windows.Forms.RichTextBox();
            _rtbQuechua.Dock = System.Windows.Forms.DockStyle.Fill;
            _rtbQuechua.ReadOnly = true;
            _rtbQuechua.BackColor = System.Drawing.Color.FromArgb(20, 35, 20);
            _rtbQuechua.ForeColor = System.Drawing.Color.FromArgb(0, 230, 130);
            _rtbQuechua.Font = new System.Drawing.Font("Segoe UI", 15f,
                System.Drawing.FontStyle.Bold);
            _rtbQuechua.BorderStyle = System.Windows.Forms.BorderStyle.None;

            pnlQuechua.Controls.Add(_rtbQuechua);
            pnlQuechua.Controls.Add(lblQuechuaHeader);
            table.Controls.Add(pnlQuechua, 0, 2);

            // --- Fila 3: confianza ---
            _lblConfidence = MakeLabel("Confianza del reconocimiento: —", false);
            _lblConfidence.Dock = System.Windows.Forms.DockStyle.Fill;
            _lblConfidence.ForeColor = System.Drawing.Color.FromArgb(150, 150, 150);
            _lblConfidence.Padding = new System.Windows.Forms.Padding(6, 2, 0, 0);
            table.Controls.Add(_lblConfidence, 0, 3);

            // --- Fila 4: cuadro de texto manual ---
            var pnlManual = new System.Windows.Forms.Panel();
            pnlManual.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlManual.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            pnlManual.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);

            _txtManualInput = new System.Windows.Forms.TextBox();
            _txtManualInput.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtManualInput.BackColor = System.Drawing.Color.FromArgb(60, 60, 65);
            _txtManualInput.ForeColor = System.Drawing.Color.White;
            _txtManualInput.Font = new System.Drawing.Font("Segoe UI", 10f);
            _txtManualInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            _txtManualInput.PlaceholderText = "Escribe aquí en español y presiona Enter para traducir…";
            _txtManualInput.TabStop = true;
            _txtManualInput.TabIndex = 0;
            _txtManualInput.KeyPress += TxtManualInput_KeyPress;

            pnlManual.Controls.Add(_txtManualInput);
            table.Controls.Add(pnlManual, 0, 4);

            // --- Fila 5: botones ---
            var pnlButtons = new System.Windows.Forms.Panel();
            pnlButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlButtons.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            pnlButtons.Padding = new System.Windows.Forms.Padding(4, 6, 4, 6);

            _tabVoice.Controls.Add(table);
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

            // Panel superior: info general
            var rtbAbout = new System.Windows.Forms.RichTextBox();
            rtbAbout.Dock = System.Windows.Forms.DockStyle.Top;
            rtbAbout.Height = 280;
            rtbAbout.ReadOnly = true;
            rtbAbout.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            rtbAbout.ForeColor = System.Drawing.Color.FromArgb(210, 210, 210);
            rtbAbout.Font = new System.Drawing.Font("Segoe UI", 10.5f);
            rtbAbout.BorderStyle = System.Windows.Forms.BorderStyle.None;
            rtbAbout.Text =
                "Compilador Quechua Boliviano — Módulo de Traducción de Voz\r\n" +
                new string('─', 60) + "\r\n\r\n" +
                "Integración con el compilador:\r\n" +
                "  El motor de traducción lee src/lexer.c y src/parser.c de tu\r\n" +
                "  compilador quechua al iniciarse, extrayendo:\r\n" +
                "    • Tabla KEYWORDS[] → vocabulario base quechua\r\n" +
                "    • match_kw2/peek_kw2 → frases de 2 palabras (mana chayqa, etc.)\r\n" +
                "  Sobre ese vocabulario construye el mapa español→quechua.\r\n\r\n" +
                "Reconocimiento de voz:\r\n" +
                "  Motor: Windows SAPI 5 | Idioma: es-BO → es-ES → es\r\n" +
                "  Modo: dictado continuo | Latencia: < 500 ms\r\n\r\n" +
                "Exportación: TXT · PDF (iTextSharp)\r\n" +
                "Versión: 1.0.0  |  .NET 6.0-windows  |  Windows Forms\r\n";

            // Panel inferior: vocabulario extraído del compilador (cargado dinámicamente)
            var lblVocab = new System.Windows.Forms.Label();
            lblVocab.Text = "Vocabulario cargado desde tu compilador (src/lexer.c + src/parser.c):";
            lblVocab.ForeColor = System.Drawing.Color.FromArgb(0, 177, 106);
            lblVocab.Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
            lblVocab.Dock = System.Windows.Forms.DockStyle.Top;
            lblVocab.Height = 28;
            lblVocab.Padding = new System.Windows.Forms.Padding(8, 4, 0, 0);

            var rtbVocab = new System.Windows.Forms.RichTextBox();
            rtbVocab.Dock = System.Windows.Forms.DockStyle.Fill;
            rtbVocab.ReadOnly = true;
            rtbVocab.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            rtbVocab.ForeColor = System.Drawing.Color.FromArgb(156, 220, 254);
            rtbVocab.Font = new System.Drawing.Font("Cascadia Code", 9.5f);
            rtbVocab.BorderStyle = System.Windows.Forms.BorderStyle.None;
            // El texto se carga en MainForm_Load para tener acceso a _grammarEngine
            rtbVocab.Name = "rtbCompilerVocab";

            _tabAbout.Controls.Add(rtbVocab);
            _tabAbout.Controls.Add(lblVocab);
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
