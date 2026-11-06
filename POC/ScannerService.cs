/**
 * Scanner servies, datasource, twain connections
 * @Author:  Rosa Hilda Melendez <rmelendez@mckinneytexas.org>
 * @Date: 2016-10-27
 * @Version:  1.0
 */

using NTwain;
using NTwain.Data;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Drawing;
using System.IO;



namespace Scanner
{
    internal class ScannerService
    {
        private TwainSession twain;
        private DataSource scanner;


        // EVENTS
        public event EventHandler<string> PageScanned;
        public event EventHandler<string> ScanError;
        public event EventHandler ScanCanceled;

        // Fires when ONE FEEDER BATCH is finished.
        public event EventHandler ScanBatchCompleted;

        // PROPERTIES
        public bool IsScannerOpen
        {
            get
            {
                return scanner != null &&
                       scanner.IsOpen;
            }
        }

        public string ScannerName
        {
            get
            {
                return scanner != null
                    ? scanner.Name
                    : null;
            }
        }

        public long GetFileSize(String filePath)
        {
            long bmpFileSize = 0;
            try
            {
                if (File.Exists(filePath))
                {
                    Logger.LogDebug("GetFileSize");
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // Get the BMP file size 
                        bmpFileSize = fs.Length;
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to parse image headers safely: {ex.Message}");
            }
            return bmpFileSize;
        }

        // =========================================================
        // Update Status Counters
        // =========================================================

        public void UpdateStatusCounters(String filePath)
        {
            Logger.LogDebug("*********************   ENTERED UpdateStatusCounters  DELETING FILE    **************************");

            Globals.pageNumber--; // Decrements the internal counter

            // --- EXTRACT IMAGE METADATA HERE ---
            int width = 0;
            int height = 0;

            try
            {

                if (File.Exists(filePath))
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // Get the BMP file size + add 2KB PDF page overhead 
                        //long bmpFileSize = fs.Length + 2048;
                        long bmpFileSize = fs.Length;

                        if (fs.Length >= 30)
                        {
                            Logger.LogDebug("Getting height and width");
                            byte[] headerBuffer = new byte[30];
                            fs.Read(headerBuffer, 0, 30);

                            // Extract Dimensions
                            width = BitConverter.ToInt32(headerBuffer, 18);
                            height = Math.Abs(BitConverter.ToInt32(headerBuffer, 22));

                        }

                        Logger.LogDebug("bmpFileSize BEFORE: " + bmpFileSize);
                        // have size be negative to that it can be subtracted.
                        bmpFileSize = bmpFileSize - (bmpFileSize*2);
                        Logger.LogDebug("bmpFileSize AFTER: " + bmpFileSize);
                        
                        //Globals.GetFriendlyImageSize();
                        UpdateEstimatePDFSize(bmpFileSize, width, height);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to parse image headers safely: {ex.Message}");
            }

            Logger.LogDebug("*********************   EXITED UpdateStatusCounters  DELETING FILE    **************************");

        }



        // =========================================================
        // OPEN SCANNER
        // =========================================================

        public bool OpenScanner(string scannerName)
        {
            try
            {
                // Create a new scan session directory.
                string scansDirectory = Globals.scanFilePath;
                string timestamp =
                    DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

                Directory.CreateDirectory(
                    scansDirectory);
                Logger.LogDebug("Directory.CreateDirectory(scansDirectory)  " + scansDirectory);

                Globals.scanSessionDirectory =
                    Path.Combine(
                        scansDirectory,
                        "Session_" + timestamp);

                Logger.LogDebug("scanSessionDirectory  " + Globals.scanSessionDirectory);
                Directory.CreateDirectory(
                    Globals.scanSessionDirectory);

                Logger.LogDebug("Directory Created scanSessionDirectory  " + Globals.scanSessionDirectory);
                Globals.pdfFilePath =
                    Path.Combine(
                        scansDirectory,
                        "Scan_" +
                        timestamp +
                        ".pdf");

                Logger.LogDebug("Globals.pdfFilePath  " + Globals.pdfFilePath);
                Globals.pageNumber = 0;
                Globals.totalImageBytes = 0;
               


                // Create TWAIN session.
                Logger.LogDebug("About to Create TWAIN session.");
                twain =
                    new TwainSession(
                        TWIdentity.CreateFromAssembly(
                            DataGroups.Image,
                            typeof(ScannerService).Assembly));
                Logger.LogDebug("SUCCESSFULLY Created TWAIN session.");
                twain.Open();
                

                Logger.LogDebug(
                    "TWAIN Data Source Manager opened successfully.");


                // Find scanner.
                foreach (var source in twain)
                {
                    Logger.LogDebug(
                        $"TWAIN source: {source.Name}");

                    if (source.Name.Equals(
                        scannerName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        scanner = source;
                    }
                }


                if (scanner == null)
                {

                    Logger.Error(
                        $"Scanner '{scannerName}' " +
                        "was not found.");
                    CleanupAfterOpenFailure();
                    return false;
                }

                Logger.LogDebug(
                    $"Scanner found: {scanner.Name}");

                Logger.LogDebug(
                    "Opening scanner...");


                var result =
                    scanner.Open();


                if (result != ReturnCode.Success)
                {
                    Logger.Error(
                        "Scanner could not be opened.");
                    CleanupAfterOpenFailure();
                    return false;
                }


                Logger.LogDebug(
                    "Scanner opened successfully.");


                // IMPORTANT:
                // Subscribe ONCE.
                // Do not subscribe again every time StartScan()
                // is called.

                twain.DataTransferred +=
                    Twain_DataTransferred;

                twain.TransferReady +=
                    Twain_TransferReady;

                twain.TransferCanceled +=
                    Twain_TransferCanceled;

                twain.TransferError +=
                    Twain_TransferError;

                twain.SourceDisabled +=
                    Twain_SourceDisabled;

                Logger.LogDebug(
                    "Scanner event handlers installed.");

                return true;
            }
            catch (Exception ex)
            {
                Scanner.Logger.Exception(ex, "ERROR OPENING SCANNER");
                CleanupAfterOpenFailure();
                return false;
            }
        }

        // =========================================================
        // Cleanup After Open Failure
        // =========================================================


        private void CleanupAfterOpenFailure()
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                Scanner.Logger.Exception(ex,"Scanner cleanup failed: ");
            }
        }

