using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using HuffPressStudio.Controls;

namespace HuffPressStudio
{
    // The main window. It creates the controls, lays them out and connects the
    // buttons to the C++ engine (through HuffmanService).
    internal sealed class MainForm : Form
    {
        private const long MaxFileBytes = 256L * 1024 * 1024;

        // visual controls
        private readonly ParticleHeader _header = new ParticleHeader();
        private readonly DropZone _drop = new DropZone();
        private readonly GlowButton _openButton = new GlowButton();
        private readonly GlowButton _compressButton = new GlowButton();
        private readonly GlowButton _decompressButton = new GlowButton();
        private readonly GlowButton _saveButton = new GlowButton();
        private readonly GlowButton _sampleButton = new GlowButton();
        private readonly StatCard _originalCard = new StatCard("ORIGINAL SIZE", Theme.Cyan);
        private readonly StatCard _compressedCard = new StatCard("RESULT SIZE", Theme.Violet);
        private readonly StatCard _ratioCard = new StatCard("SIZE RATIO", Theme.Pink);
        private readonly StatCard _entropyCard = new StatCard("ENTROPY", Theme.Green);
        private readonly FrequencyChart _chart = new FrequencyChart();
        private readonly DonutGauge _gauge = new DonutGauge();
        private readonly HuffmanTreeControl _tree = new HuffmanTreeControl();
        private readonly CodeTableCard _table = new CodeTableCard();
        private readonly StatusFooter _status = new StatusFooter();
        private readonly System.Windows.Forms.Timer _fadeTimer = new System.Windows.Forms.Timer();

        // current work
        private byte[] _data;               // the file that is loaded
        private string _dataPath;
        private bool _isPacked;             // true when the loaded file is a .hpz archive
        private byte[] _result;             // output of the last Compress / Decompress
        private string _resultName;
        private bool _busy;

        // samples
        private readonly List<string> _samples = new List<string>();
        private int _nextSample;

        public MainForm()
        {
            Text = "HuffPress Studio - Huffman compression lab";
            BackColor = Theme.Background;
            AutoScaleMode = AutoScaleMode.None;     // we scale everything ourselves (see Theme.S)
            DoubleBuffered = true;
            AllowDrop = true;
            StartPosition = FormStartPosition.CenterScreen;

            ApplyWindowSize();
            SetFormIcon();
            CreateControls();
            WireEvents();
            FindSamples();
            ShowEmptyState();
            UpdateButtons();
            LayoutControls();

            Opacity = 0.0;      // the fade-in timer starts in OnShown
        }

        // setup

        private void ApplyWindowSize()
        {
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            int width = Math.Min(Theme.S(1180), area.Width - Theme.S(30));
            int height = Math.Min(Theme.S(740), area.Height - Theme.S(60));
            ClientSize = new Size(width, height);
            MinimumSize = SizeFromClientSize(new Size(Math.Min(Theme.S(980), width), Math.Min(Theme.S(640), height)));
        }

        private void SetFormIcon()
        {
            try
            {
                string path = Path.Combine(ProjectAssets.AssetsFolder, "app.ico");
                if (File.Exists(path)) Icon = new Icon(path);
            }
            catch (Exception)
            {
                // the default icon is fine
            }
        }

        private void CreateControls()
        {
            _header.EngineInfo = HuffmanService.EngineVersion();

            ConfigureButton(_openButton, "Open file", "icon_open.png", Theme.Blue, Theme.Cyan);
            ConfigureButton(_compressButton, "Compress", "icon_compress.png", Theme.Violet, Theme.Pink);
            ConfigureButton(_decompressButton, "Decompress", "icon_decompress.png", Theme.Orange, Color.FromArgb(255, 70, 110));
            ConfigureButton(_saveButton, "Save result", "icon_save.png", Color.FromArgb(40, 200, 120), Theme.Cyan);
            ConfigureButton(_sampleButton, "Try a sample", "icon_sample.png", Color.FromArgb(110, 110, 170), Color.FromArgb(70, 80, 150));

            Controls.Add(_header);
            Controls.Add(_drop);
            Controls.Add(_openButton);
            Controls.Add(_compressButton);
            Controls.Add(_decompressButton);
            Controls.Add(_saveButton);
            Controls.Add(_sampleButton);
            Controls.Add(_originalCard);
            Controls.Add(_compressedCard);
            Controls.Add(_ratioCard);
            Controls.Add(_entropyCard);
            Controls.Add(_chart);
            Controls.Add(_gauge);
            Controls.Add(_tree);
            Controls.Add(_table);
            Controls.Add(_status);

            _fadeTimer.Interval = 15;
        }

