using Org.BouncyCastle.Tls.Crypto;
using PdfiumViewer;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;


namespace Scanner
{
    public class ScanForm : Form
    {
        private Panel mainAppPanel;
        private Panel scanSelectionPanel;
        private Panel pdfPreviewPanel;

        // ************  OBJECTS in scanSelectionPanel ************  
        private Label statusLabel;
        private Label pageLabel;


        // View scanned BMP files
        private FlowLayoutPanel scanFilesPanel;
        private PictureBox pictureBox1;

       // private GroupBox scanButtonGroup;
        private Button startScanButton;
        private Button scanMorePagesButton;
        private Button doneScanningButton;

        private GroupBox docDeliveryGroup;
        private CheckBox emailCheckBox;
        private CheckBox usbCheckBox;

        private GroupBox colorModeGroup;
        private RadioButton colorRadioButton;
        private RadioButton grayscaleRadioButton;
        private RadioButton blackWhiteRadioButton;

        private GroupBox dpiGroup;
        private RadioButton standardDpiRadioButton;
        private RadioButton highDpiRadioButton;

        private GroupBox docTypeGroup;
        private CheckBox pdfCheckBox;
        private CheckBox bmpCheckBox;


        private ScannerService scanner;

        private bool scannerOpen;



        // Security Image
        private readonly int securityButtonWidth = 100;
        private readonly int securityButtonHeight = 100;
        private Button exitSecurityButtonButton;

        // ************  OBJECTS in scanSelectionPanel ************  
        // PDF Review
        private MemoryStream pdfStream;
        private PdfDocument pdfDocument;
        private PdfViewer pdfViewer;
        private Label pdfPreviewLabel;
        private Button acceptPdfButton;
        private Button discardPdfButton;

        // ************  UI configuration ************  
        // Main Windows Form Width and Hight
        private readonly int appFormWidth = 1900;  
        private readonly static int appFormHeight = 1000; 
        //private readonly int appFormWidth = 1700; \\ Testing Setting
        //private readonly static int appFormHeight =1000; \\ Testing Setting

        // mainAppPanel Width and Hight
        private readonly static int mainAppPanelWidth = 1800;
        private readonly static int mainAppPanelHeight = appFormHeight - 50;

        // scanSelectionPanel and pdfPreviewPanel Width and Hight
        private readonly static int PanelWidth = 700;
        private readonly static int PanelHeight = mainAppPanelHeight - 60;


        // Used for alignment of UI objects in scanSelectionPanel 
        private readonly static int ColumnDistance = 235;
        private readonly static int Column1 = 20;
        private readonly static int Column2 = Column1 + ColumnDistance;
        private readonly static int Column3 = Column2 + ColumnDistance;

        private readonly static int RowDistanceControl = 130;
        private readonly static int RowDistanceText = 40;
        private readonly static int Row1 = 0;
        private readonly static int Row2 = Row1 + RowDistanceControl;
        private readonly static int Row3 = Row2 + RowDistanceText;
        private readonly static int Row4 = Row3 + RowDistanceText;
        private readonly static int Row5 = Row4 + RowDistanceControl;
        private readonly static int Row6 = Row5 + RowDistanceControl;
        private readonly static int Row7 = Row6 + RowDistanceControl;

        private readonly static int GroupWidth = 650;
        private readonly static int GroupHeight = 75;
        private readonly static int CenterOfGroupRow = 30;

        private readonly static int thumbnailWidth = 220;
        private readonly static int thumbnailHeight = 160;

        private readonly Color selectedButtonBackColor = Color.LightGray;
        private readonly Color selectedButtonForeColor = Color.White;
        private readonly int fontTextSize = 15;
        private readonly int fontButtonTextSize = 15;
        private readonly int widthButton = 230;
        private readonly int heightButton = 45;

        private readonly string scannningInstructionsText = "Scanning Instructions";
        private readonly string scannedDocuemtnPreviewText = "Scanned Document Preview";
        private enum ScanMode
        {
            BlackAndWhite,
            Grayscale,
            Color
        }


        private ScanMode selectedScanMode = ScanMode.BlackAndWhite;



        // =========================================================
        // Create and Initialize Scan Form
        // =========================================================

        public ScanForm()
        {
            Text =
                "City of McKinney Public Library Scanner";

            // Configuration.
            this.WindowState = FormWindowState.Normal; // Testing
            //this.WindowState = FormWindowState.Maximized; // Production 
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(appFormWidth, appFormHeight);
            this.AutoScroll = true;
            this.BackColor = Color.White;

            InitializeUI();

            FormClosing +=
                ScanForm_FormClosing;


        }


        // =========================================================
        // Initialize ALL UI Objects
        // =========================================================

        private void InitializeUI()
        {
            mainAppPanel = new Panel
            {
                Width = mainAppPanelWidth,
                Height = mainAppPanelHeight,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White
            };

            Controls.Add(mainAppPanel);

            scanSelectionPanel = new Panel
            {
                Location = new Point(10, 20),
                Size = new Size(PanelWidth, PanelHeight+30),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White

            };
            
            mainAppPanel.Controls.Add(scanSelectionPanel);


            // center initial main application panels
            mainAppPanel.Location = new Point(
                (ClientSize.Width - mainAppPanel.Width) / 2,
                (ClientSize.Height - mainAppPanel.Height) / 2
            );

            // Re-center main application panels whenever the window changes size
            Resize += (s, e) =>
            {
                mainAppPanel.Left = Math.Max(
                    0,
                    (ClientSize.Width - mainAppPanel.Width) / 2);

                mainAppPanel.Top = Math.Max(
                    0,
                    (ClientSize.Height - mainAppPanel.Height) / 2);
            };

            // Initialize Fonts
            Globals.boldUiFont = new Font(
                Font.FontFamily,
                fontButtonTextSize,
                FontStyle.Bold);

            Globals.normalUiFont = new Font(
                Font.FontFamily,
                fontTextSize);

            Globals.toolbarFont = new Font(
                Font.FontFamily,
                12,
                FontStyle.Regular);

            // Scanned BMP files PREVIEW PANEL
            scanFilesPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 250,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.White
            };

            pictureBox1 = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };

            scanFilesPanel.Controls.Add(pictureBox1);
            mainAppPanel.Controls.Add(scanFilesPanel);