        // =========================================================
        // CONFIGURE BASIC SETTINGS
        // =========================================================

        public bool Configure(
            double resolution,
            PixelType pixelType)
        {
            if (!IsScannerOpen)
            {
                Logger.Error(
                    "Scanner is not open.");

                return false;
            }

            bool success = true;

            // X Resolution
            try
            {
                var result =
                    scanner.Capabilities.ICapXResolution
                        .SetValue(resolution);

                Logger.LogDebug(
                    $"X Resolution Result: {result}");

                if (result != ReturnCode.Success)
                {
                    success = false;
                }
            }
            catch (Exception ex)
            {
                success = false;
                Scanner.Logger.Exception(ex,"Could not set X resolution.");
            }


            // Y Resolution
            try
            {
                var result =
                    scanner.Capabilities.ICapYResolution
                        .SetValue(resolution);

                Logger.LogDebug(
                    $"Y Resolution Result: {result}");

                if (result != ReturnCode.Success)
                {
                    success = false;
                }
            }
            catch (Exception ex)
            {
                success = false;

                Scanner.Logger.Exception(ex, "Could not set Y resolution.");

            }


            // Pixel Type
            try
            {
                var result =
                    scanner.Capabilities.ICapPixelType
                        .SetValue(pixelType);

                Logger.LogDebug(
                    $"Pixel Type Result: {result}");

                if (result != ReturnCode.Success)
                {
                    success = false;
                }
            }
            catch (Exception ex)
            {
                success = false;
                Scanner.Logger.Exception(ex,"Could not set pixel type.");

            }


            return success;
        }

        // =========================================================
        // CONFIGURE SCAN AREA
        // =========================================================

