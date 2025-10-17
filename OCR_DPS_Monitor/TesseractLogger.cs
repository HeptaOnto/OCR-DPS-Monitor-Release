using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCR_DPS_Monitor
{
    public class TesseractLogger
    {
        private readonly string _logFilePath;
        private const string LOGS_BASE_DIRECTORY = "Logs";

        public TesseractLogger(string logFilePath = "tesseract_log.txt")
        {
            _logFilePath = logFilePath;
        }

        public void Log(string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";

            // В консоль/отладку
            Debug.WriteLine(logEntry);
            Console.WriteLine(logEntry);

            // В файл
            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка записи в лог: {ex.Message}");
            }
        }

        //public void LogImage(Bitmap image, string prefix)
        //{
        //    try
        //    {
        //        string filename = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
        //        image.Save(filename, ImageFormat.Png);
        //        Log($"Сохранено изображение: {filename}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Log($"Ошибка сохранения изображения: {ex.Message}");
        //    }
        //}


        public void LogImage(Bitmap image, string sessionId, string imageType)
        {
            try
            {
                // Создаем папку с именем sessionId, если она не существует
                string directory = Path.Combine(LOGS_BASE_DIRECTORY, sessionId);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Формируем имя файла
                string filename = $"{imageType}_{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
                string fullPath = Path.Combine(directory, filename);

                // Сохраняем изображение
                image.Save(fullPath, ImageFormat.Png);
                Log($"Сохранено изображение: {fullPath}");
            }
            catch (Exception ex)
            {
                Log($"Ошибка сохранения изображения: {ex.Message}");
            }
        }
    }
}