            // exit App if passowrd is entered.
            exitSecurityButtonButton = new Button
            {
                Width = securityButtonWidth,
                Height = securityButtonHeight,
                Location = new Point(Column1, Row1),
                BackColor = selectedButtonBackColor,
                ForeColor = selectedButtonForeColor,
                Font = Globals.boldUiFont,
                Enabled = true
            };

            exitSecurityButtonButton.Click +=
                ExitSecurityButtonButton_Click;


            exitSecurityButtonButton.Image = Image.FromFile(Globals.exitAppImageFile);
            exitSecurityButtonButton.ImageAlign = ContentAlignment.MiddleCenter;

            scanSelectionPanel.Controls.Add(exitSecurityButtonButton);


            // PDF PREVIEW PANEL
            pdfPreviewPanel = new Panel
            {
                Location = new Point(760, 50),
                Size = new Size(PanelWidth, PanelHeight),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            mainAppPanel.Controls.Add(pdfPreviewPanel);


            // PDF PREVIEW HEADER
            Panel pdfPreviewHeaderPanel = new Panel
            {
                Location = new Point(760, 20),
                Size = new Size(PanelWidth, PanelHeight),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            

            pdfPreviewLabel = new Label
            {
                Text = scannningInstructionsText,
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Green,
                //ForeColor = ColorTranslator.FromHtml("#007282"),
                Font = Globals.boldUiFont
            };

            pdfPreviewHeaderPanel.Controls.Add(pdfPreviewLabel);
            mainAppPanel.Controls.Add(pdfPreviewHeaderPanel);

            // PDF VIEWER
            pdfViewer = new PdfViewer
            {
                Dock = DockStyle.Fill,
                ShowToolbar = true
            };

            // --- STRIP OUT SAVE AND PRINT BUTTONS ---
            // Look for the inner ToolStrip control inside the PdfViewer
            foreach (Control control in pdfViewer.Controls)
            {
                if (control is ToolStrip toolStrip)
                {
                    // Loop backwards through the items so removing them doesn't break the index loop
                    for (int i = toolStrip.Items.Count - 1; i >= 0; i--)
                    {
                        toolStrip.ImageScalingSize = new Size(32, 32);

                        ToolStripItem item = toolStrip.Items[i];
                        string name = item.Name.ToLower();
                        string text = item.Text.ToLower();

                        // Look for keywords indicating Save, SaveAs, or Print items
                        if (name.Contains("save") || text.Contains("save") ||
                            name.Contains("print") || text.Contains("print"))
                        {
                            // Option A: Hide them completely
                            item.Visible = false;

                        } else
                        {
                            // 2. Make the text font larger for the items you KEEP visible
                            // This keeps the existing font family/style but bumps the size to 12pt
                            item.Font = Globals.toolbarFont;

                            // 3. Optional: Force the item to scale its image up to match the new ImageScalingSize
                            item.ImageScaling = ToolStripItemImageScaling.SizeToFit;
                        }
                    }
                }
            }

            
            pdfPreviewPanel.Controls.Add(pdfViewer);


            // PDF BUTTON PANEL
            Panel pdfButtonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = GroupHeight,
                BackColor = Color.White

            };

            acceptPdfButton = new Button
            {
                Text = "Accept Document",
                Width = widthButton + 30,
                Height = heightButton,
                Location = new Point(50, 10),
                BackColor = selectedButtonBackColor,
                ForeColor = selectedButtonForeColor,
                Font = Globals.boldUiFont,
                Enabled = false
            };

            discardPdfButton = new Button
            {
                Text = "Discard Document",
                Width = widthButton + 30,
                Height = heightButton,
                Location = new Point(390, 10),
                BackColor = selectedButtonBackColor,
                ForeColor = selectedButtonForeColor,
                Font = Globals.boldUiFont,
                Enabled = false
            };

            acceptPdfButton.Click +=
                AcceptPdfButton_Click;

            discardPdfButton.Click +=
                DiscardPdfButton_Click;

            pdfButtonPanel.Controls.Add(acceptPdfButton);
            pdfButtonPanel.Controls.Add(discardPdfButton);

            pdfPreviewPanel.Controls.Add(pdfButtonPanel);





            // STATUS LABEL
            statusLabel =
                new Label
                {
                    Text = "Ready to scan",
                    AutoSize = true,
                    Location = new Point(Column1, Row2),
                    ForeColor = Color.Green,
                    Font = Globals.boldUiFont
                };


            // PAGE LABEL
            pageLabel =
                new Label
                {
                    Text = "Pages scanned: 0",
                    AutoSize = true,
                    Location = new Point(Column1, Row3),
                    Font = Globals.boldUiFont
                };


            // Document Delivery Method email or USB
            docDeliveryGroup = new GroupBox
            {
                Text = "Select e-mail or USB",
                Width = GroupWidth,
                Height = GroupHeight,
                Location = new Point(Column1, Row4),
                Font = Globals.boldUiFont,
                ForeColor = Color.Green,
            };

            emailCheckBox = new CheckBox
            {
                Text = "email",
                AutoSize = true,
                Location = new Point(Column1, CenterOfGroupRow),
                ForeColor = Color.Black
            };

            usbCheckBox = new CheckBox
            {
                Text = "USB",
                AutoSize = true,
                Location = new Point(Column2, CenterOfGroupRow),
                ForeColor = Color.Black
            };

            usbCheckBox.CheckedChanged += (sender, e) =>
            {
                if (!usbCheckBox.Checked)
                {
                    bmpCheckBox.Enabled = false;
                    bmpCheckBox.Checked = false;
                } else
                {
                    bmpCheckBox.Enabled = true;
                }

            };


            docDeliveryGroup.Controls.Add(emailCheckBox);
            docDeliveryGroup.Controls.Add(usbCheckBox);



            // Color MODE Group
            colorModeGroup =
                new GroupBox
                {
                    Text = "Select Scan Mode",
                    Width = GroupWidth,
                    Height = GroupHeight,
                    Location = new Point(Column1, Row5),
                    Font = Globals.boldUiFont,
                    ForeColor = Color.Green
                };

            blackWhiteRadioButton =
                new RadioButton
                {
                    Text = "Black and White",
                    AutoSize = true,
                    Location = new Point(Column1, CenterOfGroupRow),
                    ForeColor = Color.Black
                };
            blackWhiteRadioButton.Click += (s, e) =>
            {
                Globals.scanSelectedScanMode = "BlackAndWhite";
            };

            grayscaleRadioButton =
                new RadioButton
                {
                    Text = "Grayscale",
                    AutoSize = true,
                    Location = new Point(Column2, CenterOfGroupRow),
                    ForeColor = Color.Black
                };
            grayscaleRadioButton.Click += (s, e) =>
            {
                Globals.scanSelectedScanMode = "Grayscale";
            };

            colorRadioButton =
                new RadioButton
                {
                    Text = "Color",
                    AutoSize = true,
                    Location = new Point(Column3, CenterOfGroupRow),
                    ForeColor = Color.Black
                };
            colorRadioButton.Click += (s, e) =>
            {
                Globals.scanSelectedScanMode = "Color";
            };


            colorModeGroup.Controls.Add(colorRadioButton);
            colorModeGroup.Controls.Add(grayscaleRadioButton);
            colorModeGroup.Controls.Add(blackWhiteRadioButton);

            // DPI
            dpiGroup = new GroupBox
            {
                Text = "Select Dots Per Inch (DPI)",
                Width = GroupWidth,
                Height = GroupHeight,
                Location = new Point(Column1, Row6),
                Font = Globals.boldUiFont,
                ForeColor = Color.Green
            };

            standardDpiRadioButton = new RadioButton
            {
                Text = "Standard",
                AutoSize = true,
                Location = new Point(Column1, CenterOfGroupRow),
                ForeColor = Color.Black
            };

            highDpiRadioButton = new RadioButton
            {
                Text = "High",
                AutoSize = true,
                Location = new Point(Column2, CenterOfGroupRow),
                ForeColor = Color.Black
            };

            dpiGroup.Controls.Add(highDpiRadioButton);
            dpiGroup.Controls.Add(standardDpiRadioButton);

            // Document Type Group  (PDF, PDF searchable, BMP)
            docTypeGroup = new GroupBox
            {
                Text = "Select document type",
                Width = GroupWidth,
                Height = GroupHeight,
                Location = new Point(Column1, Row7),
                Font = Globals.boldUiFont,
                ForeColor = Color.Green,
            };


            pdfCheckBox = new CheckBox
            {
                Text = "PDF",
                AutoSize = true,
                Location = new Point(Column1, CenterOfGroupRow),
                ForeColor = Color.Black
            };


            bmpCheckBox = new CheckBox
            {
                Text = "BMP (USB only)",
                AutoSize = true,
                Location = new Point(Column2, CenterOfGroupRow),
                ForeColor = Color.Black
            };


            pdfCheckBox.CheckedChanged += (sender, e) =>
            {
                if (!pdfCheckBox.Checked)
                {
                    pdfCheckBox.Checked = true;
                }
            };
            docTypeGroup.Controls.Add(pdfCheckBox);
            docTypeGroup.Controls.Add(bmpCheckBox);


        // Color MODE Group
        Panel scanButtonPanel = new Panel
        {
                Dock = DockStyle.Bottom,
                Height = GroupHeight,
                BackColor = Color.White
        };

            startScanButton =
                new Button
                {
                    Text = "Start Scan",
                    Width = widthButton,
                    Height = heightButton,
                    Location = new Point(Column1, 10),
                    BackColor = Color.Green,
                    ForeColor = selectedButtonForeColor,
                    Font = Globals.boldUiFont,
                    Enabled = true

                };

            scanMorePagesButton =
                new Button
                {
                    //Text = "SCAN MORE PAGES",
                    Text = "Scan More Pages",
                    Width = widthButton,
                    Height = heightButton,
                    Location = new Point(Column2, 10),
                    BackColor = selectedButtonBackColor,
                    ForeColor = selectedButtonForeColor,
                    Font = Globals.boldUiFont,
                    Enabled = false
                };

            doneScanningButton =
                new Button
                {
                    Text = "Done Scanning",
                    Width = widthButton,
                    Height = heightButton,
                    Location = new Point(Column3, 10),
                    BackColor = selectedButtonBackColor,
                    ForeColor = selectedButtonForeColor,
                    Font = Globals.boldUiFont,
                    Enabled = false
                };


            scanButtonPanel.Controls.Add(startScanButton);
            scanButtonPanel.Controls.Add(scanMorePagesButton);
            scanButtonPanel.Controls.Add(doneScanningButton);

            scanSelectionPanel.Controls.Add(scanButtonPanel);



            startScanButton.Click +=
                StartScanButton_Click;

            scanMorePagesButton.Click +=
                ScanMorePagesButton_Click;

            doneScanningButton.Click +=
                DoneScanningButton_Click;


            // ADD CONTROLS
            scanSelectionPanel.Controls.Add(statusLabel);
            scanSelectionPanel.Controls.Add(pageLabel);

            scanSelectionPanel.Controls.Add(colorModeGroup);
            scanSelectionPanel.Controls.Add(dpiGroup);
            scanSelectionPanel.Controls.Add(docDeliveryGroup);
            scanSelectionPanel.Controls.Add(docTypeGroup);

            SetDefaultScannerOptions();
            LoadDocument(Globals.scanningInstructionsFile);



        }