        public bool ConfigureScanArea(
            double left,
            double top,
            double right,
            double bottom)
        {
            if (!IsScannerOpen)
            {
                Logger.Error("Scanner is not open.");

                return false;
            }

            try
            {
                var frameCapability =
                    scanner.Capabilities.ICapFrames;

                var frame =
                    new TWFrame();

                frame.Left =
                    (float)left;

                frame.Top =
                    (float)top;

                frame.Right =
                    (float)right;

                frame.Bottom =
                    (float)bottom;


                var result =
                    frameCapability.SetValue(frame);


                Logger.LogDebug(
                    $"Frame Set Result: {result}");


                return result ==
                    ReturnCode.Success;
            }
            catch (Exception ex)
            {
                Scanner.Logger.Exception(ex,"ERROR SETTING SCAN AREA");
                return false;
            }
        }

        // =========================================================
        // START AN ACQUISITION BATCH
        // =========================================================

        public bool StartScan()
        {
            if (!IsScannerOpen)
            {
                Logger.LogDebug(
                    "ERROR: Scanner is not open.");

                return false;
            }

            try
            {
                
                Logger.LogDebug(
                    "========================================");

                Logger.LogDebug(
                    "STARTING SCAN BATCH");

                Logger.LogDebug(
                    "========================================");

                Logger.LogDebug(
                    $"Pages already scanned: {Globals.pageNumber}");


                // DO NOT reset pageNumber here.
                // This allows:
                // Batch 1 -> pages 1-10
                // Batch 2 -> pages 11-20
                // Batch 3 -> pages 21-30
                var result =
                    scanner.Enable(
                        SourceEnableMode.NoUI,
                        false,
                        IntPtr.Zero);


                Logger.LogDebug(
                    $"Scanner Enable Result: {result}");
                Logger.LogDebug(
                   "---------------------");

                if (result != ReturnCode.Success)
                {
                    Logger.Error(
                        "ERROR: Scanner could not be enabled.");

                    return false;
                }


                Logger.LogDebug(
                    "Scanner acquisition started.");

                return true;
            }
            catch (Exception ex)
            {
                Scanner.Logger.Exception(ex, "ERROR STARTING SCAN");
                return false;
            }
        }

        // =========================================================
        // TRANSFER READY
        // =========================================================

        private void Twain_TransferReady(
            object sender,
            EventArgs e)
        {
            Logger.LogDebug(
                "TWAIN: TransferReady");
        }

        // =========================================================
        // PAGE RECEIVED
        // =========================================================

        private void Twain_DataTransferred(
            object sender,
            DataTransferredEventArgs e)
        {
            Logger.LogDebug("****************  ENTERED Twain_DataTransferred *************************");
            try
            {
                Globals.pageNumber++;

                Logger.LogDebug(
                    "========================================");

                Logger.LogDebug(
                    $"PAGE {Globals.pageNumber} RECEIVED");

                Logger.LogDebug(
                    "========================================");


                // Get native image stream.
                var stream =
                    e.GetNativeImageStream();


                if (stream == null)
                {
                    ReportError(
                        $"No image stream returned " +
                        $"for page {Globals.pageNumber}.");

                    return;
                }


                // Temporary BMP filename.
                string imagePath =
                    Path.Combine(
                        Globals.scanSessionDirectory,
                        $"Page_{Globals.pageNumber:D4}.bmp");

                // Save temporary BMP.
                using (stream)
                {
                    using (
                        FileStream fileStream =
                            new FileStream(
                                imagePath,
                                FileMode.Create,
                                FileAccess.ReadWrite))
                    {
                        stream.CopyTo(fileStream);

                        ////  below is what I am working on.............
                        long bmpFileSize = fileStream.Length;
                        int width = 0;
                        int height = 0;

                        // 1. Reset the position back to the beginning of the file to read headers
                        fileStream.Position = 0;

                        if (fileStream.Length >= 26) // Ensure the file is large enough to contain dimensions
                        {
                            byte[] headerBuffer = new byte[30];
                            fileStream.Read(headerBuffer, 0, 30);

                            // 2. Extract Width (Offset 18) and Height (Offset 22)
                            width = BitConverter.ToInt32(headerBuffer, 18);
                            height = BitConverter.ToInt32(headerBuffer, 22);

                            // Note: BMP heights can sometimes be negative (indicates top-down orientation)
                            height = Math.Abs(height);

                            // Extract Bits Per Pixel (Offset 28)
                            ushort bitsPerPixel = BitConverter.ToUInt16(headerBuffer, 28);
                            Logger.LogDebug("Twain_DataTransferred   bitsPerPixel:" + bitsPerPixel);
                        }

                        UpdateEstimatePDFSize( bmpFileSize,  width,  height);
                        
                    }
                }


                Logger.LogDebug(
                    $"Saved temporary image:");

                Logger.LogDebug(
                    imagePath);

                // Notify UI that one page was scanned.
                PageScanned?.Invoke(
                    this,
                    imagePath);
               

            }
            catch (Exception ex)
            {
                ReportError(
                    $"ERROR PROCESSING PAGE " +
                    $"{Globals.pageNumber}\r\n" +
                    ex);
            }

            Logger.LogDebug("****************  EXITED Twain_DataTransferred *************************");

        }

