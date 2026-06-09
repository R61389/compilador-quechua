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

        // Paleta de colores
        private static readonly System.Drawing.Color C_BG        = System.Drawing.Color.FromArgb(15, 15, 25);
        private static readonly System.Drawing.Color C_SURFACE   = System.Drawing.Color.FromArgb(22, 22, 38);
        private static readonly System.Drawing.Color C_CARD      = System.Drawing.Color.FromArgb(30, 30, 50);
        private static readonly System.Drawing.Color C_HEADER    = System.Drawing.Color.FromArgb(18, 18, 35);
        private static readonly System.Drawing.Color C_ACCENT    = System.Drawing.Color.FromArgb(99, 102, 241);   // índigo
        private static readonly System.Drawing.Color C_GREEN     = System.Drawing.Color.FromArgb(16, 185, 129);   // esmeralda
        private static readonly System.Drawing.Color C_RED       = System.Drawing.Color.FromArgb(239, 68,  68);   // rojo
        private static readonly System.Drawing.Color C_GOLD      = System.Drawing.Color.FromArgb(251, 191,  36);  // ámbar
        private static readonly System.Drawing.Color C_PURPLE    = System.Drawing.Color.FromArgb(167,  139, 250); // violeta claro
        private static readonly System.Drawing.Color C_TEXT      = System.Drawing.Color.FromArgb(226, 232, 240);
        private static readonly System.Drawing.Color C_TEXT_DIM  = System.Drawing.Color.FromArgb(148, 163, 184);
        private static readonly System.Drawing.Color C_INPUT_BG  = System.Drawing.Color.FromArgb(40,  40,  65);
        private static readonly System.Drawing.Color C_BORDER    = System.Drawing.Color.FromArgb(55,  55,  80);

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            this.SuspendLayout();
            this.Text          = "Qhichwa — Compilador & Traductor de Voz Boliviano";
            this.Size          = new System.Drawing.Size(1140, 800);
            this.MinimumSize   = new System.Drawing.Size(900, 640);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.BackColor     = C_BG;
            this.Font          = new System.Drawing.Font("Segoe UI", 9f);

            // ── Barra de título / Header ────────────────────────────────
            _headerPanel           = new System.Windows.Forms.Panel();
            _headerPanel.Dock      = System.Windows.Forms.DockStyle.Top;
            _headerPanel.Height    = 68;
            _headerPanel.BackColor = C_HEADER;
            _headerPanel.Padding   = new System.Windows.Forms.Padding(16, 0, 16, 0);

            // Franja de color en el borde inferior del header
            var headerAccent           = new System.Windows.Forms.Panel();
            headerAccent.Dock          = System.Windows.Forms.DockStyle.Bottom;
            headerAccent.Height        = 3;
            headerAccent.BackColor     = C_ACCENT;

            _lblTitle = new System.Windows.Forms.Label();
            _lblTitle.Text      = "🌄  Qhichwa  —  Compilador Quechua Boliviano";
            _lblTitle.ForeColor = C_TEXT;
            _lblTitle.Font      = new System.Drawing.Font("Segoe UI", 15f, System.Drawing.FontStyle.Bold);
            _lblTitle.AutoSize  = true;
            _lblTitle.Location  = new System.Drawing.Point(16, 10);

            _lblSubtitle = new System.Windows.Forms.Label();
            _lblSubtitle.Text      = "Traducción de Voz en Tiempo Real  ·  Español (es-MX)  →  Quechua Boliviano (Qhichwa)";
            _lblSubtitle.ForeColor = C_TEXT_DIM;
            _lblSubtitle.Font      = new System.Drawing.Font("Segoe UI", 8.5f);
            _lblSubtitle.AutoSize  = true;
            _lblSubtitle.Location  = new System.Drawing.Point(18, 42);

            _headerPanel.Controls.Add(headerAccent);
            _headerPanel.Controls.Add(_lblTitle);
            _headerPanel.Controls.Add(_lblSubtitle);

            // ── TabControl ─────────────────────────────────────────────
            _tabControl           = new System.Windows.Forms.TabControl();
            _tabControl.Dock      = System.Windows.Forms.DockStyle.Fill;
            _tabControl.Font      = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
            _tabControl.BackColor = C_BG;

            _tabCompiler = new System.Windows.Forms.TabPage("  ⚙  Compilador  ");
            _tabVoice    = new System.Windows.Forms.TabPage("  🎤  Traductor de Voz  ");
            _tabHistory  = new System.Windows.Forms.TabPage("  📋  Historial  ");
            _tabAbout    = new System.Windows.Forms.TabPage("  ℹ  Acerca de  ");

            _tabControl.TabPages.Add(_tabCompiler);
            _tabControl.TabPages.Add(_tabVoice);
            _tabControl.TabPages.Add(_tabHistory);
            _tabControl.TabPages.Add(_tabAbout);

            // ── Status strip ───────────────────────────────────────────
            _statusStrip           = new System.Windows.Forms.StatusStrip();
            _statusStrip.BackColor = C_HEADER;
            _statusStrip.ForeColor = C_TEXT;
            _statusStrip.SizingGrip = false;

            _statusLabel           = new System.Windows.Forms.ToolStripStatusLabel("  Listo");
            _statusLabel.ForeColor = C_TEXT_DIM;

            _statusWords           = new System.Windows.Forms.ToolStripStatusLabel("Palabras traducidas: 0");
            _statusWords.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            _statusWords.ForeColor = C_ACCENT;
            _statusWords.Font      = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold);

            _statusStrip.Items.Add(_statusLabel);
            _statusStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            _statusStrip.Items.Add(_statusWords);

            // ── Pulse timer ────────────────────────────────────────────
            _pulseTimer          = new System.Windows.Forms.Timer(components);
            _pulseTimer.Interval = 600;
            _pulseTimer.Tick    += PulseTimer_Tick;

            // ── Ensamblar ──────────────────────────────────────────────
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

        // ────────────────────────────────────────────────────────────────
        // TAB: COMPILADOR
        // ────────────────────────────────────────────────────────────────
        private void BuildCompilerTab()
        {
            _tabCompiler.BackColor = C_BG;
            _tabCompiler.Padding   = new System.Windows.Forms.Padding(10);

            var split = new System.Windows.Forms.SplitContainer();
            split.Dock             = System.Windows.Forms.DockStyle.Fill;
            split.Orientation      = System.Windows.Forms.Orientation.Horizontal;
            split.SplitterDistance = 310;
            split.BackColor        = C_BORDER;
            split.SplitterWidth    = 3;

            var lblInput      = MakeHeader("📝  Código Fuente  —  Quechua Boliviano", C_ACCENT);
            lblInput.Dock     = System.Windows.Forms.DockStyle.Top;
            lblInput.Height   = 30;

            _rtbCompilerInput           = new System.Windows.Forms.RichTextBox();
            _rtbCompilerInput.Dock      = System.Windows.Forms.DockStyle.Fill;
            _rtbCompilerInput.BackColor = C_SURFACE;
            _rtbCompilerInput.ForeColor = C_TEXT;
            _rtbCompilerInput.Font      = new System.Drawing.Font("Cascadia Code", 11f);
            _rtbCompilerInput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _rtbCompilerInput.Text      =
                "qallariy main()\r\n" +
                "    yupay inti = 90\r\n" +
                "    rimay \"allin p'unchay!\"\r\n" +
                "    sichus (inti aswan hatun 50)\r\n" +
                "        rimay \"hatunmi\"\r\n" +
                "    mana chayqa\r\n" +
                "        rimay \"pisimi\"\r\n" +
                "    tukukun\r\n" +
                "    kutiy 0\r\n" +
                "tukukun\r\n";

            var pnlInput = new System.Windows.Forms.Panel();
            pnlInput.Dock      = System.Windows.Forms.DockStyle.Fill;
            pnlInput.BackColor = C_SURFACE;
            pnlInput.Padding   = new System.Windows.Forms.Padding(4);
            pnlInput.Controls.Add(_rtbCompilerInput);
            pnlInput.Controls.Add(lblInput);
            split.Panel1.Controls.Add(pnlInput);
            split.Panel1.BackColor = C_SURFACE;

            var lblOutput      = MakeHeader("🖥  Salida del Compilador", C_GREEN);
            lblOutput.Dock     = System.Windows.Forms.DockStyle.Top;
            lblOutput.Height   = 30;

            _rtbCompilerOutput           = new System.Windows.Forms.RichTextBox();
            _rtbCompilerOutput.Dock      = System.Windows.Forms.DockStyle.Fill;
            _rtbCompilerOutput.BackColor = C_CARD;
            _rtbCompilerOutput.ForeColor = C_GREEN;
            _rtbCompilerOutput.Font      = new System.Drawing.Font("Cascadia Code", 10f);
            _rtbCompilerOutput.ReadOnly  = true;
            _rtbCompilerOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;

            _btnCompile        = MakeBtn("▶  Compilar", C_ACCENT, 140, 36);
            _btnCompile.Dock   = System.Windows.Forms.DockStyle.Bottom;
            _btnCompile.Height = 42;
            _btnCompile.Click += BtnCompile_Click;

            var pnlOutput = new System.Windows.Forms.Panel();
            pnlOutput.Dock      = System.Windows.Forms.DockStyle.Fill;
            pnlOutput.BackColor = C_CARD;
            pnlOutput.Padding   = new System.Windows.Forms.Padding(4);
            pnlOutput.Controls.Add(_rtbCompilerOutput);
            pnlOutput.Controls.Add(lblOutput);
            pnlOutput.Controls.Add(_btnCompile);
            split.Panel2.Controls.Add(pnlOutput);
            split.Panel2.BackColor = C_CARD;

            _tabCompiler.Controls.Add(split);
        }

        // ────────────────────────────────────────────────────────────────
        // TAB: TRADUCTOR DE VOZ
        // ────────────────────────────────────────────────────────────────
        private void BuildVoiceTab()
        {
            _tabVoice.BackColor = C_BG;

            var table        = new System.Windows.Forms.TableLayoutPanel();
            table.Dock       = System.Windows.Forms.DockStyle.Fill;
            table.ColumnCount = 1;
            table.RowCount   = 6;
            table.BackColor  = C_BG;
            table.Padding    = new System.Windows.Forms.Padding(10, 8, 10, 8);
            table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Percent, 100f));
            // fila 0: barra mic (fija)
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Absolute, 52f));
            // fila 1: español (50 %)
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Percent, 50f));
            // fila 2: quechua (50 %)
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Percent, 50f));
            // fila 3: confianza (fija)
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Absolute, 26f));
            // fila 4: input manual (fija)
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Absolute, 50f));
            // fila 5: botones (fija)
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Absolute, 60f));

            // ── Fila 0: barra de estado del micrófono ──────────────────
            _pnlMicStatus           = new System.Windows.Forms.Panel();
            _pnlMicStatus.Dock      = System.Windows.Forms.DockStyle.Fill;
            _pnlMicStatus.BackColor = C_CARD;
            _pnlMicStatus.Padding   = new System.Windows.Forms.Padding(12, 0, 12, 0);

            // borde izquierdo de color
            var micAccent           = new System.Windows.Forms.Panel();
            micAccent.Dock          = System.Windows.Forms.DockStyle.Left;
            micAccent.Width         = 4;
            micAccent.BackColor     = C_RED;
            micAccent.Name          = "micAccentBar";

            _pnlMicIndicator           = new System.Windows.Forms.Panel();
            _pnlMicIndicator.Size      = new System.Drawing.Size(14, 14);
            _pnlMicIndicator.Location  = new System.Drawing.Point(24, 19);
            _pnlMicIndicator.BackColor = C_RED;

            _lblMicStatus           = new System.Windows.Forms.Label();
            _lblMicStatus.Text      = "●  MICRÓFONO: INACTIVO";
            _lblMicStatus.ForeColor = C_RED;
            _lblMicStatus.Font      = new System.Drawing.Font("Segoe UI", 10.5f,
                System.Drawing.FontStyle.Bold);
            _lblMicStatus.AutoSize  = true;
            _lblMicStatus.Location  = new System.Drawing.Point(44, 15);

            var lblLang           = new System.Windows.Forms.Label();
            lblLang.Text          = "Motor: es-MX  →  Qhichwa Sudboliviano";
            lblLang.ForeColor     = C_TEXT_DIM;
            lblLang.Font          = new System.Drawing.Font("Segoe UI", 8f);
            lblLang.AutoSize      = true;
            lblLang.Location      = new System.Drawing.Point(44, 34);

            _pnlMicStatus.Controls.Add(micAccent);
            _pnlMicStatus.Controls.Add(_pnlMicIndicator);
            _pnlMicStatus.Controls.Add(_lblMicStatus);
            _pnlMicStatus.Controls.Add(lblLang);
            table.Controls.Add(_pnlMicStatus, 0, 0);

            // ── Fila 1: panel ESPAÑOL ───────────────────────────────────
            var pnlSpanish           = new System.Windows.Forms.Panel();
            pnlSpanish.Dock          = System.Windows.Forms.DockStyle.Fill;
            pnlSpanish.BackColor     = C_SURFACE;
            pnlSpanish.Margin        = new System.Windows.Forms.Padding(0, 6, 0, 3);

            var spAccent             = new System.Windows.Forms.Panel();
            spAccent.Dock            = System.Windows.Forms.DockStyle.Top;
            spAccent.Height          = 3;
            spAccent.BackColor       = C_ACCENT;

            var lblSpanishHeader     = MakeHeader("🇪🇸  ESPAÑOL  —  voz reconocida", C_ACCENT);
            lblSpanishHeader.Dock    = System.Windows.Forms.DockStyle.Top;
            lblSpanishHeader.Height  = 30;

            _rtbSpanish              = new System.Windows.Forms.RichTextBox();
            _rtbSpanish.Dock         = System.Windows.Forms.DockStyle.Fill;
            _rtbSpanish.ReadOnly     = true;
            _rtbSpanish.BackColor    = C_SURFACE;
            _rtbSpanish.ForeColor    = C_TEXT;
            _rtbSpanish.Font         = new System.Drawing.Font("Segoe UI", 17f);
            _rtbSpanish.BorderStyle  = System.Windows.Forms.BorderStyle.None;
            _rtbSpanish.Padding      = new System.Windows.Forms.Padding(10, 4, 10, 4);

            pnlSpanish.Controls.Add(_rtbSpanish);
            pnlSpanish.Controls.Add(lblSpanishHeader);
            pnlSpanish.Controls.Add(spAccent);
            table.Controls.Add(pnlSpanish, 0, 1);

            // ── Fila 2: panel QUECHUA ───────────────────────────────────
            var pnlQuechua           = new System.Windows.Forms.Panel();
            pnlQuechua.Dock          = System.Windows.Forms.DockStyle.Fill;
            pnlQuechua.BackColor     = C_CARD;
            pnlQuechua.Margin        = new System.Windows.Forms.Padding(0, 3, 0, 6);

            var quAccent             = new System.Windows.Forms.Panel();
            quAccent.Dock            = System.Windows.Forms.DockStyle.Top;
            quAccent.Height          = 3;
            quAccent.BackColor       = C_GREEN;

            var lblQuechuaHeader     = MakeHeader("🌿  QUECHUA BOLIVIANO  —  traducción", C_GREEN);
            lblQuechuaHeader.Dock    = System.Windows.Forms.DockStyle.Top;
            lblQuechuaHeader.Height  = 30;

            _rtbQuechua              = new System.Windows.Forms.RichTextBox();
            _rtbQuechua.Dock         = System.Windows.Forms.DockStyle.Fill;
            _rtbQuechua.ReadOnly     = true;
            _rtbQuechua.BackColor    = C_CARD;
            _rtbQuechua.ForeColor    = C_GREEN;
            _rtbQuechua.Font         = new System.Drawing.Font("Segoe UI", 17f,
                System.Drawing.FontStyle.Bold);
            _rtbQuechua.BorderStyle  = System.Windows.Forms.BorderStyle.None;
            _rtbQuechua.Padding      = new System.Windows.Forms.Padding(10, 4, 10, 4);

            pnlQuechua.Controls.Add(_rtbQuechua);
            pnlQuechua.Controls.Add(lblQuechuaHeader);
            pnlQuechua.Controls.Add(quAccent);
            table.Controls.Add(pnlQuechua, 0, 2);

            // ── Fila 3: confianza ───────────────────────────────────────
            _lblConfidence           = new System.Windows.Forms.Label();
            _lblConfidence.Text      = "Confianza del reconocimiento: —";
            _lblConfidence.Dock      = System.Windows.Forms.DockStyle.Fill;
            _lblConfidence.ForeColor = C_TEXT_DIM;
            _lblConfidence.Font      = new System.Drawing.Font("Segoe UI", 8.5f);
            _lblConfidence.Padding   = new System.Windows.Forms.Padding(4, 4, 0, 0);
            _lblConfidence.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            table.Controls.Add(_lblConfidence, 0, 3);

            // ── Fila 4: cuadro de texto manual ─────────────────────────
            var pnlManual           = new System.Windows.Forms.Panel();
            pnlManual.Dock          = System.Windows.Forms.DockStyle.Fill;
            pnlManual.BackColor     = C_BG;
            pnlManual.Padding       = new System.Windows.Forms.Padding(0, 4, 0, 4);

            _txtManualInput              = new System.Windows.Forms.TextBox();
            _txtManualInput.Dock         = System.Windows.Forms.DockStyle.Fill;
            _txtManualInput.BackColor    = C_INPUT_BG;
            _txtManualInput.ForeColor    = C_TEXT;
            _txtManualInput.Font         = new System.Drawing.Font("Segoe UI", 10.5f);
            _txtManualInput.BorderStyle  = System.Windows.Forms.BorderStyle.FixedSingle;
            _txtManualInput.PlaceholderText = "✏  Escribe en español y presiona Enter para traducir al Quechua Boliviano…";
            _txtManualInput.TabStop      = true;
            _txtManualInput.TabIndex     = 0;
            _txtManualInput.KeyPress    += TxtManualInput_KeyPress;

            pnlManual.Controls.Add(_txtManualInput);
            table.Controls.Add(pnlManual, 0, 4);

            // ── Fila 5: botones ─────────────────────────────────────────
            var pnlButtons           = new System.Windows.Forms.Panel();
            pnlButtons.Dock          = System.Windows.Forms.DockStyle.Fill;
            pnlButtons.BackColor     = C_BG;
            pnlButtons.Padding       = new System.Windows.Forms.Padding(0, 8, 0, 0);

            _btnStartVoice        = MakeBtn("🎤  Iniciar Traducción en Vivo", C_GREEN, 230, 44);
            _btnStartVoice.Location = new System.Drawing.Point(0, 0);
            _btnStartVoice.Click  += BtnStartVoice_Click;

            _btnStopVoice         = MakeBtn("⏹  Detener", C_RED, 160, 44);
            _btnStopVoice.Location  = new System.Drawing.Point(238, 0);
            _btnStopVoice.Enabled = false;
            _btnStopVoice.Click   += BtnStopVoice_Click;

            _btnTranslateText     = MakeBtn("💬  Traducir manualmente", C_ACCENT, 220, 44);
            _btnTranslateText.Location = new System.Drawing.Point(406, 0);
            _btnTranslateText.Click += BtnTranslateText_Click;

            pnlButtons.Controls.Add(_btnStartVoice);
            pnlButtons.Controls.Add(_btnStopVoice);
            pnlButtons.Controls.Add(_btnTranslateText);
            table.Controls.Add(pnlButtons, 0, 5);

            _tabVoice.Controls.Add(table);
        }

        // ────────────────────────────────────────────────────────────────
        // TAB: HISTORIAL
        // ────────────────────────────────────────────────────────────────
        private void BuildHistoryTab()
        {
            _tabHistory.BackColor = C_BG;

            _lvHistory            = new System.Windows.Forms.ListView();
            _lvHistory.Dock       = System.Windows.Forms.DockStyle.Fill;
            _lvHistory.View       = System.Windows.Forms.View.Details;
            _lvHistory.FullRowSelect = true;
            _lvHistory.GridLines  = true;
            _lvHistory.BackColor  = C_SURFACE;
            _lvHistory.ForeColor  = C_TEXT;
            _lvHistory.Font       = new System.Drawing.Font("Segoe UI", 9.5f);
            _lvHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;

            _lvHistory.Columns.Add("Hora",     90,  System.Windows.Forms.HorizontalAlignment.Left);
            _lvHistory.Columns.Add("Español",  360, System.Windows.Forms.HorizontalAlignment.Left);
            _lvHistory.Columns.Add("Quechua",  360, System.Windows.Forms.HorizontalAlignment.Left);
            _lvHistory.Columns.Add("Palabras",  80, System.Windows.Forms.HorizontalAlignment.Center);
            _lvHistory.Columns.Add("Confianza", 90, System.Windows.Forms.HorizontalAlignment.Center);

            var pnlHistBtns       = new System.Windows.Forms.Panel();
            pnlHistBtns.Dock      = System.Windows.Forms.DockStyle.Bottom;
            pnlHistBtns.Height    = 56;
            pnlHistBtns.BackColor = C_CARD;
            pnlHistBtns.Padding   = new System.Windows.Forms.Padding(10, 8, 10, 8);

            _btnExportTxt         = MakeBtn("📄  Exportar TXT", C_ACCENT, 160, 36);
            _btnExportTxt.Location  = new System.Drawing.Point(0, 0);
            _btnExportTxt.Click   += BtnExportTxt_Click;

            _btnExportPdf         = MakeBtn("📑  Exportar PDF", C_PURPLE, 160, 36);
            _btnExportPdf.Location  = new System.Drawing.Point(170, 0);
            _btnExportPdf.Click   += BtnExportPdf_Click;

            _btnClearHistory      = MakeBtn("🗑  Limpiar", C_RED, 140, 36);
            _btnClearHistory.Location = new System.Drawing.Point(340, 0);
            _btnClearHistory.Click += BtnClearHistory_Click;

            pnlHistBtns.Controls.Add(_btnExportTxt);
            pnlHistBtns.Controls.Add(_btnExportPdf);
            pnlHistBtns.Controls.Add(_btnClearHistory);

            _tabHistory.Controls.Add(_lvHistory);
            _tabHistory.Controls.Add(pnlHistBtns);
        }

        // ────────────────────────────────────────────────────────────────
        // TAB: ACERCA DE
        // ────────────────────────────────────────────────────────────────
        private void BuildAboutTab()
        {
            _tabAbout.BackColor = C_BG;

            var rtbAbout           = new System.Windows.Forms.RichTextBox();
            rtbAbout.Dock          = System.Windows.Forms.DockStyle.Top;
            rtbAbout.Height        = 270;
            rtbAbout.ReadOnly      = true;
            rtbAbout.BackColor     = C_SURFACE;
            rtbAbout.ForeColor     = C_TEXT;
            rtbAbout.Font          = new System.Drawing.Font("Segoe UI", 10.5f);
            rtbAbout.BorderStyle   = System.Windows.Forms.BorderStyle.None;
            rtbAbout.Padding       = new System.Windows.Forms.Padding(12);
            rtbAbout.Text          =
                "Qhichwa  —  Compilador & Traductor de Voz Boliviano\r\n" +
                new string('─', 55) + "\r\n\r\n" +
                "Motor de traducción integrado con el compilador:\r\n" +
                "  • Lee src/lexer.c  → extrae tabla KEYWORDS[] (vocabulario quechua)\r\n" +
                "  • Lee src/parser.c → extrae frases de 2 palabras (mana chayqa, aswan hatun…)\r\n" +
                "  • Construye mapa español → Qhichwa Sudboliviano desde esa fuente\r\n\r\n" +
                "Reconocimiento de voz:\r\n" +
                "  Motor: Windows SAPI 5  |  Idioma forzado: es-MX (Español México)\r\n" +
                "  Traducción: Quechua Boliviano (Qhichwa)  |  Latencia: < 500 ms\r\n\r\n" +
                "Exportación: TXT · PDF (iTextSharp)\r\n" +
                "Versión: 1.0.0  |  .NET 10-windows  |  Windows Forms\r\n";

            var lblVocab           = new System.Windows.Forms.Label();
            lblVocab.Text          = "  Vocabulario cargado desde src/lexer.c y src/parser.c:";
            lblVocab.ForeColor     = C_GREEN;
            lblVocab.Font          = new System.Drawing.Font("Segoe UI", 9.5f,
                System.Drawing.FontStyle.Bold);
            lblVocab.Dock          = System.Windows.Forms.DockStyle.Top;
            lblVocab.Height        = 28;
            lblVocab.TextAlign     = System.Drawing.ContentAlignment.MiddleLeft;

            var rtbVocab           = new System.Windows.Forms.RichTextBox();
            rtbVocab.Dock          = System.Windows.Forms.DockStyle.Fill;
            rtbVocab.ReadOnly      = true;
            rtbVocab.BackColor     = C_CARD;
            rtbVocab.ForeColor     = C_PURPLE;
            rtbVocab.Font          = new System.Drawing.Font("Cascadia Code", 9.5f);
            rtbVocab.BorderStyle   = System.Windows.Forms.BorderStyle.None;
            rtbVocab.Name          = "rtbCompilerVocab";

            _tabAbout.Controls.Add(rtbVocab);
            _tabAbout.Controls.Add(lblVocab);
            _tabAbout.Controls.Add(rtbAbout);
        }

        // ────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────
        private static System.Windows.Forms.Label MakeHeader(string text, System.Drawing.Color color)
        {
            return new System.Windows.Forms.Label
            {
                Text      = text,
                ForeColor = color,
                Font      = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold),
                AutoSize  = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding   = new System.Windows.Forms.Padding(8, 0, 0, 0),
                BackColor = System.Drawing.Color.Transparent
            };
        }

        private static System.Windows.Forms.Button MakeBtn(
            string text, System.Drawing.Color bg, int w, int h)
        {
            var btn = new System.Windows.Forms.Button
            {
                Text      = text,
                BackColor = bg,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Font      = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold),
                Cursor    = System.Windows.Forms.Cursors.Hand,
                Size      = new System.Drawing.Size(w, h),
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize  = 0;
            btn.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(
                Math.Min(bg.R + 30, 255),
                Math.Min(bg.G + 30, 255),
                Math.Min(bg.B + 30, 255));
            return btn;
        }

        // ── Fields ──────────────────────────────────────────────────────
        private System.Windows.Forms.Panel           _headerPanel    = null!;
        private System.Windows.Forms.Label           _lblTitle       = null!;
        private System.Windows.Forms.Label           _lblSubtitle    = null!;
        private System.Windows.Forms.TabControl      _tabControl     = null!;
        private System.Windows.Forms.TabPage         _tabCompiler    = null!;
        private System.Windows.Forms.TabPage         _tabVoice       = null!;
        private System.Windows.Forms.TabPage         _tabHistory     = null!;
        private System.Windows.Forms.TabPage         _tabAbout       = null!;
        private System.Windows.Forms.StatusStrip     _statusStrip    = null!;
        private System.Windows.Forms.ToolStripStatusLabel _statusLabel = null!;
        private System.Windows.Forms.ToolStripStatusLabel _statusWords = null!;
        private System.Windows.Forms.Timer           _pulseTimer     = null!;

        // Compiler tab
        private System.Windows.Forms.RichTextBox _rtbCompilerInput  = null!;
        private System.Windows.Forms.RichTextBox _rtbCompilerOutput = null!;
        private System.Windows.Forms.Button      _btnCompile        = null!;

        // Voice tab
        private System.Windows.Forms.Panel       _pnlMicStatus    = null!;
        private System.Windows.Forms.Panel       _pnlMicIndicator = null!;
        private System.Windows.Forms.Label       _lblMicStatus    = null!;
        private System.Windows.Forms.RichTextBox _rtbSpanish      = null!;
        private System.Windows.Forms.RichTextBox _rtbQuechua      = null!;
        private System.Windows.Forms.Label       _lblConfidence   = null!;
        private System.Windows.Forms.Button      _btnStartVoice   = null!;
        private System.Windows.Forms.Button      _btnStopVoice    = null!;
        private System.Windows.Forms.Button      _btnTranslateText = null!;
        private System.Windows.Forms.TextBox     _txtManualInput  = null!;

        // History tab
        private System.Windows.Forms.ListView _lvHistory       = null!;
        private System.Windows.Forms.Button   _btnExportTxt    = null!;
        private System.Windows.Forms.Button   _btnExportPdf    = null!;
        private System.Windows.Forms.Button   _btnClearHistory = null!;
    }
}
