using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LogoAtlasApp
{
    public class MainForm : Form
    {
        private Button btnSelect;
        private NumericUpDown numColumns;
        private Label lblColumns;
        private Button btnGenerate;
        private Button btnOpenSaved;
        private PictureBox pictureBox;
        private List<string> images = new List<string>();
        private Label lblStatus;
        private Label lblResolution;
        private ComboBox comboResolution;
        private Label lblPadding;
        private NumericUpDown numPadding;
        private string? lastSavedPath;

        public MainForm()
        {
            Text = "Logo Atlas Creator";
            Width = 800;
            Height = 600;

            btnSelect = new Button { Text = "Select PNGs", Left = 10, Top = 10, Width = 100 };
            btnSelect.Click += BtnSelect_Click;

            lblColumns = new Label { Text = "Columns:", Left = 130, Top = 15, Width = 60 };
            // Limit columns to a maximum of 6 as requested
            numColumns = new NumericUpDown { Left = 200, Top = 10, Width = 60, Minimum = 1, Maximum = 6, Value = 4 };

            lblResolution = new Label { Text = "Resolution:", Left = 280, Top = 15, Width = 70 };
            comboResolution = new ComboBox { Left = 360, Top = 10, Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            comboResolution.Items.AddRange(new object[] { "1024", "2048", "4096" });
            comboResolution.SelectedIndex = 1; // default 2048

            lblPadding = new Label { Text = "Padding:", Left = 470, Top = 15, Width = 60 };
            numPadding = new NumericUpDown { Left = 530, Top = 10, Width = 60, Minimum = 0, Maximum = 512, Value = 4 };

            btnGenerate = new Button { Text = "Generate Atlas", Left = 610, Top = 10, Width = 140 };
            btnGenerate.Click += BtnGenerate_Click;

            // Button to open the last saved atlas
            btnOpenSaved = new Button { Text = "Open Saved", Left = 610, Top = 40, Width = 140, Enabled = false };
            btnOpenSaved.Click += BtnOpenSaved_Click;

            lblStatus = new Label { Left = 10, Top = 40, Width = 580 };

            pictureBox = new PictureBox { Left = 10, Top = 80, Width = 760, Height = 480, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };

            Controls.Add(btnSelect);
            Controls.Add(lblColumns);
            Controls.Add(numColumns);
            Controls.Add(lblResolution);
            Controls.Add(comboResolution);
            Controls.Add(lblPadding);
            Controls.Add(numPadding);
            Controls.Add(btnGenerate);
            Controls.Add(btnOpenSaved);
            Controls.Add(lblStatus);
            Controls.Add(pictureBox);
        }

        private void BtnSelect_Click(object? sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "PNG Files|*.png";
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    images = ofd.FileNames.ToList();
                    lblStatus.Text = $"Selected {images.Count} images";
                }
            }
        }

        private void BtnOpenSaved_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(lastSavedPath))
            {
                MessageBox.Show("No saved atlas to open.");
                btnOpenSaved.Enabled = false;
                return;
            }

            if (!File.Exists(lastSavedPath))
            {
                MessageBox.Show("Saved atlas file not found.");
                btnOpenSaved.Enabled = false;
                return;
            }

            try
            {
                var psi = new ProcessStartInfo(lastSavedPath)
                {
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open file: {ex.Message}");
            }
        }

        private void BtnGenerate_Click(object? sender, EventArgs e)
        {
            if (images.Count == 0)
            {
                MessageBox.Show("No images selected");
                return;
            }

            int cols = (int)numColumns.Value;
            if (cols < 1) cols = 1;
            if (cols > 6) cols = 6; // safety clamp

            // compute required rows to hold all images, then clamp to max 4 rows
            int rows = (int)Math.Ceiling(images.Count / (double)cols);
            if (rows < 1) rows = 1;

            const int MaxRows = 4;
            if (rows > MaxRows)
            {
                int capacity = MaxRows * cols;
                MessageBox.Show($"Selected {images.Count} images but the atlas grid is limited to a maximum of {MaxRows} rows and 6 columns. Only the first {capacity} images will be used.", "Grid Capacity", MessageBoxButtons.OK, MessageBoxIcon.Information);
                rows = MaxRows;
            }

            int atlasSize = 2048;
            if (comboResolution.SelectedItem != null)
            {
                if (!int.TryParse(comboResolution.SelectedItem.ToString(), out atlasSize))
                    atlasSize = 2048;
            }

            int padding = (int)numPadding.Value;

            // Determine how many images will actually be used (cols * rows)
            int capacityUsed = cols * rows;
            var imagesToUse = images.Take(capacityUsed).ToList();

            // Compute inner cell size in the atlas accounting for padding around and between cells.
            // We'll use padding as the space on all outer edges and between cells: total horizontal padding = (cols + 1) * padding.
            int totalPaddingX = (cols + 1) * padding;
            int totalPaddingY = (rows + 1) * padding;

            int availableW = atlasSize - totalPaddingX;
            int availableH = atlasSize - totalPaddingY;

            if (availableW <= 0 || availableH <= 0)
            {
                MessageBox.Show("Padding too large for selected resolution/columns causing no space for images. Reduce padding or increase resolution/columns.", "Invalid Padding", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int cellInnerW = availableW / cols;
            int cellInnerH = availableH / rows;

            if (cellInnerW <= 0 || cellInnerH <= 0)
            {
                MessageBox.Show("Computed cell size is zero or negative. Adjust padding/columns/resolution.", "Invalid Cell Size", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Load bitmaps for only the images we'll use
            var bitmaps = imagesToUse.Select(p => (Bitmap)Image.FromFile(p)).ToList();

            var atlas = new Bitmap(atlasSize, atlasSize);
            using (var g = Graphics.FromImage(atlas))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                for (int i = 0; i < bitmaps.Count; i++)
                {
                    int r = i / cols;
                    int c = i % cols;

                    // top-left of this cell's inner area
                    int x = padding + c * (cellInnerW + padding);
                    int y = padding + r * (cellInnerH + padding);

                    var bmp = bitmaps[i];

                    // Scale image so it fits inside the inner cell while preserving aspect ratio.
                    float scale = Math.Min(cellInnerW / (float)bmp.Width, cellInnerH / (float)bmp.Height);
                    int drawW = (int)Math.Round(bmp.Width * scale);
                    int drawH = (int)Math.Round(bmp.Height * scale);

                    // Center inside the inner cell
                    int offsetX = x + (cellInnerW - drawW) / 2;
                    int offsetY = y + (cellInnerH - drawH) / 2;

                    g.DrawImage(bmp, new Rectangle(offsetX, offsetY, drawW, drawH));
                }
            }

            foreach (var b in bitmaps) b.Dispose();

            pictureBox.Image?.Dispose();
            pictureBox.Image = atlas;

            lblStatus.Text = $"Generated atlas {atlasSize}x{atlasSize} using {bitmaps.Count} images ({cols}x{rows} grid) padding={padding}px";

            // Save dialog
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Files|*.png";
                sfd.FileName = "atlas.png";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    atlas.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    lblStatus.Text = $"Saved atlas to {Path.GetFileName(sfd.FileName)}";
                    lastSavedPath = sfd.FileName;
                    btnOpenSaved.Enabled = true;
                }
            }
        }
    }
}