        // =========================================================
        // Estimated File PDF Size as each BMP page is scanned.
        // =========================================================
        private void UpdateEstimatePDFSize(long bmpFileSize, int width, int height)
        {
            Logger.LogDebug("*********************   ENTERED UpdateEstimatePDFSize     **************************");

            string colorMode = Globals.scanSelectedScanMode;
            double factor = 1; // Default to color factor
            
            
            // Set initial Height and With, but will be changed based on Resolution DPI Selected.
            int dpiWidth = width;
            int dpiHeight = height;

            if (Globals.selectedDPI == Globals.StandardTextScanResolutionDpi)
            {
                Logger.LogDebug("STANDARD text resolution DPI");

                dpiWidth = 1900;
                dpiHeight = 2400;

                if (colorMode == "BlackAndWhiteText") 
                {
                    // Letter Size
                    if ((height < dpiHeight && width < dpiWidth) || (height < dpiWidth && width < dpiHeight))
                    {
                        factor = 0.08;
                    }
                    else
                    {
                        // Page larger size factor estimate
                        factor = 0.15;
                    }
                    Logger.LogDebug("Black and white text factor " + factor);
                }
            }
            else if (Globals.selectedDPI == Globals.HighTextScanResolutionDpi)
            {
                Logger.LogDebug("HIGH text resolution DPI");

                dpiWidth = 2800;
                dpiHeight = 3500;

                if (colorMode == "BlackAndWhiteText")
                {
                    // Letter page size factor estimate
                    if ((height < dpiHeight && width < dpiWidth) || (height < dpiWidth && width < dpiHeight))
                    {
                        factor = 0.06;
                    }
                    else
                    {
                        // Larger page size factor estimate
                        factor = 0.14;
                    }
                    Logger.LogDebug("Black and white Text factor " + factor);
                }
            }
            else if (Globals.selectedDPI == Globals.StandardScanResolutionDpi)
            {
                Logger.LogDebug("STANDARD resolution DPI");
                // Low resolution DPI, Height and Width Settings
                dpiWidth = 1400;
                dpiHeight = 1800;

                if (colorMode == "BlackAndWhiteIMG")  // Tested Good
                {
                    // Letter page size factor estimate
                    if ((height < dpiHeight && width < dpiWidth) || (height < dpiWidth && width < dpiHeight))
                    {
                        factor = 0.21;
                    }
                    else
                    {
                        // Larger page size factor estimate
                        factor = 0.25;
                    }
                    Logger.LogDebug("Black and white factor " + factor);
                }
                else if (colorMode == "Grayscale")  // Tested Good
                {
                    // Letter page size factor estimate
                    if ((height < dpiHeight && width < dpiWidth) || (height < dpiWidth && width < dpiHeight))
                    {
                        factor = 0.43;
                    }
                    else
                    {
                        // Larger page size factor estimate
                        factor = 0.63;
                    }
                    Logger.LogDebug("****   Grascale factor  *****" + factor);
                }
                else if (colorMode == "Color")   // Tested Good - testing
                {
                    // Letter page size factor estimate
                    if ((height < dpiHeight && width < dpiWidth) || (height < dpiWidth && width < dpiHeight))
                    {
                        factor = 0.65;
                    }
                    else
                    {
                        // Larger page size factor estimate
                        factor = 0.74;
                    }
                    Logger.LogDebug("****  Color factor *****" + factor);
                }
            }
            else
            {
                Logger.LogDebug("HIGH resolution DPI");
            
                dpiWidth = 1300;
                dpiHeight = 1700;
                
                if (colorMode == "BlackAndWhiteIMG")  // Tested Good
                {
                    // Letter page size factor estimate
                    if ((height < dpiHeight && width < dpiWidth) || (height < dpiWidth && width < dpiHeight))
                    {
                        factor = 0.21;
                    }
                    else
                    {
                        // Larger page size factor estimate
                        factor = 0.25;
                    }
                    Logger.LogDebug("Black and white factor " + factor);
                }
                else if (colorMode == "Grayscale")  // Tested Good
                {
                    // Letter page size factor estimate
                    if ((height < dpiHeight && width < dpiWidth) || (height < dpiWidth && width < dpiHeight))
                    {
                        factor = 0.25;
                    }
                    else
                    {
                        // Larger page size factor estimate
                        factor = 0.52;
                    }
                    Logger.LogDebug("****   Grascale factor  *****" + factor);
                }
                else if (colorMode == "Color")   // Tested Good - testing
                {
                    // Letter page size factor estimate
                    if ((height < dpiHeight && width < dpiWidth) || (height < dpiWidth && width < dpiHeight))
                    {
                        factor = 0.65;
                    }
                    else
                    {
                        // Larger page size factor estimate
                        factor = 0.74;
                    }
                    Logger.LogDebug("****  Color factor *****" + factor);
                }

            }


            Globals.totalImageBytes += bmpFileSize;
            Globals.currentImageSizeKB = bmpFileSize / 1024.0;
            Globals.totalImageKB = Globals.totalImageBytes / 1024.0;
            Globals.totalImageMB = Globals.totalImageBytes / (1024.0 * 1024.0);
            Globals.totalEstImageBytes += (long)(bmpFileSize * factor);

            Logger.LogDebug($"selectedDPI {Globals.selectedDPI} height {height} width {width}");
            Logger.LogDebug($"bmpFileSize {bmpFileSize} currentImageSizeKB {Globals.currentImageSizeKB}");
            Logger.LogDebug($"totalImageBytes {Globals.totalImageBytes}");
            Logger.LogDebug($"totalImageKB {Globals.totalImageKB}");
            Logger.LogDebug($"totalImageMB {Globals.totalImageMB}");
            Logger.LogDebug($"totalEstImageBytes {Globals.totalEstImageBytes}");
            Logger.LogDebug($"totalEstImageBytes {Globals.GetFriendlyImageSize()}");
            Logger.LogDebug($"scanSelectedScanMode {Globals.scanSelectedScanMode}");
            
            Logger.LogDebug("*********************   EXITED UpdateEstimatePDFSize     **************************");
        }
        