        // =========================================================
        // Set UI Defaul Scanner Options
        // =========================================================

        private void SetDefaultScannerOptions()
        {
            emailCheckBox.Checked = true;
            usbCheckBox.Checked = false;

            blackWhiteRadioButton.Checked = true; 
            grayscaleRadioButton.Checked = false; 
            colorRadioButton.Checked = false;


            standardDpiRadioButton.Checked = true;
            highDpiRadioButton.Checked = false;

            pdfCheckBox.Checked = true;
            bmpCheckBox.Checked = false;
            bmpCheckBox.Enabled = false;

            
        }

        // =========================================================
        // EnableScanningOptions(true);
        // EnableScanningOptions(false);
        // =========================================================

        private void EnableScanningOptions(bool isEnabled)
        {
            Color enabledColor = Color.Black;
            Color disableldColor = Color.LightGray;

            if (isEnabled)
            {
                docDeliveryGroup.Enabled = true;
                colorModeGroup.Enabled = true;
                dpiGroup.Enabled = true;
                docTypeGroup.Enabled = true;

                emailCheckBox.ForeColor = enabledColor;
                usbCheckBox.ForeColor = enabledColor;
                colorRadioButton.ForeColor = enabledColor;
                grayscaleRadioButton.ForeColor = enabledColor;
                blackWhiteRadioButton.ForeColor = enabledColor;
                standardDpiRadioButton.ForeColor = enabledColor;
                highDpiRadioButton.ForeColor = enabledColor;
                pdfCheckBox.ForeColor = enabledColor;
                bmpCheckBox.ForeColor = enabledColor;
            }
            else
            {
                emailCheckBox.ForeColor = disableldColor;
                usbCheckBox.ForeColor = disableldColor;
                colorRadioButton.ForeColor = disableldColor;
                grayscaleRadioButton.ForeColor = disableldColor;
                blackWhiteRadioButton.ForeColor = disableldColor;
                standardDpiRadioButton.ForeColor = disableldColor;
                highDpiRadioButton.ForeColor = disableldColor;
                pdfCheckBox.ForeColor = disableldColor;
                bmpCheckBox.ForeColor = disableldColor;

                docDeliveryGroup.Enabled = false;
                colorModeGroup.Enabled = false;
                dpiGroup.Enabled = false;
                docTypeGroup.Enabled = false;
            }
        }

