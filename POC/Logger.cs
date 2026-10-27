/**
 * logger - logs information to a file and automatically deletes old log files
 * @Author:  Rosa Hilda Melendez <rmelendez@mckinneytexas.org>
 * @Date: 2016-10-27
 * @Version:  1.0
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Configuration;

namespace Scanner
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static bool _initialized;

        private static string _logFilePath;

        public static string LogFilePath
        {
            get { return _logFilePath; }
        }


        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized)
                    return;

                string appName = Assembly.GetEntryAssembly()?.GetName().Name
                                 ?? "MyApplication";

                string logDirectory = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    appName,
                    "Logs");

                Directory.CreateDirectory(logDirectory);

                // Delete log files older than 30 days.
                DeleteOldLogFiles(logDirectory);

                // Set today's log file.
                _logFilePath = Path.Combine(
                    logDirectory,
                    "application_" +
                    DateTime.Now.ToString("yyyy-MM-dd") +
                    ".log");

                _initialized = true;

                Log("==================================================");
                Log("Application started");
                Log("Application: " + appName);
                Log("Log file: " + _logFilePath);
                Log("==================================================");

                // Capture Console.WriteLine and Console.Error.WriteLine.
                Console.SetOut(new LogTextWriter(Console.Out, false));
                Console.SetError(new LogTextWriter(Console.Error, true));

                // Capture Debug.WriteLine and Trace.WriteLine.
                Debug.Listeners.Add(new LogTraceListener());
                Trace.Listeners.Add(new LogTraceListener());
            }
        }

        

        private static void DeleteOldLogFiles(string logDirectory)
        {
            try
            {
                // TODO - use 30 days, currently testing with 5 days
                //DateTime cutoffDate = DateTime.Now.AddDays(-30);
                int cutoffDays = int.Parse(ConfigurationManager.AppSettings["CutoffDays"]);
                DateTime cutoffDate = DateTime.Now.AddDays(-cutoffDays);
                Console.WriteLine($"TODO DeleteOldLogFiles cutoffDate {cutoffDate}");

                foreach (string file in Directory.GetFiles(
                    logDirectory, "application_*.log"))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    // Expected filename: application_yyyy-MM-dd
                    const string prefix = "application_";

                    if (!fileName.StartsWith(
                        prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string datePart = fileName.Substring(prefix.Length);

                    DateTime logDate;

                    if (!DateTime.TryParseExact(
                        datePart,
                        "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out logDate))
                    {
                        continue;
                    }

                    if (logDate.Date < cutoffDate.Date)
                    {
                        try
                        {
                            File.Delete(file);

                            System.Diagnostics.Debug.WriteLine(
                                "Deleted old log file: " + file);
                        }
                        catch (IOException ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "Could not delete log file: " +
                                file + " - " + ex.Message);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "Access denied deleting log file: " +
                                file + " - " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Log cleanup failed: " + ex.Message);
            }
        }
        public static void Log(string message)
        {
            Write("INFO", message);
        }

        public static void Warning(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Exception(Exception ex, string message = "")
        {
            string details = string.IsNullOrWhiteSpace(message)
                ? ex.ToString()
                : message + Environment.NewLine + ex;

            Write("ERROR", details);
        }

        private static void Write(string level, string message)
        {
            lock (_lock)
            {
                try
                {
                    if (!_initialized || string.IsNullOrEmpty(_logFilePath))
                        return;

                    // Determine the correct log file for today.
                    string appName = Assembly.GetEntryAssembly()?.GetName().Name
                                     ?? "MyApplication";

                    string logDirectory = Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData),
                        appName,
                        "Logs");

                    string todayLogFile = Path.Combine(
                        logDirectory,
                        "application_" +
                        DateTime.Now.ToString("yyyy-MM-dd") +
                        ".log");

                    // Switch files when the date changes.
                    _logFilePath = todayLogFile;

                    string entry =
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                        " [" + level + "] " + message;

                    File.AppendAllText(
                        _logFilePath,
                        entry + Environment.NewLine,
                        Encoding.UTF8);
                }
                catch
                {
                    // Do not let logging failures crash the application.
                }
            }
        }
        //private static void Write(string level, string message)
        //{
        //    string entry =
        //        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
        //        " [" + level + "] " + message;

        //    lock (_lock)
        //    {
        //        try
        //        {
        //            if (!_initialized || string.IsNullOrEmpty(_logFilePath))
        //                return;

        //            File.AppendAllText(
        //                _logFilePath,
        //                entry + Environment.NewLine,
        //                Encoding.UTF8);
        //        }
        //        catch
        //        {
        //            // Avoid recursive logging if the log file cannot be written.
        //        }
        //    }
        //}

        private sealed class LogTextWriter : TextWriter
        {
            private readonly TextWriter _original;
            private readonly bool _isError;
            private readonly StringBuilder _buffer = new StringBuilder();

            public LogTextWriter(TextWriter original, bool isError)
            {
                _original = original;
                _isError = isError;
            }

            public override Encoding Encoding
            {
                get { return _original.Encoding; }
            }

            public override void Write(char value)
            {
                _original.Write(value);

                lock (_buffer)
                {
                    if (value == '\r')
                        return;

                    if (value == '\n')
                    {
                        FlushBuffer();
                    }
                    else
                    {
                        _buffer.Append(value);
                    }
                }
            }

            public override void Write(string value)
            {
                if (value == null)
                    return;

                foreach (char c in value)
                    Write(c);
            }

            public override void WriteLine(string value)
            {
                Write(value ?? string.Empty);
                Write('\n');
                _original.Flush();
            }

            public override void Flush()
            {
                _original.Flush();

                lock (_buffer)
                {
                    FlushBuffer();
                }
            }

            private void FlushBuffer()
            {
                if (_buffer.Length == 0)
                    return;

                string message = _buffer.ToString();
                _buffer.Clear();

                if (_isError)
                    Logger.Error(message);
                else
                    Logger.Log(message);
            }
        }

        private sealed class LogTraceListener : TraceListener
        {
            public override void Write(string message)
            {
                // Trace.Write can send partial messages.
                // WriteLine below records completed messages.
                if (!string.IsNullOrEmpty(message))
                    Logger.Log("[Debug/Trace] " + message);
            }

            public override void WriteLine(string message)
            {
                Logger.Log("[Debug/Trace] " + (message ?? string.Empty));
            }
        }
    }
}