        private static void ConfigureButton(GlowButton button, string text, string iconFile, Color accent1, Color accent2)
        {
            button.Text = text;
            button.Icon = ProjectAssets.Load(iconFile);
            button.Accent1 = accent1;
            button.Accent2 = accent2;
        }

        private void WireEvents()
        {
            _openButton.Click += delegate { BrowseForFile(); };
            _drop.BrowseRequested += delegate { BrowseForFile(); };
            _drop.FileDropped += delegate (object sender, string path) { LoadFile(path); };
            _sampleButton.Click += OnSampleClicked;
            _compressButton.Click += OnCompressClicked;
            _decompressButton.Click += OnDecompressClicked;
            _saveButton.Click += OnSaveClicked;

            DragEnter += OnFormDragEnter;
            DragDrop += OnFormDragDrop;
            Resize += delegate { LayoutControls(); };
            _fadeTimer.Tick += OnFadeTick;
        }

        // layout

        // Places every control. Called at start-up and on every resize.
        private void LayoutControls()
        {
            int width = ClientSize.Width;
            int height = ClientSize.Height;
            if (width <= 0 || height <= 0) return;

            int pad = Theme.S(18);
            int gap = Theme.S(14);
            int headerHeight = Theme.S(128);
            int statusHeight = Theme.S(32);

            _header.SetBounds(0, 0, width, headerHeight);
            _status.SetBounds(0, height - statusHeight, width, statusHeight);

            int top = headerHeight + pad;
            int bottom = height - statusHeight - pad;
            int available = bottom - top;

            int sidebarWidth = Theme.S(280);
            LayoutSidebar(pad, top, sidebarWidth, available, gap);
            LayoutDashboard(pad + sidebarWidth + gap, top, width - pad - (pad + sidebarWidth + gap), available, gap);
        }

        private void LayoutSidebar(int x, int top, int sidebarWidth, int available, int gap)
        {
            GlowButton[] buttons = { _openButton, _compressButton, _decompressButton, _saveButton, _sampleButton };
            int buttonHeight = Theme.S(48);
            int buttonGap = Theme.S(10);
            int buttonsTotal = buttons.Length * buttonHeight + (buttons.Length - 1) * buttonGap;
            int dropHeight = Math.Max(Theme.S(150), Math.Min(Theme.S(200), available - buttonsTotal - gap));

            int y = top;
            _drop.SetBounds(x, y, sidebarWidth, dropHeight);
            y += dropHeight + gap;
            foreach (GlowButton button in buttons)
            {
                button.SetBounds(x, y, sidebarWidth, buttonHeight);
                y += buttonHeight + buttonGap;
            }
        }

        private void LayoutDashboard(int x, int top, int width, int available, int gap)
        {
            // row 0: four number cards
            int cardHeight = Theme.S(84);
            int cardWidth = (width - 3 * gap) / 4;
            StatCard[] cards = { _originalCard, _compressedCard, _ratioCard, _entropyCard };
            for (int i = 0; i < cards.Length; i++)
            {
                int w = i == cards.Length - 1 ? width - 3 * (cardWidth + gap) : cardWidth;
                cards[i].SetBounds(x + i * (cardWidth + gap), top, w, cardHeight);
            }

            // rows 1 and 2 share the remaining height
            int rowsTop = top + cardHeight + gap;
            int rowHeight = (available - cardHeight - 2 * gap) / 2;
            int row2Top = rowsTop + rowHeight + gap;
            int row2Height = top + available - row2Top;

            int gaugeWidth = Theme.S(240);
            _chart.SetBounds(x, rowsTop, width - gaugeWidth - gap, rowHeight);
            _gauge.SetBounds(x + width - gaugeWidth, rowsTop, gaugeWidth, rowHeight);

            int treeWidth = (int)(width * 0.54);
            _tree.SetBounds(x, row2Top, treeWidth, row2Height);
            _table.SetBounds(x + treeWidth + gap, row2Top, width - treeWidth - gap, row2Height);
        }

        // window effects

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int useDark = 1;
                NativeMethods.DwmSetWindowAttribute(Handle, 20, ref useDark, sizeof(int));   // dark title bar (Windows 10/11)
            }
            catch (Exception)
            {
                // older Windows keeps the normal title bar
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _fadeTimer.Start();
            LoadFirstSample();
        }

        private void OnFadeTick(object sender, EventArgs e)
        {
            Opacity = Math.Min(1.0, Opacity + 0.07);
            if (Opacity >= 1.0) _fadeTimer.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _fadeTimer.Dispose();
            base.Dispose(disposing);
        }

        // loading files

