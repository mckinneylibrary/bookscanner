/**
 * Main - Start of the program that calls the main ScanForm where user performs most of the scanning activities
 * @Author:  Rosa Hilda Melendez <rmelendez@mckinneytexas.org>
 * @Date: 2016-10-27
 * @Version:  1.0
 */
using System;
using System.Windows.Forms;

namespace Scanner
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Logger.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                using (var form = new ScanForm())
                {
                    Application.Run(form);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Application Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