        // =========================================================
        // CURRENT BATCH FINISHED
        // =========================================================

        private void Twain_SourceDisabled(
            object sender,
            EventArgs e)
        {
            Logger.LogDebug(
                "========================================");

            Logger.LogDebug(
                "TWAIN: SourceDisabled");

            Logger.LogDebug(
                "CURRENT BATCH COMPLETE");

            Logger.LogDebug(
                "========================================");

            Logger.LogDebug(
                $"Total pages so far: {Globals.pageNumber}");


            
            // IMPORTANT:
            // DO NOT close scanner.
            // DO NOT close TWAIN.
            // DO NOT call Application.ExitThread().
            // The scanner remains available for another batch.

            ScanBatchCompleted?.Invoke(
                this,
                EventArgs.Empty);
        }

        // =========================================================
        // TRANSFER CANCELED
        // =========================================================

        private void Twain_TransferCanceled(
            object sender,
            EventArgs e)
        {
            Logger.LogDebug(
                "TWAIN: TransferCanceled");

            Logger.LogDebug(
                $"Pages received: {Globals.pageNumber}");

            ScanCanceled?.Invoke(
                this,
                EventArgs.Empty);

            // Scanner remains open.
        }

        // =========================================================
        // TRANSFER ERROR
        // =========================================================

