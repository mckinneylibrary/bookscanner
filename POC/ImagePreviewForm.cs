/**
 * Image Preview Form - Displays the BMP scanned pages to user
 * @Author:  Rosa Hilda Melendez <rmelendez@mckinneytexas.org>
 * @Date: 2016-10-27
 * @Version:  1.0
 */

using Amazon.Auth.AccessControlPolicy;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

public partial class ImagePreviewForm : Form
{
    private readonly string filePath;

    public Bitmap RotatedImage { get; private set; }

    private readonly PictureBox pictureBox;
    private readonly Button deleteButton;
    private readonly Button cancelButton;
    private readonly Button rotateButton;
    private readonly Button saveRotationButton;

    // =========================================================
    // Image Preview Form to view BMP files with option to rotate
    // =========================================================

    public ImagePreviewForm(string filePath)
    {
        this.filePath = filePath;


        Text = "Review Scanned Document";
        Width = 900;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        // Image viewer
        pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };

        
        // Bottom panel
        Panel buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60
        };

        // Delete button
        deleteButton = new Button
        {
            Text = "Delete",
            Width = 100,
            Height = 35,
            Left = 20,
            Top = 12
        };
        deleteButton.Click += DeleteButton_Click;
        buttonPanel.Controls.Add(deleteButton);

        // Cancel button
        cancelButton = new Button
        {
            Text = "Cancel",
            Width = 100,
            Height = 35,
            Left = 130,
            Top = 12
        };
        cancelButton.Click += CancelButton_Click;
        buttonPanel.Controls.Add(cancelButton);

        // Rotate button
        rotateButton = new Button
        {
            Text = "Rotate 90°",
            Width = 110,
            Height = 35,
            Left = 240,
            Top = 12
        };
        rotateButton.Click += RotateButton_Click;
        buttonPanel.Controls.Add(rotateButton);

        // Save Rotation button
        saveRotationButton = new Button
        {
            Text = "Save Rotation",
            Width = 120,
            Height = 35,
            Left = 360,
            Top = 12,
            Enabled = true
        };
        saveRotationButton.Click += (s, e) => SaveRotatedImage();
        buttonPanel.Controls.Add(saveRotationButton);





        Controls.Add(pictureBox);
        Controls.Add(buttonPanel);

        LoadImage();
    }
    // prior version code -  see if it works.
    private void SaveRotatedImage()
    {
        if (pictureBox.Image == null)
            return;

        string extension = Path.GetExtension(this.filePath)
            .ToLowerInvariant();

        // Save to a temporary file first.
        string tempPath = filePath + ".tmp";

        try
        {
            System.Drawing.Imaging.ImageFormat format;

            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    break;

                case ".png":
                    format = System.Drawing.Imaging.ImageFormat.Png;
                    break;

                case ".bmp":
                    format = System.Drawing.Imaging.ImageFormat.Bmp;
                    break;

                default:
                    throw new NotSupportedException(
                        "Unsupported image format.");
            }

            pictureBox.Image.Save(tempPath, format);

            // Replace the original image with the rotated version.
            File.Copy(tempPath, filePath, true);
            File.Delete(tempPath);

            // Store a copy of the rotated image for the calling form.
            RotatedImage?.Dispose();
            RotatedImage = new Bitmap(pictureBox.Image);

            MessageBox.Show(
                "The rotated image has been saved.",
                "Image Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            //  return Cancel, do not delete the rotated image
            //  and close the window.
            DialogResult = DialogResult.Cancel;
            Close();
        }
        catch (Exception ex)
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            MessageBox.Show(
                "Unable to save the rotated image.\r\n\r\n" +
                ex.Message,
                "Save Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }



    // =========================================================
    // Load Image from scanned file on disk
    // =========================================================
    private void LoadImage()
    {
        Console.WriteLine("******************* Entered LoadImage  ****************");

        Bitmap loadedBitmap;

        // Load directly from BMP
        using (var source = new Bitmap(filePath))
        {

            loadedBitmap = source.Clone(
                new Rectangle(0, 0, source.Width, source.Height),
                source.PixelFormat);

            loadedBitmap.SetResolution(
                source.HorizontalResolution,
                source.VerticalResolution);

            Console.WriteLine(
                $"Loaded PixelFormat={loadedBitmap.PixelFormat}, " +
                $"Dimensions={loadedBitmap.Width}x{loadedBitmap.Height}");

        }

        pictureBox.Image?.Dispose();
        pictureBox.Image = loadedBitmap;
    }



    // =========================================================
    // DeleteButton_Click
    // =========================================================

    private void DeleteButton_Click(object sender, EventArgs e)
    {
        DialogResult confirm = MessageBox.Show(
            "Are you sure you want to delete this document?",
            "Delete Document",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm == DialogResult.Yes)
        {
            pictureBox.Image?.Dispose();
            pictureBox.Image = null;

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    // =========================================================
    // CancelButton_Click
    // =========================================================

    private void CancelButton_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }


    // =========================================================
    // RotateButton_Click
    // =========================================================
    private void RotateButton_Click(object sender, EventArgs e)
    {
        Console.WriteLine("****************** Enter RotateButton_Click  ***************************");

        if (pictureBox.Image == null)
            return;

        pictureBox.Image.RotateFlip(
            RotateFlipType.Rotate90FlipNone);

        pictureBox.Refresh();

    }
    // =========================================================
    // OnFormClosed
    // =========================================================
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        pictureBox.Image?.Dispose();
        pictureBox.Image = null;

        base.OnFormClosed(e);
    }
}
