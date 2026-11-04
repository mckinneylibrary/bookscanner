/**
 * Confirm Email Form - displays status e-mail entered by user to confirm prior to sending e-mail.
 * @Author:  Rosa Hilda Melendez <rmelendez@mckinneytexas.org>
 * @Date: 2016-11-02
 * @Version:  1.0
 */

using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace Scanner
{
    public class ConfirmForm : Form
    {
        public bool EmailConfirmed { get; private set; } = false;

        public ConfirmForm(string email)
        {
            string FormNameText = "Confirm Email Address";
            string MessageText = "Please confirm that the recipient email address is correct:";
            string QuestionText = "Do you want to continue?";
            CreateConfirmForm(email, FormNameText, MessageText, QuestionText, DialogResult.Yes, MessageBoxIcon.Question);
        }

        public ConfirmForm(string HighlightedText, string FormNameText, string MessageText, string QuestionText, DialogResult DialogResultRequested, MessageBoxIcon iconRequested)
        {
            CreateConfirmForm(HighlightedText, FormNameText, MessageText, QuestionText, DialogResultRequested, iconRequested);
        }
        private void CreateConfirmForm(string HighlightedText, string FormText, string MessageText, string QuestionText, DialogResult DialogResultRequested, MessageBoxIcon MessageBoxIconRequested)
        {
            // Form properties
            Text = FormText;
            Size = new Size(580, 310);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10);
            DialogResult DialogResultPositive;
            DialogResult DialogResultNegative;
            string btnPositiveText;
            string btnNegativeText;
            Image imageSelected;
            Button btnNegative;

            //  Information, Hand, and Error will only have OK Button
            if (MessageBoxIconRequested == MessageBoxIcon.Information)
            {
                imageSelected = SystemIcons.Information.ToBitmap();
            }  else if (MessageBoxIconRequested == MessageBoxIcon.Hand)
            {
                imageSelected = SystemIcons.Hand.ToBitmap();
            } else if (MessageBoxIconRequested == MessageBoxIcon.Error)
            {
                imageSelected = SystemIcons.Error.ToBitmap();
            }  //  Question, Warning, and Exclamation will have [OK or Yes] and [Cancel or No] Buttons
            else if (MessageBoxIconRequested == MessageBoxIcon.Question)
            {
                imageSelected = SystemIcons.Question.ToBitmap();
            } else if (MessageBoxIconRequested == MessageBoxIcon.Warning)
            {
                imageSelected = SystemIcons.Warning.ToBitmap();
            } else if (MessageBoxIconRequested == MessageBoxIcon.Exclamation)
            {
                imageSelected = SystemIcons.Exclamation.ToBitmap();
            }
            else
            {
                //  Default to Warning with [OK or Yes] and [Cancel or No] Buttons
                imageSelected = SystemIcons.Warning.ToBitmap();
            }

            if (DialogResultRequested == DialogResult.Yes)
            {
                // MessageBoxIcons for Information, Hand, and Error will only have [Yes] Button
                // MessageBoxIcons for Question, Warning, and Exclamation will have [Yes] and [No] Buttons
                DialogResultPositive = DialogResult.Yes;
                DialogResultNegative = DialogResult.No;
                btnPositiveText = "Yes";
                btnNegativeText = "No";

            }  
            else if (DialogResultRequested == DialogResult.OK)
            {
                // MessageBoxIcons for Information, Hand, and Error will only have OK Button
                // MessageBoxIcons for Question, Warning, and Exclamation will have [OK] and [Cancel] Buttons
                DialogResultPositive = DialogResult.OK;
                DialogResultNegative = DialogResult.Cancel;
                btnPositiveText = "OK";
                btnNegativeText = "Cancel";

            }
            else
            {
                // Default buttons will have [Yes] and [No] Buttons
                DialogResultPositive = DialogResult.Yes;
                DialogResultNegative = DialogResult.No;
                btnPositiveText = "Yes";
                btnNegativeText = "No";

            }

            // Main message
            Label lblMessage = new Label
                {
                    Text = MessageText,
                    Location = new Point(50, 25),
                    Size = new Size(500, 45),
                    Font = Globals.normalUiFont,
                    TextAlign = ContentAlignment.MiddleLeft
                };

            // Email address
            Label lblHighlightedText = new Label
            {
                Text = HighlightedText,
                Location = new Point(50, 85),
                Size = new Size(500, 55),
                Font = Globals.boldUiFont,
                ForeColor = Color.FromArgb(0, 105, 190),
                BackColor = Color.FromArgb(240, 247, 255),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Confirmation question
            Label lblQuestion = new Label
            {
                Text = QuestionText,
                Location = new Point(25, 155),
                Size = new Size(520, 30),
                Font = Globals.normalUiFont,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Yes/OK button
            Button btnPositive = new Button
            {
                Text = btnPositiveText,
                Location = new Point(245, 200),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(0, 130, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = Globals.normalUiFont,
                DialogResult = DialogResultPositive
            };

            btnPositive.FlatAppearance.BorderSize = 0;

            // No/Cancel button
            btnNegative = new Button
            {
                Text = btnNegativeText,
                Location = new Point(345, 200),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = Globals.normalUiFont,
                DialogResult = DialogResultNegative
            };

            PictureBox iconRequested = new PictureBox
            {
                Location = new Point(5, 90),
                Size = new Size(40, 40),
                Image = imageSelected,
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            btnNegative.FlatAppearance.BorderSize = 0;

            // Add controls
            Controls.Add(lblMessage);
            Controls.Add(lblHighlightedText);
            Controls.Add(lblQuestion);
            Controls.Add(btnPositive);
            Controls.Add(iconRequested);

            //  Question, Warning, and Exclamation will have [OK or Yes] and [Cancel or No] Buttons
            if (MessageBoxIconRequested == MessageBoxIcon.Question || MessageBoxIconRequested == MessageBoxIcon.Warning || MessageBoxIconRequested == MessageBoxIcon.Exclamation)
            {
                // add No/Cancel button
                Controls.Add(btnNegative);
            }

            // Default buttons
            AcceptButton = btnPositive;
            CancelButton = btnNegative;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            EmailConfirmed = DialogResult == DialogResult.Yes;
            base.OnFormClosing(e);
        }
    }
}