        // =========================================================
        // SetButtonEnabled(acceptPdfButton, true);
        // SetButtonEnabled(acceptPdfButton, false);
        // =========================================================

        private void SetButtonEnabled(Button button, bool enabled)
        {
            button.Enabled = enabled;

            if (enabled)
            {
                if (button.Text == "Discard Document")
                {
                    button.BackColor = Color.Red;
                    button.ForeColor = Color.White;
                }
                else
                {
                    button.BackColor = Color.Green;
                    button.ForeColor = Color.White;
                }
            }
            else
            {
                button.BackColor = selectedButtonBackColor;
                button.ForeColor = selectedButtonForeColor;
            }
        }


        // =========================================================
        // Add scanned BMP file
        // =========================================================

        private void AddScannedFile(string file)
        {
            if (string.IsNullOrEmpty(file))
                return;

            if (!File.Exists(file))
                return;

            PictureBox thumbnail = new PictureBox
            {
                Width = thumbnailWidth,
                Height = thumbnailHeight,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand,
                Tag = file
            };

            // Load a copy so the file isn't locked
            using (FileStream fs = new FileStream(
                file,
                FileMode.Open,
                FileAccess.Read))
            using (Image temp = Image.FromStream(fs))
            {
                thumbnail.Image = new Bitmap(temp);
            }

            // Double-click to view
            thumbnail.DoubleClick += Thumbnail_DoubleClick;

            // Right-click to delete
            thumbnail.MouseDown += Thumbnail_MouseDown;

            // Append the new thumbnail
            scanFilesPanel.Controls.Add(thumbnail);

            // Optional: automatically scroll to the newest image
            scanFilesPanel.ScrollControlIntoView(thumbnail);
        }



        // =========================================================
        // Thumbnail_DoubleClick
        // =========================================================

