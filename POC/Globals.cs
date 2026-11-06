/**
 * Global properties and functions
 * @Author:  Rosa Hilda Melendez <rmelendez@mckinneytexas.org>
 * @Date: 2016-10-27
 * @Version:  1.0
 */
using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;


namespace Scanner
{
    public static class Globals
    {
        // Global constant properties which value DO NOT change
        public const string scanFilePath = @"C:\BookScanner7145\Scans";
        public const string scanningInstructionsFile = @"C:\BookScanner7145\ScanningInstructions.pdf";
        public const string exitAppImageFile = @"C:\BookScanner7145\McKinneyPublicLibrary.png";
        public const int requiredSecurityClicks = 5;
        public const int requiredSecuritySeconds = 1;
        
        public const double StandardScanResolutionDpi = 150.0;
        public const double HighScanResolutionDpi = 200.0;
        public const double StandardTextScanResolutionDpi = 200.0;
        public const double HighTextScanResolutionDpi = 300.0;


        // Global static properties which value DO change
        public static string pdfFilePath;
        public static string scanSessionDirectory;
        public static int securityCounter = 0;
        public static DateTime? securityStartTime = null;
        public static string usbDrive = null;
        public static int pageNumber;
        public static string createdPdf = null;
        public static string scanSelectedScanMode = "BlackAndWhiteText";
        public static double totalImageBytes;
        public static double currentImageSizeKB;
        public static double totalEstImageBytes;
        public static double totalImageKB;
        public static double totalImageMB;
        public static int PDFMaxSize = int.Parse(ConfigurationManager.AppSettings["PDFMaxSize"]);


        public static double selectedDPI = StandardTextScanResolutionDpi;

        public static Font boldUiFont;
        public static Font normalUiFont;
        public static Font toolbarFont;

        public static bool CopyFilesToUsb()
        {

            usbDrive = FindUsbDrive();

            if (usbDrive == null)
            {
                return false;
            }


            if (File.Exists(Globals.pdfFilePath))
            {

                string destinationFile = Path.Combine(
                    usbDrive,
                    Path.GetFileName(Globals.pdfFilePath));

                try
                {
                    File.Copy(Globals.pdfFilePath, destinationFile, true);
                }
                catch (IOException)
                {
                    
                    using (ConfirmForm confirmForm = 
                        new ConfirmForm("There is not enough space on the USB drive.",
                        "Not Enough Disk Space",
                        "",
                        "",
                        DialogResult.OK,
                        MessageBoxIcon.Information))
                    {
                        DialogResult result = confirmForm.ShowDialog();
                    }

                    return false;
                }

            }

            return true;
        }

        public static string GetFriendlyImageSize()
        {
            Logger.LogDebug("**********   Entered GetFriendlyImageSize *****************");

            // 1. Guard check for empty or zero bytes
            if (totalImageBytes <= 0)
            {
                Logger.LogDebug($"Before Resetting to 0 ---  totalImageBytes={totalImageBytes} totalEstImageBytes={totalEstImageBytes} totalImageMegaBytes {totalImageMB}");
                totalImageBytes = 0;
                totalEstImageBytes = 0;
                totalImageKB = 0;
                totalImageMB = 0;
                return "0 KB";
            }

            // 2. Perform the mathematical divisions
            double kb = totalEstImageBytes / 1024.0;
            double mb = kb / 1024.0;

            // 3. Formulate and return the formatted string match
            if (mb >= 1.0)
            {
                // Formats to 2 decimal places (e.g., "3.15 MB")
                return $"{mb:N2} MB";
            }
            else
            {
                // Formats to 1 decimal place (e.g., "120.5 KB")
                return $"{kb:N1} KB";
            }

        }
        public static bool CopyBMPFilesToUsb(
            Action<int, int, string> progress = null)
        {
            usbDrive = FindUsbDrive();

            if (usbDrive == null)
                return false;

            string directoryName = Path.GetFileName(
                scanSessionDirectory.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));

            string destinationDirectory = Path.Combine(
                usbDrive,
                directoryName);

