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
                    Console.WriteLine("GetFileSize");
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        Console.WriteLine("here 4");
                        // Get the BMP file size 
                        bmpFileSize = fs.Length;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse image headers safely: {ex.Message}");
            }
            return bmpFileSize;
        }

        // =========================================================
        // Update Status Counters
        // =========================================================

        public void UpdateStatusCounters(String filePath)
        {
            Console.WriteLine("*********************   ENTERED UpdateStatusCounters  DELETING FILE    **************************");

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
                            Console.WriteLine("Getting height and width");
                            byte[] headerBuffer = new byte[30];
                            fs.Read(headerBuffer, 0, 30);

                            // Extract Dimensions
                            width = BitConverter.ToInt32(headerBuffer, 18);
                            height = Math.Abs(BitConverter.ToInt32(headerBuffer, 22));

                        }

                        Console.WriteLine("bmpFileSize BEFORE: " + bmpFileSize);
                        // have size be negative to that it can be subtracted.
                        bmpFileSize = bmpFileSize - (bmpFileSize*2);
                        Console.WriteLine("bmpFileSize AFTER: " + bmpFileSize);
                        
                        //Globals.GetFriendlyImageSize();
                        UpdateEstimatePDFSize(bmpFileSize, width, height);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse image headers safely: {ex.Message}");
            }

            Console.WriteLine("*********************   EXITED UpdateStatusCounters  DELETING FILE    **************************");

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
                Console.WriteLine("Directory.CreateDirectory(scansDirectory)  " + scansDirectory);

                Globals.scanSessionDirectory =
                    Path.Combine(
                        scansDirectory,
                        "Session_" + timestamp);

                Console.WriteLine("scanSessionDirectory  " + Globals.scanSessionDirectory);
                Directory.CreateDirectory(
                    Globals.scanSessionDirectory);

                Console.WriteLine("Directory Created scanSessionDirectory  " + Globals.scanSessionDirectory);
                Globals.pdfFilePath =
                    Path.Combine(
                        scansDirectory,
                        "Scan_" +
                        timestamp +
                        ".pdf");

                Console.WriteLine("Globals.pdfFilePath  " + Globals.pdfFilePath);
                Globals.pageNumber = 0;
                Globals.totalImageBytes = 0;
               


                // Create TWAIN session.
                Console.WriteLine("About to Create TWAIN session.");
                twain =
                    new TwainSession(
                        TWIdentity.CreateFromAssembly(
                            DataGroups.Image,
                            typeof(ScannerService).Assembly));
                Console.WriteLine("SUCCESSFULLY Created TWAIN session.");
                twain.Open();
                

                Console.WriteLine(
                    "TWAIN Data Source Manager opened successfully.");

                Console.WriteLine();


                // Find scanner.
                foreach (var source in twain)
                {
                    Console.WriteLine(
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

                    Console.WriteLine(
                        $"ERROR: Scanner '{scannerName}' " +
                        "was not found.");
                    CleanupAfterOpenFailure();
                    return false;
                }


                Console.WriteLine();
                Console.WriteLine(
                    $"Scanner found: {scanner.Name}");

                Console.WriteLine(
                    "Opening scanner...");


                var result =
                    scanner.Open();


                if (result != ReturnCode.Success)
                {
                    Console.WriteLine(
                        "ERROR: Scanner could not be opened.");
                    CleanupAfterOpenFailure();
                    return false;
                }


                Console.WriteLine(
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


                Console.WriteLine();

                Console.WriteLine(
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
                Console.WriteLine(
                    "ERROR: Scanner is not open.");

                return false;
            }

            bool success = true;

            // X Resolution
            try
            {
                var result =
                    scanner.Capabilities.ICapXResolution
                        .SetValue(resolution);

                Console.WriteLine(
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

                Console.WriteLine(
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

                Console.WriteLine(
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
                Console.WriteLine(
                    "ERROR: Scanner is not open.");

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


                Console.WriteLine(
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
                Console.WriteLine(
                    "ERROR: Scanner is not open.");

                return false;
            }

            try
            {
                
                Console.WriteLine();
                Console.WriteLine(
                    "========================================");

                Console.WriteLine(
                    "STARTING SCAN BATCH");

                Console.WriteLine(
                    "========================================");

                Console.WriteLine(
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


                Console.WriteLine(
                    $"Scanner Enable Result: {result}");
                Console.WriteLine(
                   "---------------------");

                if (result != ReturnCode.Success)
                {
                    Console.WriteLine(
                        "ERROR: Scanner could not be enabled.");

                    return false;
                }


                Console.WriteLine(
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
            Console.WriteLine();
            Console.WriteLine(
                "TWAIN: TransferReady");
        }

        // =========================================================
        // PAGE RECEIVED
        // =========================================================

        private void Twain_DataTransferred(
            object sender,
            DataTransferredEventArgs e)
        {
            Console.WriteLine("****************  ENTERED Twain_DataTransferred *************************");
            try
            {
                Globals.pageNumber++;


                Console.WriteLine();
                Console.WriteLine(
                    "========================================");

                Console.WriteLine(
                    $"PAGE {Globals.pageNumber} RECEIVED");

                Console.WriteLine(
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
                            Console.WriteLine("Twain_DataTransferred   bitsPerPixel:" + bitsPerPixel);
                        }

                        UpdateEstimatePDFSize( bmpFileSize,  width,  height);
                        
                    }
                }


                Console.WriteLine(
                    $"Saved temporary image:");

                Console.WriteLine(
                    imagePath);

                // Notify UI that one page was scanned.
                PageScanned?.Invoke(
                    this,
                    imagePath);
                
                //Console.WriteLine("Estimated file size: " + Globals.GetFriendlyImageSize());



            }
            catch (Exception ex)
            {
                ReportError(
                    $"ERROR PROCESSING PAGE " +
                    $"{Globals.pageNumber}\r\n" +
                    ex);
            }

            Console.WriteLine("****************  EXITED Twain_DataTransferred *************************");

        }

        // =========================================================
        // Estimated File PDF Size as each BMP page is scanned.
        // =========================================================
        private void UpdateEstimatePDFSize(long bmpFileSize, int width, int height)
        {
            Console.WriteLine("*********************   ENTERED UpdateEstimatePDFSize     **************************");

            string colorMode = Globals.scanSelectedScanMode;
            double factor = 1; // Default to color factor
            
            
            // Set initial Height and With, but will be changed based on Resolution DPI Selected.
            int dpiWidth = width;
            int dpiHeight = height;

            if (Globals.selectedDPI == Globals.StandardTextScanResolutionDpi)
            {
                Console.WriteLine("STANDARD text resolution DPI");

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
                    Console.WriteLine("Black and white text factor " + factor);
                }
            }
            else if (Globals.selectedDPI == Globals.HighTextScanResolutionDpi)
            {
                Console.WriteLine("HIGH text resolution DPI");

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
                    Console.WriteLine("Black and white Text factor " + factor);
                }
            }
            else if (Globals.selectedDPI == Globals.StandardScanResolutionDpi)
            {
                Console.WriteLine("STANDARD resolution DPI");
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
                    Console.WriteLine("Black and white factor " + factor);
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
                    Console.WriteLine("****   Grascale factor  *****" + factor);
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
                    Console.WriteLine("****  Color factor *****" + factor);
                }
            }
            else
            {
                Console.WriteLine("HIGH resolution DPI");
            
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
                    Console.WriteLine("Black and white factor " + factor);
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
                    Console.WriteLine("****   Grascale factor  *****" + factor);
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
                    Console.WriteLine("****  Color factor *****" + factor);
                }

            }


            Globals.totalImageBytes += bmpFileSize;
            Globals.currentImageSizeKB = bmpFileSize / 1024.0;
            Globals.totalImageKB = Globals.totalImageBytes / 1024.0;
            Globals.totalImageMB = Globals.totalImageBytes / (1024.0 * 1024.0);
            Globals.totalEstImageBytes += (long)(bmpFileSize * factor);

            Console.WriteLine($"selectedDPI {Globals.selectedDPI} height {height} width {width}");
            Console.WriteLine($"bmpFileSize {bmpFileSize} currentImageSizeKB {Globals.currentImageSizeKB}");
            Console.WriteLine($"totalImageBytes {Globals.totalImageBytes}");
            Console.WriteLine($"totalImageKB {Globals.totalImageKB}");
            Console.WriteLine($"totalImageMB {Globals.totalImageMB}");
            Console.WriteLine($"totalEstImageBytes {Globals.totalEstImageBytes}");
            Console.WriteLine($"totalEstImageBytes {Globals.GetFriendlyImageSize()}");
            Console.WriteLine($"scanSelectedScanMode {Globals.scanSelectedScanMode}");
            
            Console.WriteLine("*********************   EXITED UpdateEstimatePDFSize     **************************");
        }
        
        // =========================================================
        // CURRENT BATCH FINISHED
        // =========================================================

        private void Twain_SourceDisabled(
            object sender,
            EventArgs e)
        {
            Console.WriteLine();
            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "TWAIN: SourceDisabled");

            Console.WriteLine(
                "CURRENT BATCH COMPLETE");

            Console.WriteLine(
                "========================================");

            Console.WriteLine();

            Console.WriteLine(
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
            Console.WriteLine();
            Console.WriteLine(
                "TWAIN: TransferCanceled");

            Console.WriteLine(
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
            Console.WriteLine();
            Console.WriteLine(
                "TWAIN: TransferError");

            ReportError(
                "Scanner transfer failed.");
        }

        // =========================================================
        // CREATE FINAL PDF
        // =========================================================
        public string CreateFinalPdf()
        {
            Console.WriteLine("**************   ENTERED CreateFinalPdf *************");

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


            Console.WriteLine();
            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "CREATING FINAL PDF");

            Console.WriteLine(
                "========================================");

            Console.WriteLine();


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
                        Console.WriteLine(
                            $"WARNING: Missing page " +
                            $"{i}: {imagePath}");

                        continue;
                    }


                    using (Image image =
                        Image.FromFile(imagePath))
                    {

                        double dpiX = image.HorizontalResolution;
                        double dpiY = image.VerticalResolution;

                        Console.WriteLine($"IMAGE dpiX HorizontalResolution = {dpiX}   dpiY VerticalResolution = {dpiY}");

                        // Use a reasonable fallback for invalid DPI values.
                        if (double.IsNaN(dpiX) ||
                            double.IsInfinity(dpiX) ||
                            dpiX < 50 || dpiX > 1200)
                        {
                            dpiX = 300.0;
                            Console.WriteLine($"fallback dpiX HorizontalResolution = {dpiX}");
                        }

                        if (double.IsNaN(dpiY) ||
                            double.IsInfinity(dpiY) ||
                            dpiY < 50 || dpiY > 1200)
                        {
                            dpiY = 300.0;
                            Console.WriteLine($"fallback dpiY VerticalResolution = {dpiY}");
                        }

                        double widthPoints = image.Width / dpiX * 72.0;
                        double heightPoints = image.Height / dpiY * 72.0;
                        
                        Console.WriteLine($"widthPoints  {widthPoints} =  image.Width {image.Width} / dpix {dpiX} * 72.0");
                        Console.WriteLine($"heightPoints  {heightPoints} =  image.Width {image.Height} / dpiY {dpiY} * 72.0");

                        PdfPage page = document.AddPage();

                        page.Width = XUnit.FromPoint(widthPoints);
                        page.Height = XUnit.FromPoint(heightPoints);
                        Console.WriteLine($"page.Width   {page.Width}");
                        Console.WriteLine($"page.Height  {page.Height}");

                        Console.WriteLine($"page.Width.Point   {page.Width.Point}");
                        Console.WriteLine($"page.Height.Point  {page.Height.Point}");

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
                            Console.WriteLine($"xImage = {xImage.Size}");
                            //Console.WriteLine($"page.Size = {page.Size}");
                        }
                    }


                    Console.WriteLine(
                        $"Added page {i} to PDF.");
                }


                using (document)
                {
                    // Add pages and images here.
                    document.Save(finalPdf);
                    Console.WriteLine($"document.FileSize = {document.FileSize}");
                }


                Console.WriteLine();

                Console.WriteLine(
                    "PDF CREATED:");

                Console.WriteLine(
                    finalPdf);


                // Delete temporary BMP files.
                DeleteTemporaryImages();

                //  reseting the page number and the image counters
                Globals.pageNumber = 0;
                Globals.totalImageBytes = 0;
                Globals.totalEstImageBytes = 0;
                Globals.totalImageKB = 0;
                Globals.totalImageMB = 0;
                Console.WriteLine($"Before Resetting to 0 ---  pageNumber={Globals.pageNumber} totalImageBytes={Globals.totalImageBytes} totalEstImageBytes={Globals.totalEstImageBytes} totalImageKB={Globals.totalImageKB} totalImageMegaBytes {Globals.totalImageMB}");

                Console.WriteLine("**************   EXITED CreateFinalPdf *************");

                return finalPdf;
            }
            catch (Exception ex)
            {
                Scanner.Logger.Exception(ex, "ERROR CREATING PDF");

                ReportError(
                    "Could not create final PDF.\r\n\r\n" +
                    ex.Message);

                Console.WriteLine("**************   EXITED CreateFinalPdf *************");

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
            Console.WriteLine();
            Console.WriteLine(message);

            ScanError?.Invoke(
                null,
                message);
        }

        // =========================================================
        // CLOSE SCANNER
        // =========================================================

        public void Close()
        {
            Console.WriteLine();
            Console.WriteLine(
                "CLOSING SCANNER SERVICE");

            Console.WriteLine(
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
                    Console.WriteLine(
                        "Closing scanner data source...");

                    scanner.Close();

                    Console.WriteLine(
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
                    Console.WriteLine(
                        "Closing TWAIN DSM...");

                    twain.Close();

                    Console.WriteLine(
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

            Console.WriteLine(
                "SCANNER SERVICE CLOSED.");
        }


    }
}