        private void Thumbnail_DoubleClick(object sender, EventArgs e)
        {
            Console.WriteLine("******************  Entered Thumbnail_DoubleClick  **************");

            PictureBox thumbnail = sender as PictureBox;

            if (thumbnail == null || thumbnail.Tag == null)
                return;

            string filePath = thumbnail.Tag.ToString();

            using (ImagePreviewForm preview = new ImagePreviewForm(filePath))
            {
                //Console.WriteLine("filePath.Length" + filePath.Length);
                
                DialogResult result = preview.ShowDialog(this);

                Console.WriteLine("******* ImagePreviewForm ShowDialog result: " + result);

                if (result == DialogResult.OK)
                {

                    if (scanner != null)
                    {
                        scanner.UpdateStatusCounters(filePath);
                        if (Globals.pageNumber <= 0)
                        {
                            pageLabel.Text = $"Pages scanned: {Globals.pageNumber}";
                        } else
                        {
                            pageLabel.Text = $"Pages scanned: {Globals.pageNumber}" + "   [ " + Globals.GetFriendlyImageSize() + " ]";
                        }
                        //Console.WriteLine("******* Thumbnail_DoubleClick  Updated Status Counters\nfilepath " + filePath);
                    }

                    // User chose Delete
                    thumbnail.Image?.Dispose();

                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    thumbnail.Dispose();

                    if (IsFileSizeOverLimit())
                    {
                        MessageBox.Show(
                            "The scanned files exceed the maximum allowed size of 40 MB.\n\n" +
                            "Please delete some files and try again.",
                            "File Size Limit Exceeded",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        return;
                    }

                }
                else
                {
                    Console.WriteLine("******* Thumbnail_DoubleClick Canceled Or Rotated Image" + filePath);
                    // if we havae a rotated image save it to thumbnail
                    // so that it is displayed in the UI.
                    if (preview.RotatedImage != null)
                    {
                        // Replace the thumbnail with an independent copy.
                        Image oldImage = thumbnail.Image;
                        thumbnail.Image = new Bitmap(preview.RotatedImage);
                        oldImage?.Dispose();

                        // Release the preview's returned image.
                        preview.RotatedImage.Dispose();
                    }


                }

            }
        }

        // =========================================================
        // ClearAllThumbnails
        // =========================================================

        private void ClearAllThumbnails()
        {
            foreach (Control control in scanFilesPanel.Controls)
            {
                if (control is PictureBox thumbnail)
                {
                    thumbnail.Image?.Dispose();
                    thumbnail.Image = null;
                }

                control.Dispose();
            }

            scanFilesPanel.Controls.Clear();
        }


        // =========================================================
        // Thumbnail_MouseDown   Right-click to Delete
        // =========================================================

        private void Thumbnail_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            PictureBox thumbnail = (PictureBox)sender;
            string filePath = thumbnail.Tag?.ToString();

            if (string.IsNullOrEmpty(filePath))
                return;

            DialogResult result = MessageBox.Show(
                $"Delete this scanned file?\n\n{Path.GetFileName(filePath)}",
                "Delete Scan",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            // Remove UI first
            scanFilesPanel.Controls.Remove(thumbnail);

            thumbnail.Image?.Dispose();
            thumbnail.Image = null;
            thumbnail.Dispose();

            // Delete file
            try
            {

                // Update your counter
                if (scanner != null)
                {
                    scanner.UpdateStatusCounters(filePath);
                    if (Globals.pageNumber <= 0)
                    {
                        pageLabel.Text = $"Pages scanned: {Globals.pageNumber}";
                    }
                    else
                    {
                        pageLabel.Text = $"Pages scanned: {Globals.pageNumber}" + "   [ " + Globals.GetFriendlyImageSize() +  " ]";
                    }
                }

                if (File.Exists(filePath))
                    File.Delete(filePath);

                if (IsFileSizeOverLimit())
                {
                    MessageBox.Show(
                        "The scanned files exceed the maximum allowed size of 40 MB.\n\n" +
                        "Please delete some files and try again.",
                        "File Size Limit Exceeded",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    return;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not delete the file:\n\n{ex.Message}",
                    "Delete Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

        }


        // =========================================================
        // Clear Pdf Viewer
        // =========================================================

        private void ClearPdfViewer()
        {
            if (pdfViewer != null)
            {
                // Detach the document before disposing it.
                pdfViewer.Document = null;
                pdfViewer.Invalidate();
            }

            pdfDocument?.Dispose();
            pdfDocument = null;

            pdfStream?.Dispose();
            pdfStream = null;
        }

        // =========================================================
        // Exit App only if you enter password
        // =========================================================

        private void ExitSecurityButtonButton_Click(object sender, EventArgs e)
        {


            if (Globals.securityStartTime == null)
            {
                Globals.securityStartTime = DateTime.Now;
                Globals.securityCounter = 0;
            }

            Globals.securityCounter++;

            TimeSpan elapsed = DateTime.Now - Globals.securityStartTime.Value;

            Console.WriteLine("elapsed: " + elapsed + "  securityCounter:  " + Globals.securityCounter);

            if (Globals.securityCounter >= Globals.requiredSecurityClicks
                && elapsed.TotalSeconds <= Globals.requiredSecuritySeconds)
            {
                // Reset so the user can try again later
                Globals.securityCounter = 0;
                Globals.securityStartTime = null;

                // User has clicked button the more then requiredSecurityClicks
                // within requiredSecuritySeconds
                Console.WriteLine("PROMPT user for password.");
                
            }
            else if (elapsed.TotalSeconds > Globals.requiredSecuritySeconds)
            {
                // Time window expired, so start a new attempt
                Globals.securityCounter = 0;
                Globals.securityStartTime = null; //DateTime.Now;

                Console.WriteLine("Time window expired, so start a new attempt.  We do not want to prompt user for password.");
                return;
            }
            else
            {
                // Still within the window, but not enough clicks yet
                Console.WriteLine("Still within the window, but not enough clicks yet. We do not want to prompt user for password.");
                return;
            }



            using (var prompt = new Form())
            {
                prompt.Text = "Password";
                prompt.Width = 300;
                prompt.Height = 150;

                var passwordBox = new TextBox
                {
                    PasswordChar = '*',
                    Left = 20,
                    Top = 20,
                    Width = 240
                };

                var okButton = new Button
                {
                    Text = "OK",
                    Left = 180,
                    Top = 55,
                    Width = 80
                };

                okButton.Click += (s, args) =>
                {
                    if (passwordBox.Text == "1234")
                        Application.Exit();
                    else
                        MessageBox.Show("Incorrect password.");
                };

                prompt.Controls.Add(passwordBox);
                prompt.Controls.Add(okButton);
                prompt.ShowDialog();
            }

            Globals.DisposeFonts();
        }


        // =========================================================
        // DiscardPdfButton_Click
        // =========================================================

        private void DiscardPdfButton_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                DialogResult result = MessageBox.Show(
                   "Are you sure you want to discard this document?\r\n\r\n" +
                   "The scanned document will be permanently deleted. " +
                   "You will need to scan the pages again if you wish to keep them.",
                   "Discard Document",
                   MessageBoxButtons.OKCancel,
                   MessageBoxIcon.Warning);

                if (result != DialogResult.OK)
                {
                    return;
                }

                EnableScanningOptions(true);
                SetDefaultScannerOptions();

                // Clear PDF viewer
                ClearPdfViewer();

                LoadDocument(Globals.scanningInstructionsFile);

                pdfViewer.PerformLayout();
                pdfViewer.Renderer.Page = 0;
                pdfViewer.Renderer.Zoom = 1;
                pdfViewer.ZoomMode = PdfViewerZoomMode.FitBest;
                pdfViewer.PerformLayout();
                pdfViewer.Invalidate();


                // Delete after patron has discarded this document.
                if (File.Exists(Globals.pdfFilePath))
                {
                    File.Delete(Globals.pdfFilePath);
                }

                SetButtonEnabled(acceptPdfButton, false);
                SetButtonEnabled(discardPdfButton, false);

                pageLabel.Text =
                    "Pages scanned: 0";

                pdfPreviewLabel.Text = scannningInstructionsText;

                
                SetButtonEnabled(startScanButton,true);

                MessageBox.Show(
                    "The document was discarded.\r\n\r\n" +
                    "Please scan the document again.",
                    "Scan Again",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }


        // =========================================================
        // AcceptPdfButton_Click
        // =========================================================

        private async void AcceptPdfButton_Click(
            object sender,
            EventArgs e)
        {


            SetButtonEnabled(acceptPdfButton,false);
            SetButtonEnabled(discardPdfButton, false);

            
            statusLabel.Text =
                "Document approved.";

            // Clear PDF viewer
            ClearPdfViewer();

            // load PDF document to PDF document viewer.
            LoadDocument(Globals.scanningInstructionsFile);

            // Continue with Email and/or USB
            await DeliverDocument();

            // reset default options after delivering the documents
            // so that the email/USB options selected are not cleared prior to delivering the docuemnt.
            EnableScanningOptions(true);
            SetDefaultScannerOptions();
            //pdfPreviewLabel.Text = scannningInstructionsText;
            pageLabel.Text = "Pages scanned: 0";


        }


        // -------------------------------------------------
        // Display PDF for user review.
        // -------------------------------------------------

        private void LoadDocument(string filePath)
        {

            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            pdfViewer.ZoomMode = PdfViewerZoomMode.FitBest;

            if (extension == ".pdf")
            {
                // Read the PDF into memory.
                // PdfiumViewer will work with the MemoryStream rather than
                // keeping the physical PDF file open.

                byte[] pdfBytes = File.ReadAllBytes(filePath);

                pdfStream = new MemoryStream(
                    pdfBytes,
                    writable: false);


                try
                {
                    pdfDocument = PdfDocument.Load(pdfStream);
                    pdfViewer.Document = pdfDocument;
                    pdfViewer.ZoomMode = PdfViewerZoomMode.FitBest;
                    pdfViewer.Visible = true;
                }
                catch
                {
                    ClearPdfViewer();
                    throw;
                }

            }
            else if (extension == ".bmp")
            {
                byte[] imageBytes = File.ReadAllBytes(filePath);

                using (MemoryStream stream = new MemoryStream(imageBytes))
                using (Image image = Image.FromStream(stream))
                {

                    Image oldImage = pictureBox1.Image;
                    pictureBox1.Image = new Bitmap(image);
                    oldImage?.Dispose();

                    //pictureBox1.Image?.Dispose();
                    //pictureBox1.Image = new Bitmap(image);
                }

                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Visible = true;
                pdfViewer.Visible = false;
            }



            statusLabel.Text =
                "Ready to scan";

        }

        // -------------------------------------------------
        // Display PDF for user review.
        // -------------------------------------------------

        private bool DisplayScannedDocument(string pdfFilePath)
        {
            PdfDocument newDocument = null;
            MemoryStream newStream = null;

            try
            {
                if (!File.Exists(pdfFilePath))
                {
                    MessageBox.Show(
                        "The scanned PDF could not be found.\r\n\r\n" +
                        pdfFilePath,
                        "PDF Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return false;
                }

                byte[] pdfBytes = File.ReadAllBytes(pdfFilePath);
                newStream = new MemoryStream(pdfBytes, writable: false);
                newDocument = PdfDocument.Load(newStream);

                // Detach the old document before disposing it.
                PdfDocument oldDocument = pdfDocument;
                MemoryStream oldStream = pdfStream;

                pdfViewer.Document = null;

                pdfDocument = newDocument;
                pdfStream = newStream;

                newDocument = null;
                newStream = null;

                pdfViewer.Document = pdfDocument;
                pdfViewer.Visible = true;

                pdfViewer.PerformLayout();
                pdfViewer.ZoomMode = PdfViewerZoomMode.FitBest;
                pdfViewer.Invalidate();

                oldDocument?.Dispose();
                oldStream?.Dispose();

                statusLabel.Text = "Review the scanned document.";

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to display the PDF.\r\n\r\n" + ex.Message,
                    "PDF Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
            finally
            {
                // Dispose resources that were created but not transferred.
                newDocument?.Dispose();
                newStream?.Dispose();
            }
        }

        // =========================================================
        // START SCAN
        // =========================================================

        private void StartScanner()
        {
            try
            {
                statusLabel.Text =
                    "Opening scanner...";


                scanner =
                    new ScannerService();
                Console.WriteLine("******** New Scanner Service ******************** ");
                Console.WriteLine("IsScannerOpen " + scanner.IsScannerOpen);
                Console.WriteLine("PageCount " + Globals.pageNumber);
                Console.WriteLine("PdfFilePath " + Globals.pdfFilePath);
                Console.WriteLine("ScannerName " + scanner.ScannerName);
                Console.WriteLine("**************************** ");


                // Subscribe to ScannerService events.
                scanner.ScanBatchCompleted +=
                    Scanner_ScanBatchCompleted;

                scanner.PageScanned +=
                    Scanner_PageScanned;

                scanner.ScanError +=
                    Scanner_ScanError;


                // Open scanner.
                if (!scanner.OpenScanner(
                    "BookScanner 7145"))
                {
                    statusLabel.Text =
                        "Unable to open scanner.";

                    scanner.Close();

                    scanner = null;

                    return;
                }
                Console.WriteLine("******** Open Scanner Service ******************** ");
                Console.WriteLine("IsScannerOpen " + scanner.IsScannerOpen);
                Console.WriteLine("PageCount " + Globals.pageNumber);
                Console.WriteLine("PdfFilePath " + Globals.pdfFilePath);
                Console.WriteLine("ScannerName " + scanner.ScannerName);
                Console.WriteLine("**************************** ");


                scannerOpen = true;


                // Configure scanner.
                statusLabel.Text =
                    "Configuring scanner...";


                scanner.ConfigureScanArea(
                    0.0,
                    0.0,
                    6.0,
                    8.0);

            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        // =========================================================
        // Get Selected DPI (Standard or High)
        // =========================================================

        private Double GetSelectedDPI()
        {
            if (standardDpiRadioButton.Checked)
                return Globals.StandardScanResolutionDpi;
            else if (highDpiRadioButton.Checked)
                return Globals.HighScanResolutionDpi;
            else
                return Globals.StandardScanResolutionDpi;
        }


        // =========================================================
        // Get Selected Scan Mode
        // =========================================================

        private ScanMode GetSelectedScanMode()
        {
            if (blackWhiteRadioButton.Checked)
                return ScanMode.BlackAndWhite;

            else if (grayscaleRadioButton.Checked)
                return ScanMode.Grayscale;

            else if (colorRadioButton.Checked)
                return ScanMode.Color;
            else
                return ScanMode.BlackAndWhite;
        }


        // =========================================================
        // Get PixelType BW, Gray, RGB
        // =========================================================
        private NTwain.Data.PixelType GetPixelType()
        {
            switch (selectedScanMode)
            {
                case ScanMode.BlackAndWhite:
                    return NTwain.Data.PixelType.BlackWhite;

                case ScanMode.Grayscale:
                    return NTwain.Data.PixelType.Gray;

                case ScanMode.Color:
                    return NTwain.Data.PixelType.RGB;

                default:
                    return NTwain.Data.PixelType.Gray;
            }
        }

        // =========================================================
        // Set Color Mode from Group on click of option
        // =========================================================

        private void SetScannerColorModeAndDPI()
        {


            Console.WriteLine(
                "ENTERING SetScannerColorModeAndDPI");

            if (scanner == null)
            {
                return;
            }

            selectedScanMode = GetSelectedScanMode();

            Console.WriteLine(
                "Selected Scan Mode: " +
                selectedScanMode);

            NTwain.Data.PixelType pixelType = GetPixelType();

            Console.WriteLine(
                "Scan Mode: " +
                selectedScanMode);

            Console.WriteLine(
                "Pixel Type: " +
                pixelType);

            Globals.selectedDPI = GetSelectedDPI();

            Console.WriteLine(
                "DPI selected: " +
                Globals.selectedDPI);

            scanner.Configure(
                Globals.selectedDPI,
                pixelType);

        }

        // =========================================================
        // START SCAN
        // =========================================================

        private void StartScanButton_Click(
            object sender,
            EventArgs e)
        {
            
            Console.WriteLine("ENTERING StartScanButton_Click");


            // disable Scanning Options for this duration of the scanning batch
            // until user discard or Accepts PDF
            EnableScanningOptions(false);
            // Cleanup Any prior Temporary Files
            Globals.CleanupTemporaryFiles();


            // Require at least one delivery method
            if (!emailCheckBox.Checked && !usbCheckBox.Checked)
            {

                MessageBox.Show(
                    "Please select at least one delivery method:\n\n" +
                    "• Email\n" +
                    "• USB",
                    "Delivery Method Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }


            try
            {
                // Create/open/configure a completely new scanner session.
                if (scanner == null)
                {
                    StartScanner();
                    if (scanner == null ||
                        !scanner.IsScannerOpen)
                    {
                        statusLabel.Text =
                            "Unable to open scanner.";

                        return;
                    }
                }

                // set scanner color and DPI in case user has changed it.
                SetScannerColorModeAndDPI();

                // Start first batch.
                StartAcquisition();

            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }


        // =========================================================
        // StartAcquisition
        // =========================================================

        private void StartAcquisition()
        {
            statusLabel.Text =
                "Scanning...";

            SetButtonEnabled(startScanButton,false);
            SetButtonEnabled(scanMorePagesButton,false);
            SetButtonEnabled(doneScanningButton,false);

            Console.WriteLine("******** StartAcquisition ******************** ");
            Console.WriteLine("IsScannerOpen " + scanner.IsScannerOpen);
            Console.WriteLine("PageCount " + Globals.pageNumber);
            Console.WriteLine("PdfFilePath " + Globals.pdfFilePath);
            Console.WriteLine("ScannerName " + scanner.ScannerName);
            Console.WriteLine("**************************** ");
            if (!scanner.StartScan())
            {
                statusLabel.Text =
                    "Unable to start scan.";

                scanMorePagesButton.Enabled =
                    scannerOpen;

                doneScanningButton.Enabled =
                    scannerOpen;

                return;
            }

        }


        // =========================================================
        // PAGE SCANNED
        // =========================================================

        private void Scanner_PageScanned(
            object sender,
            string imagePath)
        {
            if (InvokeRequired)
            {
                BeginInvoke(
                    new Action(() =>
                    {
                        Scanner_PageScanned(
                            sender,
                            imagePath);
                    }));

                return;
            }

            pageLabel.Text = $"Pages scanned: {Globals.pageNumber}" + "   [ " + Globals.GetFriendlyImageSize() + " ]";

            // Add ONLY the newly scanned image
            AddScannedFile(imagePath);

        }

       private bool IsFileSizeOverLimit()
        {
            if (Globals.totalEstImageBytes >= Globals.PDFMaxSize)
            {
                Console.WriteLine($"EnforceFileSizeLimits {Globals.totalEstImageBytes} >= {Globals.PDFMaxSize}");
                SetButtonEnabled(scanMorePagesButton, false);
                SetButtonEnabled(doneScanningButton, false);
                return true;
            } else
            {
                Console.WriteLine($"EnforceFileSizeLimits {Globals.totalEstImageBytes} < {Globals.PDFMaxSize}");
                SetButtonEnabled(scanMorePagesButton, true);
                SetButtonEnabled(doneScanningButton, true);
                return false;
            }

        }
        

        // =========================================================
        // SCANNER BATCH COMPLETE
        // =========================================================

        private void Scanner_ScanBatchCompleted(
            object sender,
            EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(
                    new Action(() =>
                    {
                        UpdateBatchCompletedUI();
                    }));

                return;
            }

            UpdateBatchCompletedUI();

        }

        // =========================================================
        // UpdateBatchCompletedUI
        // =========================================================

        private void UpdateBatchCompletedUI()
        {
            pageLabel.Text = $"Pages scanned: {Globals.pageNumber}" + "   [ " + Globals.GetFriendlyImageSize() + " ]";
            statusLabel.Text =
                "Batch complete. Ready for more pages.";


            SetButtonEnabled(startScanButton,false);
            SetButtonEnabled(scanMorePagesButton,true);
            SetButtonEnabled(doneScanningButton,true);
            
        }


        // =========================================================
        // SCAN MORE PAGES
        // =========================================================

        private void ScanMorePagesButton_Click(
            object sender,
            EventArgs e)
        {
            if (IsFileSizeOverLimit())
            {
                MessageBox.Show(
                    "The scanned files exceed the maximum allowed size of 40 MB.\n\n" +
                    "Please delete some files and try again.",
                    "File Size Limit Exceeded",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            try
            {
                if (scanner == null ||
                    !scanner.IsScannerOpen)
                {
                    statusLabel.Text =
                        "Scanner is not open.";

                    return;
                }

                // set scanner color and DPI in case user has changed it.
                SetScannerColorModeAndDPI();

                // Disable buttons while the next batch starts.
                SetButtonEnabled(scanMorePagesButton,false);
                SetButtonEnabled(doneScanningButton,false);


                // Start next batch.
                StartAcquisition();
            }
            catch (Exception ex)
            {
                ShowError(ex);
                SetButtonEnabled(scanMorePagesButton,true);
                SetButtonEnabled(doneScanningButton,true);
            }
        }


        // =========================================================
        // DONE SCANNING
        // =========================================================
        private async void DoneScanningButton_Click(
            object sender,
            EventArgs e)
        {

            if (IsFileSizeOverLimit())
            {
                MessageBox.Show(
                    "The scanned files exceed the maximum allowed size of 40 MB.\n\n" +
                    "Please delete some files and try again.",
                    "File Size Limit Exceeded",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }


            SetButtonEnabled(startScanButton, false);
            SetButtonEnabled(scanMorePagesButton, false);
            SetButtonEnabled(doneScanningButton, false);

            await CopyBMPFilesToUsbWithProgressBar();
            ClearAllThumbnails();
            await FinishScanning();

            pdfPreviewLabel.Text = scannedDocuemtnPreviewText;
        }

        // =========================================================
        // Copy BMP Files To Usb With Progress Bar
        // =========================================================

        private async Task CopyBMPFilesToUsbWithProgressBar()
        {
            if (!bmpCheckBox.Checked)
            {
                return;
            } 

            using (CopyProgressForm progressForm =
           new CopyProgressForm("Please wait.....\nCoping BMP file to USB.", true))
            {
                progressForm.Show(this);

                bool success = false;

                await Task.Run(() =>
                {
                    success = Globals.CopyBMPFilesToUsb(
                        (current, total, fileName) =>
                        {
                            progressForm.UpdateProgress(
                                current,
                                total,
                                fileName);
                        });

                });

                progressForm.Close();

                // uncheck box, usser
                if (Globals.usbDrive == null)
                {
                    usbCheckBox.Checked = false;
                }

                if (success)
                {
                    MessageBox.Show(
                        ("Files successfully copied to USB drive "),
                        "Copy Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }

        }

        // =========================================================
        // Finish Scanning
        // =========================================================

        private async Task FinishScanning()
        {
            try
            {
                if (scanner == null)
                {
                    return;
                }



                statusLabel.Text =
                    "Finishing scan...";


                // Close TWAIN.
                // unsubscribes the method
                // This happens ONLY after the user presses
                // DONE SCANNING.

                scanner.ScanBatchCompleted -=
                    Scanner_ScanBatchCompleted;

                scanner.PageScanned -=
                    Scanner_PageScanned;

                scanner.ScanError -=
                    Scanner_ScanError;


                // Create ONE PDF containing all batches.
                statusLabel.Text =
                    "Creating PDF...";


                string pdfPath =
                    Globals.pdfFilePath;

                using (CopyProgressForm progressForm = 
                    new CopyProgressForm("Please wait.....\nCreating PDF file.", false))
                {
                    progressForm.Show(this);

                    await Task.Run(() =>
                    {
                        Globals.createdPdf = scanner.CreateFinalPdf();
                    });


                    progressForm.Close();
                }


                    // There were not files scanned 
                    if (string.IsNullOrWhiteSpace(
                    Globals.createdPdf))
                {
                    // Display error
                    statusLabel.Text =
                        "PDF could not be created.";

                    MessageBox.Show(
                        "No PDF was created.",
                        "PDF Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    // Close scanner properly
                    scanner.Close();
                    scanner = null;
                    scannerOpen = false;

                    // Enable the Start Scann button
                    EnableScanningOptions(true);
                    SetButtonEnabled(startScanButton, true);

                    return;
                }
                
                // Close Scanner properly
                scanner.Close();
                scanner = null;
                scannerOpen = false;


                

                // Display PDF for user review.
                if (DisplayScannedDocument(Globals.pdfFilePath))
                {
                    SetButtonEnabled(acceptPdfButton,true);
                    SetButtonEnabled(discardPdfButton, true);

                    SetButtonEnabled(startScanButton, false);
                    SetButtonEnabled(scanMorePagesButton, false);
                    SetButtonEnabled(doneScanningButton, false);

                    statusLabel.Text =
                        "Review document, then select Accept Document or Discard Document.";

                    return;
                }


            }
            catch (Exception ex)
            {
                ShowError(ex);

                SetButtonEnabled(startScanButton,true);
                SetButtonEnabled(scanMorePagesButton,false);
                SetButtonEnabled(doneScanningButton,false);
            }
        }

        // =========================================================
        // DeliverDocument
        // =========================================================
        private async Task DeliverDocument()
        {

            // Email
            if (emailCheckBox.Checked)
            {
                using (EmailForm promptUserAndSend = new EmailForm())
                {
                    try
                    {
                        // Display the email form and wait for the user
                        // to finish or close it.
                        promptUserAndSend.ShowDialog(this);

                    } catch
                    {
                        MessageBox.Show(
                            "The email service is temporarily unavailable.\n\n" +
                            "Please try again later or ask a library staff member for assistance.",
                            "Email Service Unavailable",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                    }
                    
                }

                // Runs after the EmailForm closes.
                EnableScanningOptions(true);
                SetButtonEnabled(startScanButton, true);
            }


            if (usbCheckBox.Checked)
            {
                using (CopyProgressForm progressForm =
                        new CopyProgressForm("Please wait.....\nCoping PDF file to USB.", false))
                {
                    progressForm.Show(this);

                    bool success = false;

                    await Task.Run(() =>
                    {
                        success = Globals.CopyFilesToUsb();
                    });

                    progressForm.Close();


                    if (success)
                    {
                        MessageBox.Show(
                            ("Files successfully copied to  USB drive."),
                            "Copy Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }

                }


                // Only delete after successful delivery
                if (File.Exists(Globals.pdfFilePath))
                {
                    File.Delete(Globals.pdfFilePath);
                }

                EnableScanningOptions(true);
                SetButtonEnabled(startScanButton, true);
                SetButtonEnabled(acceptPdfButton, false);
                SetButtonEnabled(discardPdfButton, false);


            }
        }


        // =========================================================
        // SCANNER ERROR
        // =========================================================

        private void Scanner_ScanError(
            object sender,
            string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(
                    new Action(() =>
                    {
                        Scanner_ScanError(
                            sender,
                            message);
                    }));

                return;
            }


            statusLabel.Text =
                "Scanner error.";


            MessageBox.Show(
                message,
                "Scanner Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            SetButtonEnabled(scanMorePagesButton,scannerOpen);
            SetButtonEnabled(doneScanningButton,scannerOpen);
        }


        // =========================================================
        // ERROR
        // =========================================================

        private void ShowError(
            Exception ex)
        {
            statusLabel.Text =
                "Error.";


            MessageBox.Show(
                ex.ToString(),
                "Scan Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }



        // =========================================================
        // FORM CLOSING
        // =========================================================

        private void ScanForm_FormClosing(
            object sender,
            FormClosingEventArgs e)
        {
            try
            {
                ClearPdfViewer();

                // Release the image displayed in the main PictureBox.
                if (pictureBox1 != null)
                {
                    pictureBox1.Image?.Dispose();
                    pictureBox1.Image = null;
                }

                // Release the exit button image and its file lock.
                if (exitSecurityButtonButton != null)
                {
                    exitSecurityButtonButton.Image?.Dispose();
                    exitSecurityButtonButton.Image = null;
                }

                if (scanner != null)
                {
                    scanner.ScanBatchCompleted -= Scanner_ScanBatchCompleted;
                    scanner.PageScanned -= Scanner_PageScanned;
                    scanner.ScanError -= Scanner_ScanError;

                    scanner.Close();
                    scanner = null;
                }

                scannerOpen = false;

                Globals.CleanupTemporaryFiles();
                Globals.DisposeFonts();
            }
            catch (Exception ex)
            {
                Scanner.Logger.Exception(ex, "ERROR DURING FORM CLOSING CLEANUP: " + ex);
            }
        }
        
    }
}
