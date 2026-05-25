using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ComponentBOMTool.Models;
using ComponentBOMTool.Services;
using ComponentBOMTool.UI;

namespace ComponentBOMTool
{
    public partial class MainForm : Form
    {
        // UI controls
        private DataGridView grid = null!;
        private Button btnLoad = null!;
        private Button btnProcess = null!;
        private Button btnExport = null!;
        private TextBox txtSearch = null!;
        private Label lblTotal = null!;
        private Label lblStatus = null!;
        private ProgressBar progress = null!;

        // Data + processor
        private List<BomItem> data = new();
        private BomProcessor processor = null!;

        public MainForm()
        {
            InitializeComponent();

            // ✅ API key popup at startup
            using (var apiForm = new ApiKeyForm())
            {
                if (apiForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(apiForm.ApiKey))
                {
                    processor = new BomProcessor(apiForm.ApiKey.Trim());
                    lblStatus.Text = "API key set. Ready.";
                }
                else
                {
                    MessageBox.Show("API Key is required. Application will close.",
                        "Component BOM Tool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Environment.Exit(0);
                }
            }
        }

        private void InitializeComponent()
        {
            // Form
            Text = "Component BOM Tool";
            Width = 1350;
            Height = 760;
            Font = new Font("Segoe UI", 10);
            StartPosition = FormStartPosition.CenterScreen;

            // =====================================================
            // HEADER (Title left + capgemini.png logo right)
            // =====================================================
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 80,
                ColumnCount = 2,
                BackColor = Color.FromArgb(245, 247, 250)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            var title = new Label
            {
                Text = "Component BOM Tool",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };

            var logoPanel = new Panel { Dock = DockStyle.Fill };

            var logo = new PictureBox
            {
                Width = 220,
                Height = 100,
                SizeMode = PictureBoxSizeMode.Zoom,
                Anchor = AnchorStyles.Right
            };

            // ✅ Load embedded capgemini.png (inside EXE)
            logo.Image = LoadEmbeddedCapgeminiLogo();

            // Position logo nicely on resize
            logoPanel.Resize += (_, __) =>
            {
                logo.Left = Math.Max(0, logoPanel.Width - logo.Width - 20);
                logo.Top = Math.Max(0, (logoPanel.Height - logo.Height) / 2);
            };
            logoPanel.Controls.Add(logo);

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(logoPanel, 1, 0);

            // =====================================================
            // TOOLBAR (2 rows)
            // Row 1: buttons + status
            // Row 2: search box full width
            // =====================================================
            var toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 95,
                ColumnCount = 5,
                RowCount = 2,
                Padding = new Padding(15, 8, 15, 8),
                BackColor = Color.FromArgb(245, 247, 250)
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            btnLoad = new Button { Text = "📂 Load CSV", Width = 140, Height = 36, Margin = new Padding(5) };
            btnProcess = new Button { Text = "🚀 Process", Width = 140, Height = 36, Margin = new Padding(5) };
            btnExport = new Button { Text = "💾 Export CSV", Width = 150, Height = 36, Margin = new Padding(5) };

            btnLoad.Click += BtnLoad_Click;
            btnProcess.Click += BtnProcess_Click;
            btnExport.Click += BtnExport_Click;

            lblStatus = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.DimGray,
                Margin = new Padding(5)
            };

            txtSearch = new TextBox
            {
                PlaceholderText = "🔍 Search Part Number...",
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            toolbar.Controls.Add(btnLoad, 0, 0);
            toolbar.Controls.Add(btnProcess, 1, 0);
            toolbar.Controls.Add(btnExport, 2, 0);
            toolbar.Controls.Add(lblStatus, 3, 0);

            toolbar.Controls.Add(txtSearch, 0, 1);
            toolbar.SetColumnSpan(txtSearch, 5);

            // =====================================================
            // GRID
            // =====================================================
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 120, 212);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            // =====================================================
            // PROGRESS + TOTAL
            // =====================================================
            progress = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                Visible = false,
                Style = ProgressBarStyle.Marquee
            };

            lblTotal = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 38,
                Text = "💰 Total Cost: $0",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                BackColor = Color.White
            };

