/**
 * Copy Progress Form - displays status of copy to USB progress and other progress messages.
 * @Author:  Rosa Hilda Melendez <rmelendez@mckinneytexas.org>
 * @Date: 2016-10-27
 * @Version:  1.0
 */

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Scanner
{
    public class CopyProgressForm : Form
    {
        private Label lblStatus;
        private Label lblFile;
        private ProgressBar progressBar;


        public CopyProgressForm(
        string message,
        bool showProgress)
        {
            // Form settings
            Text = "Processing Scanned Files";
            Width = 550;
            Height = showProgress ? 200 : 180;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;

            if (showProgress)
            {

                // Status label
                lblStatus = new Label
                {
                    Left = 20,
                    Top = 5,
                    Width = 440,
                    Height = 80,
                    Text = message,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = Globals.normalUiFont
                };

                // Progress bar
                progressBar = new ProgressBar
                {
                    Left = 20,
                    Top = 90,
                    Width = 440,
                    Height = 25,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0
                };

                // Current file label
                lblFile = new Label
                {
                    Left = 20,
                    Top = 110,
                    Width = 440,
                    Height = 30,
                    Text = "Preparing...",
                    Font = Globals.normalUiFont
                };

                Controls.Add(lblStatus);
                Controls.Add(progressBar);
                Controls.Add(lblFile);

            }
            else
            {
                // Status label
                lblStatus = new Label
                {
                    Left = 20,
                    Top = 25,
                    Width = 440,
                    Height = 80,
                    Text = message,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = Globals.normalUiFont
                };

                Controls.Add(lblStatus);

            }

        }


        public void UpdateProgress(
            int current,
            int total,
            string fileName)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    UpdateProgress(
                        current,
                        total,
                        fileName);
                }));

                return;
            }

            if (total <= 0)
                return;

            int percent = 0;
            percent = (int)(
                ((double)current / total) * 100);

            progressBar.Value = Math.Max(0, Math.Min(percent, 100));

            lblStatus.Text =
                $"Copying files to USB... {percent}%";

            lblFile.Text =
                $"Copying: {fileName}";
        }

    }

}