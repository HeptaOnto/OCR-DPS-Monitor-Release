using System.Threading;

namespace OCR_DPS_Monitor
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.

            const string appName = "OCR_DPS_Monitor_Unique_Mutex_Name";
            bool createdNew;

            using var mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                // Приложение уже запущено
                MessageBox.Show("Приложение уже запущено!", "OCR DPS Monitor",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainWindow());
        }
    }
}