            // Add controls (order matters)
            Controls.Add(grid);
            Controls.Add(progress);
            Controls.Add(lblTotal);
            Controls.Add(toolbar);
            Controls.Add(header);
        }

        // =====================================================
        // ✅ Embedded logo loader for capgemini.png (no RESX needed)
        // =====================================================
        private Image? LoadEmbeddedCapgeminiLogo()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();

                // ✅ auto-detect correct embedded resource name
                var resName = asm.GetManifestResourceNames()
                                 .FirstOrDefault(n => n.EndsWith("capgemini.png", StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrWhiteSpace(resName))
                    return null;

                using Stream? s = asm.GetManifestResourceStream(resName);
                if (s == null) return null;

                return Image.FromStream(s);
            }
            catch
            {
                return null;
            }
        }

        // =====================================================
        // Popups
        // =====================================================
        private void ShowError(string message)
        {
            MessageBox.Show(message, "⚠ Component BOM Tool Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ShowInfo(string message)
        {
            MessageBox.Show(message, "Component BOM Tool",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // =====================================================
        // LOAD CSV (safe even if open in Excel)
        // =====================================================
        private void BtnLoad_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "CSV Files|*.csv",
                Title = "Select BOM CSV File"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                lblStatus.Text = "Loading CSV...";

                string[] lines;
                using (var stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                }

                if (lines.Length <= 1)
                {
                    ShowError("CSV appears empty or invalid.");
                    lblStatus.Text = "CSV load failed";
                    return;
                }

                data = lines
                    .Skip(1)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(ParseCsvLineToBomItem)
                    .Where(x => !string.IsNullOrWhiteSpace(x.PartNumber) && x.Quantity > 0)
                    .ToList();

                grid.DataSource = null;
                grid.DataSource = data;

                ConfigureGridSafe();

                lblTotal.Text = "💰 Total Cost: $0";
                lblStatus.Text = $"Loaded {data.Count} parts";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "CSV load failed";
                ShowError("CSV Load Error:\n" + ex.Message);
            }
        }

        private BomItem ParseCsvLineToBomItem(string line)
        {
            // Note: This handles most BOM CSVs. If your CSV has commas inside quotes,
            // we can replace with a proper CSV parser later.
            var parts = line.Split(',')
                            .Select(v => v.Replace("\"", "").Trim())
                            .ToArray();

            string pn = parts.Length > 0 ? parts[0] : "";
            string qtyStr = parts.Length > 1 ? parts[1] : "0";

            return new BomItem
            {
                PartNumber = pn,
                Quantity = SafeInt(qtyStr)
            };
        }

        // =====================================================
        // PROCESS BOM
        // =====================================================
        private async void BtnProcess_Click(object? sender, EventArgs e)
        {
            try
            {
                if (data == null || data.Count == 0)
                {
                    ShowError("Please load a BOM CSV before processing.");
                    return;
                }

                btnProcess.Enabled = false;
                btnLoad.Enabled = false;
                btnExport.Enabled = false;

                progress.Visible = true;
                progress.Style = ProgressBarStyle.Marquee;
                lblStatus.Text = "Processing BOM (Mouser API)...";

                List<BomItem> result = await processor.ProcessAsync(data);

                data = result;

                grid.DataSource = null;
                grid.DataSource = data;

                ConfigureGridSafe();

                decimal total = result.Sum(x => x.TotalPrice ?? 0);
                lblTotal.Text = $"💰 Total Cost: ${total:N2}";
                lblStatus.Text = $"Processing complete - {result.Count} parts";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Processing failed";
                ShowError(
                    "Mouser processing failed.\n\n" +
                    "Possible causes:\n" +
                    "- API key invalid\n" +
                    "- Mouser API rate limit reached\n" +
                    "- Network issue\n" +
                    "- Max retries exceeded\n\n" +
                    "Details:\n" + ex.Message
                );
            }
            finally
            {
                progress.Visible = false;
                btnProcess.Enabled = true;
                btnLoad.Enabled = true;
                btnExport.Enabled = true;
            }
        }

        // =====================================================
        // SEARCH
        // =====================================================
        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            try
            {
                string search = txtSearch.Text.Trim();

                var filtered = data
                    .Where(x => x.PartNumber != null &&
                                x.PartNumber.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                grid.DataSource = null;
                grid.DataSource = filtered;

                ConfigureGridSafe();

                lblStatus.Text = string.IsNullOrWhiteSpace(search)
                    ? $"Showing {data.Count} parts"
                    : $"Filtered {filtered.Count} parts";
            }
            catch (Exception ex)
            {
                ShowError("Search Error:\n" + ex.Message);
            }
        }

        // =====================================================
        // EXPORT CSV
        // =====================================================
        private void BtnExport_Click(object? sender, EventArgs e)
        {
            try
            {
                if (data == null || data.Count == 0)
                {
                    ShowError("No processed data available to export.");
                    return;
                }

                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "BOM_Result.csv"
                );

                using StreamWriter sw = new StreamWriter(path);
                sw.WriteLine("Part,Qty,Mfr,Lifecycle,Stock,UnitPrice,TotalPrice,Alternates");

                foreach (var row in data)
                {
                    sw.WriteLine(
                        $"{Csv(row.PartNumber)}," +
                        $"{row.Quantity}," +
                        $"{Csv(row.Manufacturer)}," +
                        $"{Csv(row.Lifecycle)}," +
                        $"{Csv(row.Stock)}," +
                        $"{row.UnitPrice}," +
                        $"{row.TotalPrice}," +
                        $"{Csv(row.Alternates)}"
                    );
                }

                lblStatus.Text = "Export complete";
                ShowInfo("✅ Exported to Desktop:\n" + path);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Export failed";
                ShowError("Export Error:\n" + ex.Message);
            }
        }

        // =====================================================
        // GRID FORMAT
        // =====================================================
        private void ConfigureGridSafe()
        {
            if (grid.Columns.Count == 0)
                return;

            SetHeader("PartNumber", "Part Number");
            SetHeader("Quantity", "Quantity");
            SetHeader("Manufacturer", "Mfr (Mouser)");
            SetHeader("Lifecycle", "Lifecycle (Mouser)");
            SetHeader("Stock", "Stock (Mouser)");
            SetHeader("UnitPrice", "Unit Price (Mouser)");
            SetHeader("TotalPrice", "Total Price (Mouser)");
            SetHeader("Alternates", "Alternates (Mouser)");

            FormatCurrency("UnitPrice");
            FormatCurrency("TotalPrice");

            AlignRight("Quantity");
            AlignRight("UnitPrice");
            AlignRight("TotalPrice");

            if (grid.Columns.Contains("Alternates"))
                grid.Columns["Alternates"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            ColorLifecycleCells();
        }

        private void SetHeader(string columnName, string headerText)
        {
            if (grid.Columns.Contains(columnName))
                grid.Columns[columnName].HeaderText = headerText;
        }

        private void FormatCurrency(string columnName)
        {
            if (grid.Columns.Contains(columnName))
                grid.Columns[columnName].DefaultCellStyle.Format = "C2";
        }

        private void AlignRight(string columnName)
        {
            if (grid.Columns.Contains(columnName))
                grid.Columns[columnName].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private void ColorLifecycleCells()
        {
            if (!grid.Columns.Contains("Lifecycle"))
                return;

            foreach (DataGridViewRow row in grid.Rows)
            {
                string lifecycle = row.Cells["Lifecycle"].Value?.ToString() ?? "";

                if (lifecycle.Contains("Obsolete", StringComparison.OrdinalIgnoreCase) ||
                    lifecycle.Contains("End of Life", StringComparison.OrdinalIgnoreCase))
                {
                    row.Cells["Lifecycle"].Style.ForeColor = Color.Red;
                    row.Cells["Lifecycle"].Style.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                }
                else if (lifecycle.Contains("New", StringComparison.OrdinalIgnoreCase) ||
                         lifecycle.Contains("Active", StringComparison.OrdinalIgnoreCase))
                {
                    row.Cells["Lifecycle"].Style.ForeColor = Color.Green;
                    row.Cells["Lifecycle"].Style.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                }
            }
        }

        // =====================================================
        // HELPERS
        // =====================================================
        private int SafeInt(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0;

            input = input.Replace("\"", "").Trim();
            return int.TryParse(input, out int value) ? value : 0;
        }

        private string Csv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            value = value.Replace("\"", "\"\"");
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return $"\"{value}\"";

            return value;
        }
    }
}