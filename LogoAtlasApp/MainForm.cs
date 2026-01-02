using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LogoAtlasApp
{
    /// <summary>
    /// Main window for the Logo Atlas Creator application.
    /// Provides UI to select PNGs, configure grid (columns/rows), padding and resolution,
    /// preview the atlas layout, and generate/save the final atlas.
    /// </summary>
    public class MainForm : Form
    {
        // UI controls used across the form
        private Button btnSelect;           // Button to open file dialog and select PNG files
        private NumericUpDown numColumns;   // Numeric control for number of columns (1..6)
        private Label lblColumns;           // Label for columns control
        private NumericUpDown numRows;      // Numeric control for number of rows (1..4)
        private Label lblRows;              // Label for rows control
        private Button btnGenerate;         // Button to generate atlas PNG
        private Button btnOpenSaved;        // Button to open the last saved atlas
        private PictureBox pictureBox;      // Preview area showing the atlas grid and thumbnails
        private List<string> images = new List<string>(); // Paths of selected images
        private List<Image>? cachedImages;  // Cached loaded images for preview to avoid repeated disk I/O
        private Label lblStatus;            // Status label to show messages to the user
        private Label lblResolution;        // Label for resolution dropdown
        private ComboBox comboResolution;   // Dropdown to pick atlas resolution (1024/2048/4096)
        private Label lblPadding;           // Label for padding control
        private NumericUpDown numPadding;   // Numeric control for padding in pixels
        private string? lastSavedPath;      // Path to last saved atlas (for Open Saved)
        private ToolTip tooltip;            // ToolTip instance used to show help for each control

        private const int MaxColumns = 6; // maximum allowed columns
        private const int MaxRows = 4;    // maximum allowed rows

        /// <summary>
        /// Construct the main form, create controls, wire events and draw initial preview.
        /// </summary>
        public MainForm()
        {
            // Basic window properties
            Text = "Logo Atlas Creator";
            Width = 800;
            Height = 600;

            // Initialize controls
            btnSelect = new Button { Text = "Select PNGs", Width = 100, Height = 24 };
            btnSelect.Click += BtnSelect_Click;

            lblColumns = new Label { Text = "Columns:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            // Limit columns to a maximum of 6 as requested
            numColumns = new NumericUpDown { Width = 60, Minimum = 1, Maximum = MaxColumns, Value = 4 };
            // When columns change, ensure grid can fit selected images and update preview
            numColumns.ValueChanged += (s, e) => { EnsureGridCapacity(); UpdatePreviewGrid(); };

            lblRows = new Label { Text = "Rows:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            // Limit rows to a maximum of 4 as requested
            numRows = new NumericUpDown { Width = 60, Minimum = 1, Maximum = MaxRows, Value = 1 };
            // When rows change, ensure grid can fit selected images and update preview
            numRows.ValueChanged += (s, e) => { EnsureGridCapacity(); UpdatePreviewGrid(); };

            lblResolution = new Label { Text = "Resolution:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            comboResolution = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList }; 
            comboResolution.Items.AddRange(new object[] { "1024", "2048", "4096" });
            comboResolution.SelectedIndex = 1; // default 2048
            comboResolution.SelectedIndexChanged += (s, e) => UpdatePreviewGrid();

            lblPadding = new Label { Text = "Padding:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            numPadding = new NumericUpDown { Width = 60, Minimum = 0, Maximum = 512, Value = 4 };
            numPadding.ValueChanged += (s, e) => UpdatePreviewGrid();

            // Generate button: creates the atlas using current settings and selected images
            btnGenerate = new Button { Text = "Generate Atlas", Width = 120, Height = 28 };
            btnGenerate.Click += BtnGenerate_Click;

            // Button to open the last saved atlas (disabled until save happens)
            btnOpenSaved = new Button { Text = "Open Saved", Width = 120, Height = 28, Enabled = false };
            btnOpenSaved.Click += BtnOpenSaved_Click;

            // Status label: informs user of selections, saves, and generation results
            lblStatus = new Label { AutoSize = false, Height = 22, Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleLeft };

            // Create a panel that flows controls and docks to the top so it resizes nicely with the window
            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(8),
                Height = 110
            };

            // Add controls to the flow panel with small margins
            topPanel.Controls.Add(btnSelect);
            topPanel.SetFlowBreak(btnSelect, false);

            topPanel.Controls.Add(lblColumns);
            topPanel.Controls.Add(numColumns);

            topPanel.Controls.Add(lblRows);
            topPanel.Controls.Add(numRows);

            topPanel.Controls.Add(lblResolution);
            topPanel.Controls.Add(comboResolution);

            topPanel.Controls.Add(lblPadding);
            topPanel.Controls.Add(numPadding);

            topPanel.Controls.Add(btnGenerate);
            topPanel.Controls.Add(btnOpenSaved);

            // PictureBox shows a scaled preview of the atlas and grid layout and fills remaining space
            pictureBox = new PictureBox { BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };

            // Add controls to the form in proper docking order
            Controls.Add(pictureBox);
            Controls.Add(lblStatus);
            Controls.Add(topPanel);

            // Initialize and configure ToolTip component
            tooltip = new ToolTip();
            tooltip.AutoPopDelay = 5000;    // How long the tooltip stays visible
            tooltip.InitialDelay = 500;     // Delay before tooltip appears
            tooltip.ReshowDelay = 100;      // Delay before showing subsequent tooltips
            tooltip.ShowAlways = true;      // Show even if the parent control is not active

            // Set tooltips for all interactive controls
            tooltip.SetToolTip(btnSelect, "Open a dialog to select multiple PNG images to include in the atlas.");
            tooltip.SetToolTip(numColumns, "Number of columns in the atlas grid (1 to 6).");
            tooltip.SetToolTip(lblColumns, "Label for columns control.");
            tooltip.SetToolTip(numRows, "Number of rows in the atlas grid (1 to 4).");
            tooltip.SetToolTip(lblRows, "Label for rows control.");
            tooltip.SetToolTip(comboResolution, "Output atlas resolution in pixels (choose 1024, 2048 or 4096).");
            tooltip.SetToolTip(lblResolution, "Label for resolution dropdown.");
            tooltip.SetToolTip(numPadding, "Padding in pixels placed around and between images in the atlas.");
            tooltip.SetToolTip(lblPadding, "Label for padding control.");
            tooltip.SetToolTip(btnGenerate, "Generate and save the atlas PNG using the current settings and selected images.");
            tooltip.SetToolTip(btnOpenSaved, "Open the last saved atlas file with the default system viewer.");
            tooltip.SetToolTip(pictureBox, "Preview of the atlas layout. Thumbnails of selected images are shown in their grid cells.");
            tooltip.SetToolTip(lblStatus, "Status messages about selection, generation and save operations.");

            // Draw initial empty grid preview so users see the layout immediately
            UpdatePreviewGrid();
        }

        /// <summary>
        /// Handler for the Select PNGs button. Opens file dialog and stores selected file paths.
        /// Also updates rows automatically based on columns and number of selected images.
        /// </summary>
        private void BtnSelect_Click(object? sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "PNG Files|*.png";
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // Store selected images and notify user
                    images = ofd.FileNames.ToList();
                    lblStatus.Text = $"Selected {images.Count} images";

                    // Dispose old cached images and load new ones into cache
                    DisposeCachedImages();
                    LoadImagesToCache();

                    // Ensure the grid settings can accommodate the selected images; this may adjust rows/columns and inform the user
                    EnsureGridCapacity();
                    UpdatePreviewGrid();
                }
            }
        }

        /// <summary>
        /// Handler for the Open Saved button. Attempts to open the last saved atlas file with the system default program.
        /// </summary>
        private void BtnOpenSaved_Click(object? sender, EventArgs e)
        {
            // Validate path and existence
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

            // Launch external process to open the file
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

        /// <summary>
        /// Handler for the Generate Atlas button. Validates inputs, computes the grid and cell sizes,
        /// loads the selected images (up to the grid capacity), composes the atlas bitmap and prompts the user to save it.
        /// </summary>
        private void BtnGenerate_Click(object? sender, EventArgs e)
        {
            // Ensure images were selected
            if (images.Count == 0)
            {
                MessageBox.Show("No images selected");
                return;
            }

            // Columns: enforce allowed range
            int cols = (int)numColumns.Value;
            if (cols < 1) cols = 1;
            if (cols > 6) cols = 6; // safety clamp

            // Rows: use user-selected rows but enforce max rows constant
            int rows = (int)numRows.Value;
            if (rows < 1) rows = 1;
            const int MaxRows = 4;
            if (rows > MaxRows) rows = MaxRows;

            // If there are more images than cells, inform user that only first N will be used
            int capacity = cols * rows;
            if (images.Count > capacity)
            {
                MessageBox.Show($"Selected {images.Count} images but the atlas grid capacity is {capacity}. Only the first {capacity} images will be used.", "Grid Capacity", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // Parse atlas resolution from dropdown, default to 2048 if parsing fails
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
            int totalPaddingX = (cols + 1) * padding;
            int totalPaddingY = (rows + 1) * padding;

            int availableW = atlasSize - totalPaddingX;
            int availableH = atlasSize - totalPaddingY;

            // Validate that padding/resolution/columns/rows combination leaves room for images
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

            // Load bitmaps for only the images we'll use - these are disposed after drawing
            var bitmaps = imagesToUse.Select(p => (Bitmap)Image.FromFile(p)).ToList();

            // Create atlas bitmap and draw all images into their computed cell rectangles
            var atlas = new Bitmap(atlasSize, atlasSize);
            using (var g = Graphics.FromImage(atlas))
            {
                // Prepare drawing surfaces and quality settings
                g.Clear(Color.Transparent);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                for (int i = 0; i < bitmaps.Count; i++)
                {
                    int r = i / cols; // row index in grid
                    int c = i % cols; // column index in grid

                    // top-left of this cell's inner area (accounting for outer padding and spacing)
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

            // Dispose loaded bitmaps to free memory
            foreach (var b in bitmaps) b.Dispose();

            // Set the generated atlas as the PictureBox image (disposing previous preview)
            pictureBox.Image?.Dispose();
            pictureBox.Image = atlas;

            lblStatus.Text = $"Generated atlas {atlasSize}x{atlasSize} using {bitmaps.Count} images ({cols}x{rows} grid) padding={padding}px";

            // Prompt the user to save the atlas PNG file
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

        /// <summary>
        /// Updates the PictureBox preview. Draws a scaled representation of the atlas grid, cell boxes and image thumbnails
        /// so the user can see how the final atlas will be arranged.
        /// </summary>
        private void UpdatePreviewGrid()
        {
            // Read current UI settings
            int cols = (int)numColumns.Value;
            int rows = (int)numRows.Value;
            int atlasSize = 2048;
            if (comboResolution.SelectedItem != null)
            {
                if (!int.TryParse(comboResolution.SelectedItem.ToString(), out atlasSize))
                    atlasSize = 2048;
            }
            int padding = (int)numPadding.Value;

            // Compute inner cell size in atlas coordinates
            int totalPaddingX = (cols + 1) * padding;
            int totalPaddingY = (rows + 1) * padding;
            int availableW = Math.Max(1, atlasSize - totalPaddingX);
            int availableH = Math.Max(1, atlasSize - totalPaddingY);
            int cellInnerW = Math.Max(1, availableW / Math.Max(1, cols));
            int cellInnerH = Math.Max(1, availableH / Math.Max(1, rows));

            // Create a small preview bitmap sized to the PictureBox control
            int pw = Math.Max(1, pictureBox.Width - 2);
            int ph = Math.Max(1, pictureBox.Height - 2);
            var preview = new Bitmap(pw, ph);
            using (var g = Graphics.FromImage(preview))
            {
                // Background and high-quality drawing settings for preview
                g.Clear(Color.DarkGray);
                // Use high-quality compositing and interpolation to produce smooth thumbnails
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                // scale factor from atlas pixels to preview pixels
                float scale = Math.Min(pw / (float)atlasSize, ph / (float)atlasSize);
                float startX = padding * scale; // left margin in preview
                float startY = padding * scale; // top margin in preview
                float scaledCellW = cellInnerW * scale; // scaled width of a single cell
                float scaledCellH = cellInnerH * scale; // scaled height of a single cell
                float spacing = padding * scale; // scaled spacing between cells

                using (var pen = new Pen(Color.FromArgb(200, Color.Black), 1))
                using (var gridPen = new Pen(Color.FromArgb(200, Color.LightYellow), 2))
                {
                    // Draw each cell rectangle and, if available, a thumbnail for the corresponding image
                    int used = Math.Min(images.Count, cols * rows);
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            // Cell rectangle position in preview coordinates
                            float x = startX + c * (scaledCellW + spacing);
                            float y = startY + r * (scaledCellH + spacing);

                            var rect = new RectangleF(x, y, scaledCellW, scaledCellH);

                            // Fill and border for the cell to make it visible in preview
                            g.FillRectangle(Brushes.DimGray, rect);
                            g.DrawRectangle(pen, x, y, scaledCellW, scaledCellH);

                            int idx = r * cols + c; // index of image for this cell
                            if (idx < images.Count)
                            {
                                try
                                {
                                    // Use cached image for thumbnail instead of loading from disk.
                                    // This prevents lag when adjusting padding or other settings.
                                    if (cachedImages != null && idx < cachedImages.Count && cachedImages[idx] != null)
                                    {
                                        var img = cachedImages[idx];
                                        float imgScale = Math.Min(scaledCellW / img.Width, scaledCellH / img.Height);
                                        int dw = Math.Max(1, (int)(img.Width * imgScale));
                                        int dh = Math.Max(1, (int)(img.Height * imgScale));
                                        int dx = (int)(x + (scaledCellW - dw) / 2);
                                        int dy = (int)(y + (scaledCellH - dh) / 2);
                                        g.DrawImage(img, new Rectangle(dx, dy, dw, dh));
                                    }
                                }
                                catch
                                {
                                    // Ignore preview image load errors; preview is only advisory
                                }
                            }
                        }
                    }

                    // Draw an outer border around the atlas area for clarity
                    float atlasW = (padding + cols * (cellInnerW + padding)) * scale;
                    float atlasH = (padding + rows * (cellInnerH + padding)) * scale;
                    g.DrawRectangle(gridPen, 0 + 0.5f, 0 + 0.5f, atlasW - 1, atlasH - 1);
                }
            }

            // Replace the PictureBox image with the generated preview, disposing previous image
            pictureBox.Image?.Dispose();
            pictureBox.Image = preview;
        }

        /// <summary>
        /// Ensure the current grid (columns x rows) can accommodate the selected images.
        /// If not, attempt to increase rows first, then columns. If impossible, clamp to maximum capacity and inform user.
        /// </summary>
        private void EnsureGridCapacity()
        {
            if (images == null || images.Count == 0)
                return;

            int cols = (int)numColumns.Value;
            int rows = (int)numRows.Value;
            int capacity = cols * rows;

            // If current grid already fits, nothing to do
            if (images.Count <= capacity)
                return;

            // First try to increase rows to fit all images (without exceeding MaxRows)
            int neededRows = (int)Math.Ceiling(images.Count / (double)cols);
            if (neededRows <= MaxRows)
            {
                numRows.Value = neededRows;
                MessageBox.Show($"Adjusted rows to {neededRows} to fit {images.Count} images.", "Grid Adjusted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // If increasing rows alone isn't enough, try increasing columns (keeping current rows)
            int neededCols = (int)Math.Ceiling(images.Count / (double)rows);
            if (neededCols <= MaxColumns)
            {
                numColumns.Value = neededCols;
                MessageBox.Show($"Adjusted columns to {neededCols} to fit {images.Count} images.", "Grid Adjusted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // If neither single-dimension increase fits, clamp to maximum capacity and inform user
            numColumns.Value = MaxColumns;
            numRows.Value = MaxRows;
            int maxCapacity = MaxColumns * MaxRows;
            MessageBox.Show($"Selected {images.Count} images exceed the maximum grid capacity of {maxCapacity}. The grid has been set to {MaxColumns}x{MaxRows} and only the first {maxCapacity} images will be used.", "Grid Capacity Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Load selected images into cache to avoid repeated disk I/O during preview updates.
        /// This prevents lag when adjusting padding or other settings.
        /// </summary>
        private void LoadImagesToCache()
        {
            if (images == null || images.Count == 0)
            {
                cachedImages = null;
                return;
            }

            cachedImages = new List<Image>();
            foreach (var path in images)
            {
                try
                {
                    cachedImages.Add(Image.FromFile(path));
                }
                catch
                {
                    // If an image fails to load, add null to maintain index alignment
                    cachedImages.Add(null!);
                }
            }
        }

        /// <summary>
        /// Dispose all cached images to free memory when selecting new images.
        /// </summary>
        private void DisposeCachedImages()
        {
            if (cachedImages != null)
            {
                foreach (var img in cachedImages)
                {
                    img?.Dispose();
                }
                cachedImages = null;
            }
        }
    }
}