        private void BrowseForFile()
        {
            if (_busy) return;
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose a file to analyse";
                dialog.Filter = "All files (*.*)|*.*|HuffPress archives (*.hpz)|*.hpz";
                if (dialog.ShowDialog(this) == DialogResult.OK) LoadFile(dialog.FileName);
            }
        }

        private void OnFormDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnFormDragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0) LoadFile(files[0]);
        }

        private void LoadFile(string path)
        {
            if (_busy) return;

            if (Directory.Exists(path))
            {
                ShowStatus("Please choose a file - folders are not supported.", StatusKind.Error);
                return;
            }

            try
            {
                FileInfo info = new FileInfo(path);
                if (!info.Exists)
                {
                    ShowStatus("File not found: " + path, StatusKind.Error);
                    return;
                }
                if (info.Length > MaxFileBytes)
                {
                    ShowStatus("That file is larger than 256 MB - please pick a smaller one.", StatusKind.Error);
                    return;
                }
                if (info.Length == 0)
                {
                    ShowStatus("That file is empty - there is nothing to compress.", StatusKind.Error);
                    return;
                }

                _data = File.ReadAllBytes(path);
                _dataPath = path;
                _isPacked = path.EndsWith(".hpz", StringComparison.OrdinalIgnoreCase) && HuffmanService.LooksPacked(_data);
                _result = null;
                _resultName = null;
                ShowLoadedFile();
            }
            catch (IOException ex)
            {
                ShowStatus("Could not read the file: " + ex.Message, StatusKind.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowStatus("No permission to read the file: " + ex.Message, StatusKind.Error);
            }
            UpdateButtons();
        }

        // Analyses the loaded file and refreshes every chart.
        private void ShowLoadedFile()
        {
            string name = Path.GetFileName(_dataPath);
            uint[] freq = _isPacked ? HuffmanService.ReadHeaderFrequencies(_data) : HuffmanService.CountFrequencies(_data);
            Analysis analysis = HuffmanService.Analyze(freq);

            _chart.SetData(analysis.Frequencies);
            _tree.SetCodes(analysis.Codes);
            _table.SetCodes(analysis.Codes);
            _entropyCard.SetValue(analysis.Entropy, FormatEntropy);

            if (_isPacked)
            {
                _drop.ShowFile(name, Theme.FormatBytes(_data.Length) + "  -  HuffPress archive");
                ShowResultNumbers(analysis.TotalBytes, _data.Length);
                ShowStatus("Loaded archive " + name + ". Press Decompress to restore the original.", StatusKind.Info);
            }
            else
            {
                _drop.ShowFile(name, Theme.FormatBytes(_data.Length) + "  -  " + analysis.DistinctSymbols + " different bytes");
                _originalCard.SetValue(_data.Length, Theme.FormatBytes);
                _compressedCard.Reset();
                _ratioCard.Reset();
                _gauge.Reset();
                ShowStatus("Loaded " + name + ". Press Compress to shrink it.", StatusKind.Info);
            }
        }

        private void ShowEmptyState()
        {
            _chart.Clear();
            _tree.SetCodes(null);
            _table.SetCodes(new List<CodeInfo>());
            _originalCard.Reset();
            _compressedCard.Reset();
            _ratioCard.Reset();
            _entropyCard.Reset();
            _gauge.Reset();
            ShowStatus("Drop a file anywhere, or press Open file to begin.", StatusKind.Info);
        }

        // samples

        private void FindSamples()
        {
            string folder = ProjectAssets.SamplesFolder;
            if (!Directory.Exists(folder)) return;
            string[] files = Directory.GetFiles(folder);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            _samples.AddRange(files);
        }

        private void LoadFirstSample()
        {
            for (int i = 0; i < _samples.Count; i++)
            {
                if (Path.GetFileName(_samples[i]).Equals("sample_text.txt", StringComparison.OrdinalIgnoreCase))
                {
                    _nextSample = i;
                    break;
                }
            }
            OnSampleClicked(this, EventArgs.Empty);
        }

        private void OnSampleClicked(object sender, EventArgs e)
        {
            if (_busy) return;
            if (_samples.Count == 0)
            {
                ShowStatus("The Samples folder was not found next to the program.", StatusKind.Error);
                return;
            }
            LoadFile(_samples[_nextSample]);
            _nextSample = (_nextSample + 1) % _samples.Count;
        }

        // compress / decompress / save

        private async void OnCompressClicked(object sender, EventArgs e)
        {
            if (_data == null || _isPacked || _busy) return;

            byte[] input = _data;
            string sourceName = Path.GetFileName(_dataPath);
            SetBusy(true, "Compressing " + sourceName + " with the C++ engine...");
            try
            {
                Stopwatch timer = Stopwatch.StartNew();
                CompressionOutcome outcome = await Task.Run(() => HuffmanService.CompressAndVerify(input));
                timer.Stop();

                if (!outcome.Verified)
                {
                    throw new InvalidOperationException("verification failed - the restored data differs from the original.");
                }

                _result = outcome.Packed;
                _resultName = sourceName + ".hpz";
                ShowResultNumbers(input.Length, outcome.Packed.Length);

                double saved = 100.0 * (1.0 - (double)outcome.Packed.Length / input.Length);
                string message = "Compressed " + Theme.FormatBytes(input.Length) + " to " + Theme.FormatBytes(outcome.Packed.Length)
                               + " (" + saved.ToString("0.0", CultureInfo.InvariantCulture) + "% saved)"
                               + "  |  round-trip verified  |  " + timer.ElapsedMilliseconds + " ms";
                if (saved <= 0.0) message = "This file is already dense - Huffman coding cannot shrink it. Try a text file.";
                ShowStatus(message, saved > 0.0 ? StatusKind.Success : StatusKind.Info);
            }
            catch (Exception ex)
            {
                ShowStatus("Compression failed: " + ex.Message, StatusKind.Error);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private async void OnDecompressClicked(object sender, EventArgs e)
        {
            if (_data == null || !_isPacked || _busy) return;

            byte[] packed = _data;
            string sourceName = Path.GetFileName(_dataPath);
            SetBusy(true, "Decompressing " + sourceName + "...");
            try
            {
                Stopwatch timer = Stopwatch.StartNew();
                byte[] restored = await Task.Run(() => HuffmanService.Decompress(packed));
                timer.Stop();

                _result = restored;
                _resultName = RestoredName(sourceName);
                ShowResultNumbers(restored.Length, packed.Length);
                ShowStatus("Restored " + Theme.FormatBytes(restored.Length) + " in " + timer.ElapsedMilliseconds
                           + " ms. Press Save result to write it to disk.", StatusKind.Success);
            }
            catch (Exception ex)
            {
                ShowStatus("Decompression failed: " + ex.Message, StatusKind.Error);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            if (_result == null || _busy) return;

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Save result";
                dialog.FileName = _resultName;
                dialog.InitialDirectory = _dataPath != null ? Path.GetDirectoryName(_dataPath) : "";
                dialog.Filter = _isPacked
                    ? "All files (*.*)|*.*"
                    : "HuffPress archive (*.hpz)|*.hpz|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    File.WriteAllBytes(dialog.FileName, _result);
                    ShowStatus("Saved " + Theme.FormatBytes(_result.Length) + " to " + dialog.FileName, StatusKind.Success);
                }
                catch (IOException ex)
                {
                    ShowStatus("Could not save the file: " + ex.Message, StatusKind.Error);
                }
                catch (UnauthorizedAccessException ex)
                {
                    ShowStatus("No permission to save there: " + ex.Message, StatusKind.Error);
                }
            }
        }

        private static string RestoredName(string archiveName)
        {
            if (archiveName.EndsWith(".hpz", StringComparison.OrdinalIgnoreCase) && archiveName.Length > 4)
            {
                return archiveName.Substring(0, archiveName.Length - 4);
            }
            return archiveName + ".restored";
        }

        // updating the dashboard

        // Fills the number cards and the gauge for a (original size, result size) pair.
        private void ShowResultNumbers(long originalBytes, long packedBytes)
        {
            double ratio = originalBytes > 0 ? (double)packedBytes / originalBytes : 1.0;
            double saved = 1.0 - ratio;

            _originalCard.SetValue(originalBytes, Theme.FormatBytes);
            _compressedCard.SetValue(packedBytes, Theme.FormatBytes);
            _ratioCard.SetValue(ratio * 100.0, FormatPercent);
            _gauge.SetValue(saved, saved > 0.0 ? "space saved" : "no gain");
        }

        private static string FormatPercent(double value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatEntropy(double value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture) + " bits/byte";
        }

        private void ShowStatus(string message, StatusKind kind)
        {
            _status.SetMessage(message, kind);
        }

        private void SetBusy(bool busy, string message)
        {
            _busy = busy;
            _status.SetBusy(busy);
            if (busy && message != null) ShowStatus(message, StatusKind.Info);
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool hasData = _data != null;
            _openButton.Enabled = !_busy;
            _sampleButton.Enabled = !_busy;
            _compressButton.Enabled = hasData && !_isPacked && !_busy;
            _decompressButton.Enabled = hasData && _isPacked && !_busy;
            _saveButton.Enabled = _result != null && !_busy;
        }
    }
}
