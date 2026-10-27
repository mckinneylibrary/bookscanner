/**
 * Email Form - User interface for entering email information
 * @Author:  Rosa Hilda Melendez <rmelendez@mckinneytexas.org>
 * @Date: 2016-10-27
 * @Version:  1.0
 */

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

namespace Scanner
{
    public class EmailForm : Form
    {
        private readonly TextBox txtTo = new TextBox();
        private readonly TextBox txtCc = new TextBox();
        private readonly TextBox txtSubject = new TextBox();
        private readonly TextBox txtMessage = new TextBox();

        private readonly Button btnSend = new Button();
        private readonly Button btnClear = new Button();
        private readonly Button btnCancel = new Button();

        private readonly Color txtBackColor = Color.White;
        private readonly Color txtForeColor = Color.Black;


        // The email field currently receiving keyboard input
        private TextBox activeTextBox;
        private bool shift = false;
        private FlowLayoutPanel keyboard;


        public EmailForm()
        {
            Text = "Send Email";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.White;
            Font = Globals.normalUiFont;

            BuildForm();

        }

        private void BuildForm()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                ColumnCount = 2,
                RowCount = 7
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // To
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Cc
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Subject
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); // Message
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Keyboard
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));  // Buttons
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));  // Hint

            Controls.Add(layout);

            txtTo.BackColor = txtBackColor;
            txtTo.ForeColor = txtForeColor;
            AddField(layout, "To:", txtTo, 0);

            txtCc.BackColor = txtBackColor;
            txtCc.ForeColor = txtForeColor;
            AddField(layout, "Cc:", txtCc, 1);

            txtSubject.BackColor = txtBackColor;
            txtSubject.ForeColor = txtForeColor;
            txtSubject.Text = "Your scanned files are attached.";
            AddField(layout, "Subject:", txtSubject, 2);

            txtMessage.Multiline = true;
            txtMessage.ScrollBars = ScrollBars.Vertical;
            txtMessage.AcceptsReturn = true;
            txtMessage.AcceptsTab = true;
            txtMessage.Dock = DockStyle.Fill;
            txtMessage.Font = Globals.normalUiFont;
            txtMessage.Margin = new Padding(5);
            txtMessage.BackColor = txtBackColor;
            txtMessage.ForeColor = txtForeColor;
            txtMessage.Text = "The files you scanned on the Library Document Station are attached to this email.   \r\nDo NOT reply to this email.";

            layout.Controls.Add(new Label
            {
                Text = "Message:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft
            }, 0, 3);

            layout.Controls.Add(txtMessage, 1, 3);

            btnSend.Text = "Send Email";
            btnSend.Dock = DockStyle.Fill;
            btnSend.Margin = new Padding(5, 12, 10, 12);
            btnSend.BackColor = Color.Green;
            btnSend.ForeColor = Color.White;
            btnSend.Font = Globals.boldUiFont;
            btnSend.Click += async (s, e) => await SendEmailAsync();

            btnClear.Text = "Clear";
            btnClear.Dock = DockStyle.Fill;
            btnClear.Margin = new Padding(10, 12, 5, 12);
            btnClear.Font = Globals.boldUiFont;
            btnClear.Click += (s, e) =>
            {
                txtTo.Clear();
                txtCc.Clear();
                txtSubject.Clear();
                txtMessage.Clear();

                activeTextBox = txtTo;
                txtTo.Focus();
            };

            btnCancel.Text = "Cancel";
            btnCancel.Dock = DockStyle.Fill;
            btnCancel.Margin = new Padding(15, 12, 5, 12);
            btnCancel.BackColor = Color.Red;
            btnCancel.ForeColor = Color.White;
            btnCancel.Font = Globals.boldUiFont;

            btnCancel.Click += (s, e) =>
            {
               
                DialogResult result = MessageBox.Show(
                    "Are you sure you want to cancel?\r\n\r\n" +
                    "The scanned document will be permanently deleted. \r\n" +
                    "You will need to scan the pages again if you wish to email them.",
                    "Cancel Email Send",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }

                    
            };

            // Three - button panel
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 10, 0, 10)
            };

            buttonPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 33.33F));
            buttonPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 33.33F));
            buttonPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 33.34F));


            // Consistent button sizing and spacing
            btnSend.Dock = DockStyle.Fill;
            btnSend.Margin = new Padding(5);

            btnClear.Dock = DockStyle.Fill;
            btnClear.Margin = new Padding(5);

            btnCancel.Dock = DockStyle.Fill;
            btnCancel.Margin = new Padding(5);

            buttonPanel.Controls.Add(btnSend, 0, 0);
            buttonPanel.Controls.Add(btnClear, 1, 0);
            buttonPanel.Controls.Add(btnCancel, 2, 0);

            // Add the button panel to the main layout
            layout.Controls.Add(buttonPanel, 0, 5);
            layout.SetColumnSpan(buttonPanel, 2);

            // Instruction below the buttons
            var hint = new Label
            {
                Text = "Tap a field to enter text using the on-screen keyboard.",
                Dock = DockStyle.Bottom,
                Height = 35,
                ForeColor = txtForeColor,
                Font = Globals.normalUiFont,
                TextAlign = ContentAlignment.MiddleLeft
            };

            layout.Controls.Add(hint, 0, 6);
            layout.SetColumnSpan(hint, 2);



            /////  ************************************************
            // Custom touchscreen keyboard

            keyboard = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(5),
                BackColor = Color.White,
                Margin = new Padding(0)
            };

            layout.Controls.Add(keyboard, 0, 4);
            layout.SetColumnSpan(keyboard, 2);

            // Center each keyboard row horizontally
            AddCenteredRow(keyboard, "'1234567890-=");
            AddCenteredRow(keyboard, "QWERTYUIOP[]\\");
            AddCenteredRow(keyboard, "ASDFGHJKL;@");
            AddCenteredRow(keyboard, "ZXCVBNM,./");

            // Bottom function keys
            FlowLayoutPanel bottom = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0),
                Anchor = AnchorStyles.None
            };

            AddKey(bottom, "Shift", 110);
            AddKey(bottom, ".COM", 90);
            AddKey(bottom, "Space", 220);
            AddKey(bottom, "Backspace", 150);
            AddKey(bottom, "Enter", 100);

            // Bottom function keys
            FlowLayoutPanel bottom2 = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0),
                Anchor = AnchorStyles.None
            };

            AddKey(bottom2, "@gmail.com", 200);
            AddKey(bottom2, "@yahoo.com", 200);
            AddKey(bottom2, "@icloud.com", 200);

            // Center the bottom row too
            AddCenteredControl(keyboard, bottom);
            AddCenteredControl(keyboard, bottom2);

            // Initially select the recipient field
            activeTextBox = txtTo;
            txtTo.Enter += (s, e) => activeTextBox = txtTo;
            txtCc.Enter += (s, e) => activeTextBox = txtCc;
            txtSubject.Enter += (s, e) => activeTextBox = txtSubject;
            txtMessage.Enter += (s, e) => activeTextBox = txtMessage;


            btnSend.Click += (s, e) =>
             {
                 btnSend.Enabled = false;
                 btnCancel.Enabled = false;
                 btnClear.Enabled = false;
             };


        }

        private void AddCenteredRow(
    FlowLayoutPanel parent, string letters)
        {
            FlowLayoutPanel row = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 3, 0, 3),
                Anchor = AnchorStyles.None
            };

            foreach (char letter in letters)
            {
                AddKey(row, letter.ToString(), 85);
            }

            AddCenteredControl(parent, row);
        }

        private void AddCenteredControl(
    FlowLayoutPanel parent, Control control)
        {
            Panel wrapper = new Panel
            {
                Width = parent.ClientSize.Width,
                Height = control.Height + 6,
                Margin = new Padding(0),
                BackColor = parent.BackColor
            };

            control.Location = new Point(
                Math.Max(0, (wrapper.Width - control.Width) / 2),
                3);

            wrapper.Controls.Add(control);
            parent.Controls.Add(wrapper);

            // Keep the row centered when the window resizes
            parent.SizeChanged += (s, e) =>
            {
                wrapper.Width = parent.ClientSize.Width;

                control.Left = Math.Max(
                    0, (wrapper.ClientSize.Width - control.Width) / 2);
            };
        }

        private void AddKey(
            FlowLayoutPanel parent, string key, int width)
        {
            Button button = new Button
            {
                Text = key,
                Tag = key,
                Width = width,
                Height = 58,
                Margin = new Padding(4),
                Font = Globals.boldUiFont,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(65, 70, 82),
                ForeColor = Color.White,
                UseVisualStyleBackColor = false,
                TabStop = false
            };

            button.FlatAppearance.BorderSize = 0;
            button.Click += Key_Click;

            parent.Controls.Add(button);
        }

        private void Key_Click(object sender, EventArgs e)
        {

            Button button = sender as Button;

            if (button == null || activeTextBox == null)
                return;

            string key = button.Tag.ToString();
            TextBox target = activeTextBox;

            switch (key)
            {
                case "Shift":
                    shift = !shift;
                    button.BackColor = shift
                        ? Color.DarkOrange
                        : Color.FromArgb(65, 70, 82);
                    return;

                case "Space":
                    target.SelectedText = " ";
                    break;

                case "Backspace":
                    if (target.SelectionLength > 0)
                    {
                        target.SelectedText = "";
                    }
                    else if (target.SelectionStart > 0)
                    {
                        int position = target.SelectionStart;

                        target.Text = target.Text.Remove(
                            position - 1, 1);

                        target.SelectionStart = position - 1;
                    }
                    break;

                case "Enter":
                    if (target.Multiline)
                    {
                        target.SelectedText = Environment.NewLine;
                    }
                    else if (target == txtTo)
                    {
                        txtCc.Focus();
                    }
                    else if (target == txtCc)
                    {
                        txtSubject.Focus();
                    }
                    else if (target == txtSubject)
                    {
                        txtMessage.Focus();
                    }
                    break;

                default:
                    target.SelectedText = shift
                        ? key.ToUpperInvariant()
                        : key.ToLowerInvariant();

                    //shift = false;

                    // Reset Shift button appearance
                    foreach (Control control in keyboard.Controls)
                    {
                        FlowLayoutPanel row =
                            control as FlowLayoutPanel;

                        if (row == null)
                            continue;

                        foreach (Control item in row.Controls)
                        {
                            Button shiftButton = item as Button;

                            if (shiftButton != null &&
                                shiftButton.Tag.ToString() == "Shift")
                            {
                                shiftButton.BackColor =
                                    Color.FromArgb(65, 70, 82);
                            }
                        }
                    }
                    break;
            }
            target.Focus();
        }
        private void AddField(
            TableLayoutPanel layout,
            string caption,
            TextBox box,
            int row)
        {
            layout.Controls.Add(new Label
            {
                Text = caption,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, row);

            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(5, 10, 5, 10);
            box.Font = Globals.normalUiFont;

            layout.Controls.Add(box, 1, row);
        }


        private async Task SendEmailAsync()
        {
            // TODO:  Place this on a Config File so that I can modify it witout having to rebuild the code.
            //string fromAddress = "rosahmelendez@sbcglobal.net";
            string fromAddress = ConfigurationManager.AppSettings["FromAddress"];
            Console.WriteLine($"TODO SendEmailAsync fromAddress {fromAddress}");
            btnSend.Enabled = false;
            btnCancel.Enabled = false;
            btnClear.Enabled = false;

            // Change this to the actual scanned document path.
            //string attachmentPath = @"C:\Scans\document.pdf";
            string attachmentPath = Globals.createdPdf;

            if (string.IsNullOrWhiteSpace(txtTo.Text))
            {
                System.Windows.Forms.MessageBox.Show(
                    "Please enter a recipient email address.");
                txtTo.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtSubject.Text))
            {
                System.Windows.Forms.MessageBox.Show(
                    "Please enter a subject.");
                txtSubject.Focus();
                return;
            }

            btnSend.Enabled = false;

            try
            {
                string messageId =
                    await SesEmailSender.SendScannedDocumentAsync(
                        fromAddress,
                        txtTo.Text.Trim(),
                        txtSubject.Text.Trim(),
                        txtMessage.Text,
                        attachmentPath);

                if (messageId == null)
                {
                    System.Windows.Forms.MessageBox.Show(
                        "FAILED to send email. \n\nPlease try again later.",
                        "Email Error",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(
                        "Email sent successfully!\nMessage ID: " + messageId,
                        "Email Sent",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                }

                txtTo.Clear();
                txtCc.Clear();
                txtSubject.Clear();
                txtMessage.Clear();

                txtTo.Focus();

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show(
                    "The scanned document could not be found.\n\n" +
                    "Please ask a library staff member for assistance.",
                    "Document Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Amazon.Runtime.AmazonServiceException ex)
            {
                Scanner.Logger.Exception(ex, "SES service error: ");

                MessageBox.Show(
                    "The email service is temporarily unavailable.\n\n" +
                    "Please try again later or ask a library staff member for assistance.",
                    "Email Service Unavailable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Scanner.Logger.Exception(ex, "Email sending failed: ");

                MessageBox.Show(
                    "Your email could not be sent.\n\n" +
                    "Please try again later or ask a library staff member for assistance.",
                    "Email Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                btnSend.Enabled = true;
            }
        }

    }
}