            try
            {
                Directory.CreateDirectory(destinationDirectory);

                string[] files = Directory.GetFiles(
                    scanSessionDirectory,
                    "*.bmp");


                int totalFiles = files.Length;
                int currentFile = 0;

                if (totalFiles == 0)
                    return false;

                foreach (string file in files)
                {

                    string destinationFile = Path.Combine(
                        destinationDirectory,
                        Path.GetFileName(file));

                    try
                    {
                        File.Copy(file, destinationFile, true);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show(
                            "There is not enough space on the USB drive.",
                            "Not Enough Disk Space",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        return false;
                    }

                    currentFile++;

                    // Report progress
                    progress?.Invoke(
                        currentFile,
                        totalFiles,
                        Path.GetFileName(file));
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error copying directory:\n\n{ex.Message}",
                    "Copy Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }



        public static string FindUsbDrive()
        {
            while (true)
            {
                DriveInfo[] usbDrives = DriveInfo.GetDrives()
                    .Where(d =>
                        d.IsReady &&
                        d.DriveType == DriveType.Removable)
                    .ToArray();

                // No USB drives
                if (usbDrives.Length == 0)
                {
                    DialogResult result = MessageBox.Show(
                        "No USB drive was found.\n\n" +
                        "Yes - connect a USB drive and try again\n" +
                        "No - continue without a USB drive",
                        "USB Drive Required",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        // Go back to the beginning of the loop
                        continue;
                    }
                    else
                    {
                        usbDrive = null;
                        // User chose to continue without USB
                        return null;
                    }
                }

                // More than one USB drive
                if (usbDrives.Length > 1)
                {
                    MessageBox.Show(
                        "More than one USB drive is connected.\n\n" +
                        "Please disconnect all USB drives except the one " +
                        "you want to use for the BookScanner.",
                        "Multiple USB Drives",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // Check again after the user disconnects one
                    continue;
                }

                // Exactly one USB drive
                return usbDrives[0].RootDirectory.FullName;
            }
        }

        // =========================================================
        // Dispose Fonts
        // =========================================================

        public static void DisposeFonts()
        {
            boldUiFont?.Dispose();
            normalUiFont?.Dispose();
            toolbarFont?.Dispose();

            boldUiFont = null;
            normalUiFont = null;
            toolbarFont = null;
        }


        // =========================================================
        // Cleanup ALL Temporary Files
        // Application cleans up the specific document after every successful Scan
        // Applcation also cleans up the specific document after rejecting a document
        // This method cleans up ALL document and subdirectories whenever it is called.
        // =========================================================

        public static void CleanupTemporaryFiles()
        {



            Logger.LogDebug(
                "========================================");

            Logger.LogDebug(
                "PUBLIC COMPUTER CLEANUP");

            Logger.LogDebug(
                "========================================");

            Logger.LogDebug(
                "Scan directory:");

            Logger.LogDebug(Globals.scanFilePath);

            if (string.IsNullOrWhiteSpace(
                Globals.scanFilePath))
            {
                Logger.LogDebug(
                    "ERROR: Scan directory is empty.");

                return;
            }

            if (!Directory.Exists(
                Globals.scanFilePath))
            {
                Logger.LogDebug(
                    "Scan directory does not exist.");

                return;
            }


            try
            {
                // PASS 1
                // Delete all files.

                string[] files =
                    Directory.GetFiles(
                        Globals.scanFilePath,
                        "*",
                        SearchOption.AllDirectories);

                Logger.LogDebug(
                    "Files found: " + files.Length);

                foreach (string file in files)
                {

                    // Try more than once because another process may just
                    // have released a file handle.

                    for (int attempt = 1;
                         attempt <= 3;
                         attempt++)
                    {
                        try
                        {
                            File.SetAttributes(
                                file,
                                FileAttributes.Normal);

                            File.Delete(file);

                            Logger.LogDebug(
                                "Deleted: " + file);

                            break;
                        }
                        catch (Exception ex)
                        {

                            Logger.Error(
                                "Delete attempt " +
                                attempt +
                                " failed:");

                            Logger.Error(
                                file);

                            Logger.Error(
                                ex.Message);

                            System.Threading.Thread.Sleep(
                                250);
                        }
                    }

                }

                // PASS 2
                // Delete empty subdirectories.

                string[] directories =
                    Directory.GetDirectories(
                        Globals.scanFilePath,
                        "*",
                        SearchOption.AllDirectories);

                // Deepest directories first.
                Array.Sort(
                    directories,
                    delegate (string x, string y)
                    {
                        return y.Length.CompareTo(
                            x.Length);
                    });

                foreach (string directory in directories)
                {
                    try
                    {
                        Directory.Delete(
                            directory,
                            false);

                        Logger.LogDebug(
                            "Deleted directory: " +
                            directory);
                    }
                    catch (Exception ex)
                    {
                        Scanner.Logger.Exception(ex, "Could not delete directory:");

                        Logger.Error(
                            directory);
                        
                    }
                }

                // VERIFY
                Logger.LogDebug(
                    "========================================");

                Logger.LogDebug(
                    "CLEANUP VERIFICATION");

                Logger.LogDebug(
                    "========================================");

                string[] remainingFiles =
                    Directory.GetFiles(
                        Globals.scanFilePath,
                        "*",
                        SearchOption.AllDirectories);

                string[] remainingDirectories =
                    Directory.GetDirectories(
                        Globals.scanFilePath,
                        "*",
                        SearchOption.AllDirectories);

                Logger.LogDebug(
                    "Remaining files: " +
                    remainingFiles.Length);

                Logger.LogDebug(
                    "Remaining directories: " +
                    remainingDirectories.Length);

                // Report remaining files.
                foreach (string file in remainingFiles)
                {
                    Logger.LogDebug(
                        "STILL EXISTS: " + file);
                }

                foreach (string directory in remainingDirectories)
                {
                    Logger.LogDebug(
                        "DIRECTORY STILL EXISTS: " +
                        directory);
                }

                // RESULT
                if (remainingFiles.Length == 0 &&
                    remainingDirectories.Length == 0)
                {
                    Logger.LogDebug(
                        "SUCCESS:");

                    Logger.LogDebug(
                        "Scan directory is completely empty.");
                }
                else
                {

                    Logger.LogDebug(
                        "WARNING:");

                    Logger.LogDebug(
                        "Some files could not be deleted.");

                    Logger.LogDebug(
                        "These files may be locked by another process.");
                }

                // Make sure the main directory still exists.
                Directory.CreateDirectory(
                    Globals.scanFilePath);
            }
            catch (Exception ex)
            {

                Logger.Error(
                    "ERROR DURING PUBLIC COMPUTER CLEANUP");

                Logger.Error(
                    ex.ToString());
            }
        }


    }
}