        private void Twain_TransferError(
            object sender,
            EventArgs e)
        {
            Logger.LogDebug(
                "TWAIN: TransferError");

            ReportError(
                "Scanner transfer failed.");
        }

        // =========================================================
        // CREATE FINAL PDF
        // =========================================================
        public string CreateFinalPdf()
        {
            Logger.LogDebug("**************   ENTERED CreateFinalPdf *************");

            if (Globals.pageNumber == 0)
            {
                return null;
            }


            if (string.IsNullOrWhiteSpace(
                Globals.scanSessionDirectory))
            {
                return null;
            }


            string finalPdf =
                Globals.pdfFilePath;


            Logger.LogDebug(
                "========================================");

            Logger.LogDebug(
                "CREATING FINAL PDF");

            Logger.LogDebug(
                "========================================");


            try
            {
                PdfDocument document =
                    new PdfDocument();


                for (int i = 1;
                     i <= Globals.pageNumber;
                     i++)
                {
                    string imagePath =
                        Path.Combine(
                            Globals.scanSessionDirectory,
                            $"Page_{i:D4}.bmp");


                    if (!File.Exists(imagePath))
                    {
                        Logger.Warning(
                            $"Missing page " +
                            $"{i}: {imagePath}");

                        continue;
                    }


                    using (Image image =
                        Image.FromFile(imagePath))
                    {

                        double dpiX = image.HorizontalResolution;
                        double dpiY = image.VerticalResolution;

                        Logger.LogDebug($"IMAGE dpiX HorizontalResolution = {dpiX}   dpiY VerticalResolution = {dpiY}");

                        // Use a reasonable fallback for invalid DPI values.
                        if (double.IsNaN(dpiX) ||
                            double.IsInfinity(dpiX) ||
                            dpiX < 50 || dpiX > 1200)
                        {
                            dpiX = 300.0;
                            Logger.LogDebug($"fallback dpiX HorizontalResolution = {dpiX}");
                        }

                        if (double.IsNaN(dpiY) ||
                            double.IsInfinity(dpiY) ||
                            dpiY < 50 || dpiY > 1200)
                        {
                            dpiY = 300.0;
                            Logger.LogDebug($"fallback dpiY VerticalResolution = {dpiY}");
                        }

                        double widthPoints = image.Width / dpiX * 72.0;
                        double heightPoints = image.Height / dpiY * 72.0;
                        
                        Logger.LogDebug($"widthPoints  {widthPoints} =  image.Width {image.Width} / dpix {dpiX} * 72.0");
                        Logger.LogDebug($"heightPoints  {heightPoints} =  image.Width {image.Height} / dpiY {dpiY} * 72.0");

                        PdfPage page = document.AddPage();

                        page.Width = XUnit.FromPoint(widthPoints);
                        page.Height = XUnit.FromPoint(heightPoints);
                        Logger.LogDebug($"page.Width   {page.Width}");
                        Logger.LogDebug($"page.Height  {page.Height}");

                        Logger.LogDebug($"page.Width.Point   {page.Width.Point}");
                        Logger.LogDebug($"page.Height.Point  {page.Height.Point}");

                        using (
                            XGraphics graphics =
                                XGraphics.FromPdfPage(page))
                        using (
                            XImage xImage =
                                XImage.FromFile(imagePath))
                        {
                            graphics.DrawImage(
                                xImage,
                                0,
                                0,
                                page.Width.Point,
                                page.Height.Point);
                            Logger.LogDebug($"xImage = {xImage.Size}");
                        }
                    }


                    Logger.LogDebug(
                        $"Added page {i} to PDF.");
                }


                using (document)
                {
                    // Add pages and images here.
                    document.Save(finalPdf);
                    Logger.LogDebug($"document.FileSize = {document.FileSize}");
                }


                Logger.LogDebug(
                    "PDF CREATED:");

                Logger.LogDebug(
                    finalPdf);


                // Delete temporary BMP files.
                DeleteTemporaryImages();

                //  reseting the page number and the image counters
                Globals.pageNumber = 0;
                Globals.totalImageBytes = 0;
                Globals.totalEstImageBytes = 0;
                Globals.totalImageKB = 0;
                Globals.totalImageMB = 0;
                Logger.LogDebug($"Before Resetting to 0 ---  pageNumber={Globals.pageNumber} totalImageBytes={Globals.totalImageBytes} totalEstImageBytes={Globals.totalEstImageBytes} totalImageKB={Globals.totalImageKB} totalImageMegaBytes {Globals.totalImageMB}");

                Logger.LogDebug("**************   EXITED CreateFinalPdf *************");

                return finalPdf;
            }
            catch (Exception ex)
            {
                Scanner.Logger.Exception(ex, "ERROR CREATING PDF");

                ReportError(
                    "Could not create final PDF.\r\n\r\n" +
                    ex.Message);

                Logger.LogDebug("**************   EXITED CreateFinalPdf *************");

                return null;
            }

        }



        // =========================================================
        // DELETE TEMPORARY IMAGES
        // =========================================================

        private void DeleteTemporaryImages()
        {
            if (string.IsNullOrWhiteSpace(
                Globals.scanSessionDirectory))
            {
                return;
            }


            try
            {
                string[] files =
                    Directory.GetFiles(
                        Globals.scanSessionDirectory,
                        "*.bmp");


                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore individual File.Delete cleanup errors.
                    }
                }


                try
                {
                    Directory.Delete(
                        Globals.scanSessionDirectory,
                        false);
                }
                catch
                {
                    // Ignore Directory.Delete cleanup errors.
                }
            }
            catch
            {
                // Ignore Directory.GetFiles cleanup errors.
            }
        }

        // =========================================================
        // Report ERROR
        // =========================================================

        private void ReportError(
            string message)
        {
            Logger.Error(message);

            ScanError?.Invoke(
                null,
                message);
        }

        // =========================================================
        // CLOSE SCANNER
        // =========================================================

        public void Close()
        {
            Logger.LogDebug(
                "CLOSING SCANNER SERVICE");

            Logger.LogDebug(
                $"Final page count: {Globals.pageNumber}");

            try
            {
                // Unsubscribe TWAIN events first.
                if (twain != null)
                {
                    twain.DataTransferred -=
                        Twain_DataTransferred;

                    twain.TransferReady -=
                        Twain_TransferReady;

                    twain.TransferCanceled -=
                        Twain_TransferCanceled;

                    twain.TransferError -=
                        Twain_TransferError;

                    twain.SourceDisabled -=
                        Twain_SourceDisabled;
                }
            }
            catch (Exception ex)
            {
                Scanner.Logger.Exception(ex, "Error removing TWAIN event handlers:");
            }


            // Close the scanner data source.
            try
            {
                if (scanner != null &&
                    scanner.IsOpen)
                {
                    Logger.LogDebug(
                        "Closing scanner data source...");

                    scanner.Close();

                    Logger.LogDebug(
                        "Scanner data source closed.");
                }
            }
            catch (Exception ex)
            {
                Scanner.Logger.Exception(ex, "Error closing scanner:");
            }


            // Close TWAIN DSM.
            try
            {
                if (twain != null &&
                    twain.IsDsmOpen)
                {
                    Logger.LogDebug(
                        "Closing TWAIN DSM...");

                    twain.Close();

                    Logger.LogDebug(
                        "TWAIN DSM closed.");
                }
            }
            catch (Exception ex)
            {
                Scanner.Logger.Exception(ex, "Error closing TWAIN:");
            }  


            // Release references.
            scanner = null;
            twain = null;

            Logger.LogDebug(
                "SCANNER SERVICE CLOSED.");
        }


    }
}
