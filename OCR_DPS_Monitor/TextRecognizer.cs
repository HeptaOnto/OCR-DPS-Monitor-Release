using OCR_DPS_Monitor;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using Tesseract;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.AxHost;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using Size = System.Drawing.Size;

public class TextRecognizer
{
    private readonly TesseractEngine _engine;
    private readonly TesseractLogger _logger;

    private bool _disposed = false;


    public TextRecognizer()
    {
        //_logger = new TesseractLogger();

        try
        {
            //_logger.Log("Инициализация Tesseract Engine...");

            //// Инициализация движка ОДИН РАЗ при создании
            _engine = new TesseractEngine(@"./tessdata", "eng+rus", EngineMode.LstmOnly);

            //var settings = new Dictionary<string, string>
            //{
            //    ["user_defined_dpi"] = "96",
            //    ["tessedit_char_whitelist"] = "0123456789.",
            //    ["tessedit_pageseg_mode"] = "7",
            //    ["tessedit_ocr_engine_mode"] = "0",
            //    ["textord_min_linesize"] = "2.5",
            //    ["textord_heavy_nr"] = "0",
            //    ["textord_noise_sizelimit"] = "0.5",
            //    ["classify_min_norm_scale"] = "0.1",
            //    ["classify_max_rating_ratio"] = "10.0",
            //    ["classify_min_proto_size"] = "1",
            //    ["textord_min_blob_size"] = "1"
            //};

            //foreach (var setting in settings)
            //{
            //    _engine.SetVariable(setting.Key, setting.Value);
            //    _logger.Log($"Установлена настройка: {setting.Key} = {setting.Value}");
            //}

            //_logger.Log("Tesseract Engine успешно инициализирован");

            //_engine.SetVariable("user_defined_dpi", "96");
            _engine.SetVariable("tessedit_char_whitelist", "0123456789.млнрд");
            _engine.SetVariable("tessedit_pageseg_mode", "7");

            _engine.SetVariable("tessedit_adaptive_threshold", "0");
            //_engine.SetVariable("classify_bln_numeric_mode", "1");

            _engine.SetVariable("tessedit_ocr_engine_mode", "1"); // 0 Legacy engine
            //_engine.SetVariable("textord_min_linesize", "2.5"); // Минимальный размер линии 2.5
            //_engine.SetVariable("textord_heavy_nr", "0"); // Отключаем агрессивный noise reduction
            //_engine.SetVariable("textord_noise_sizelimit", "0.5"); // Более чувствительный к мелким деталям 0.5


            //_engine.SetVariable("classify_min_norm_scale", "0.5");    // Более чувствительный к мелким деталям 0.1
            //_engine.SetVariable("classify_max_rating_ratio", "30.0"); // Более терпим к вариациям 10.0
            ////_engine.SetVariable("classify_min_proto_size", "1");      // Минимальный размер прототипа
            _engine.SetVariable("textord_min_blob_size", "3");        // Минимальный размер blob 1
        }
        catch (Exception ex)
        {
            //_logger.Log($"ОШИБКА инициализации Tesseract: {ex.Message}");
            throw;
        }
    }

    public void TestScenario()
    {
        //Дополнительные настройки для минимальной обработки
        //_engine.SetVariable("thresholding_method", "0"); // Без пороговой обработки
        //_engine.SetVariable("textord_noise_rej", "0");   // Отключить удаление шума
        //_engine.SetVariable("textord_smooth_offset", "0"); // Отключить сглаживание
        //_engine.SetVariable("edges_children_fix", "0");  // Отключить исправление границ
        //_engine.SetVariable("edges_childarea", "0");     // Отключить обработку дочерних областей
        //_engine.SetVariable("edges_boxarea", "0");       // Отключить обработку областей

        string folderPath = @"d:\Boosty\Versions\OCR_DPS_Monitor\Session 5 Original\Result\";
        string outputFile = Path.Combine(folderPath, "recognition_results.txt");

        try
        {
            using (StreamWriter writer = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                writer.WriteLine($"Результаты распознавания - {DateTime.Now}");
                writer.WriteLine(new string('-', 50));

                // Получаем все PNG файлы в папке
                var imageFiles = Directory.GetFiles(folderPath, "Original_*.png")
                                         .OrderBy(f => f)
                                         .ToList();

                foreach (string imagePath in imageFiles)
                {
                    try
                    {
                        string fileName = Path.GetFileName(imagePath);

                        // Загружаем изображение
                        using (Bitmap image = new Bitmap(imagePath))
                        {
                            var processedImage = PreprocessNumberForTesseract(image); 
                            //var processedImage = image;

                            using (var pix = ConvertBitmapToPix(processedImage))
                            using (var page = _engine.Process(pix))
                            {
                                string text = page.GetText().Trim();
                                float confidence = page.GetMeanConfidence();

                                // Записываем результат в файл
                                string resultLine = $"{fileName} -> '{text}' (доверие: {confidence:F2})";
                                writer.WriteLine(resultLine);

                                // Также выводим в консоль для отслеживания прогресса
                                Debug.WriteLine(resultLine);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"{Path.GetFileName(imagePath)} -> ОШИБКА: {ex.Message}";
                        writer.WriteLine(errorMessage);
                        Debug.WriteLine(errorMessage);
                    }
                }

                writer.WriteLine(new string('-', 50));
                writer.WriteLine("Обработка завершена.");
            }

            Debug.WriteLine($"Результаты сохранены в: {outputFile}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при обработке папки: {ex.Message}");
        }
    }

    private void OpenBitmap(Bitmap bitmap)
    {
        try
        {
            // Создаем временный файл
            string tempPath = Path.GetTempFileName() + ".png";

            // Сохраняем Bitmap во временный файл
            bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

            // Открываем файл с помощью стандартного приложения
            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            });

            // Удаляем временный файл после задержки
            Task.Delay(1000).ContinueWith(t =>
            {
                try { File.Delete(tempPath); }
                catch { /* Игнорируем ошибки удаления */ }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Не удалось открыть Bitmap: {ex.Message}");
        }
    }

    //Распознавание лобби рейда
    //public List<string> RecognizeNicknames(Bitmap image)
    //{
    //    var results = new List<string>();

    //    try
    //    {
    //        using (var finalImage = PreprocessForTesseract(image))
    //        {
    //            int width = finalImage.Width;
    //            int height = finalImage.Height;
    //            int middleX = 430; // 1075;//430; //215

    //            // Распознаем левую колонку
    //            //RecognizeColumn(finalImage, 0, 0, middleX, height, results);
    //            RecognizeColumnLines(finalImage, 0, 0, middleX, height, results);

    //            //RecognizeColumn(finalImage, 0, 120, middleX, 60, results, true);

    //            // Распознаем правую колонку
    //            //RecognizeColumn(finalImage, middleX, 0, width - middleX, height, results);
    //            RecognizeColumnLines(finalImage, middleX, 0, width - middleX, height, results);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Ошибка: {ex.Message}");
    //    }

    //    return results;
    //}

    private Pix ConvertBitmapToPix(Bitmap bitmap)
    {
        // Сохраняем временно в память и загружаем как Pix
        using (var memoryStream = new MemoryStream())
        {
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;
            return Pix.LoadFromMemory(memoryStream.ToArray());
        }
    }

    //Распознавание лобби рейда
    //private void RecognizeColumnLines(Bitmap image, int startX, int startY, int columnWidth, int height,
    //                     List<string> results)
    //{
    //    try
    //    {
    //        int totalNicknames = 4; // Всего 4 ника
    //        int nicknameHeight = 60;

    //        for (int i = 0; i < totalNicknames; i++)
    //        {
    //            // Вычисляем координаты для текущего ника
    //            int currentY = startY + (i * nicknameHeight);

    //            // Обрезаем изображение для текущего ника
    //            using (var croppedImage = new Bitmap(columnWidth, nicknameHeight))
    //            using (var graphics = Graphics.FromImage(croppedImage))
    //            {
    //                graphics.DrawImage(image, new Rectangle(0, 0, columnWidth, nicknameHeight),
    //                                 new Rectangle(startX, currentY, columnWidth, nicknameHeight), GraphicsUnit.Pixel);

    //                // Распознаем текст для текущего ника
    //                string nickname = RecognizeSingleNickname(croppedImage);

    //                if (!string.IsNullOrWhiteSpace(nickname))
    //                {
    //                    results.Add(nickname);
    //                    Debug.WriteLine($"Распознан ник {i + 1}: {nickname}");
    //                }
    //                else
    //                {
    //                    results.Add(""); // Добавляем пустую строку, если не распознали
    //                    Debug.WriteLine($"Ник {i + 1} не распознан");
    //                }
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Ошибка распознавания ников: {ex.Message}");
    //    }
    //}

    private Bitmap CropImage(Bitmap original, int x, int y, int width, int height)
    {
        var cropped = new Bitmap(width, height);

        Rectangle srcRect = new Rectangle(x, y, width, height);
        Rectangle destRect = new Rectangle(0, 0, width, height);

        using (var graphics = Graphics.FromImage(cropped))
        {
            graphics.DrawImage(original, destRect, srcRect, GraphicsUnit.Pixel);
        }

        return cropped;
    }

    public double? RecognizeNumber(Bitmap image)
    {
        if (_disposed) throw new ObjectDisposedException("TextRecognizer");

        string sessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
        //_logger.Log($"=== Начало распознавания сессии {sessionId} ===");

        try
        {
            // Сохраняем оригинальное изображение
            //_logger.LogImage(image, sessionId, "Original");

            // Предобработка
            //_logger.Log("Начало предобработки изображения...");
            var processedImage = PreprocessNumberForTesseract(image);

            //_logger.LogImage(processedImage, sessionId, "Processed");


            using (var pix = ConvertBitmapToPix(processedImage))
            //using (var page = _engine.Process(pix, PageSegMode.SingleWord))
            using (var page = _engine.Process(pix))
            {
                string text = page.GetText().Trim();
                float confidence = page.GetMeanConfidence();

                
                // Детальное логирование результатов
                //_logger.Log($"Результат распознавания: '{text}'");
                //_logger.Log($"Уверенность распознавания: {confidence:P2}");

                // Логирование посимвольно
                using (var iterator = page.GetIterator())
                {
                    //_logger.Log("Посимвольный анализ:");
                    int charIndex = 0;
                    var recognizedChars = new List<string>();
                    Tesseract.Rect? previousBounds = null; // Только предыдущий bounding box

                    do
                    {
                        string charText = iterator.GetText(PageIteratorLevel.Symbol);
                        float charConfidence = iterator.GetConfidence(PageIteratorLevel.Symbol);
                        if (!string.IsNullOrEmpty(charText))
                        {
                            //_logger.Log($"  Символ {charIndex}: '{charText}' (уверенность: {charConfidence:F2})");
                            charIndex++;

                            var choiceIterator = iterator.GetChoiceIterator();

                            // Перебираем все альтернативные варианты
                            while (choiceIterator.Next())
                            {
                                string alternativeText = choiceIterator.GetText();
                                float choiceConfidence = choiceIterator.GetConfidence();

                                //_logger.Log($"Вариант: {alternativeText}, Уверенность: {choiceConfidence:F2}");
                            }
                        }

                        if (iterator.TryGetBoundingBox(PageIteratorLevel.Symbol, out var bounds))
                        {
                            //_logger.Log($"Символ в области: [{bounds.X1},{bounds.Y1}-{bounds.X2},{bounds.Y2}]");


                            // Проверяем валидность bounding box'а
                            //bool isBoundsValid = IsValidSymbolBounds(bounds, charText);

                            //bool shouldSkip = previousBounds.HasValue && isBoundsValid &&
                            // IsInsideOrOverlapping(bounds, previousBounds.Value);
                            //if (!shouldSkip)
                            //{
                            //    recognizedChars.Add(charText);
                            //    if (isBoundsValid)
                            //    {
                            //        previousBounds = bounds;
                            //    }
                            //    _logger.Log($"  Добавлен символ: '{charText}'");
                            //}
                            //else
                            //{
                            //    _logger.Log($"  Пропущен: символ '{charText}' пересекается с предыдущим");
                            //}


                            // Отладка
                            //var symbolImage = CropImage(processedImage, bounds.X1, bounds.Y1, bounds.X2 - bounds.X1, bounds.Y2 - bounds.Y1);
                            //_logger.LogImage(symbolImage, sessionId, $"debug_symbol_{bounds.X1}_{bounds.Y1}");

                            //Повторное распознавание отдельного символа
                            //string reRecognizedChar = RecognizeSingleCharacter(symbolImage);
                            //if (!string.IsNullOrEmpty(reRecognizedChar))
                            //{
                            //    recognizedChars.Add(reRecognizedChar);
                            //    _logger.Log($"  Перераспознанный символ {charIndex}: '{reRecognizedChar}'");
                            //}
                            //else
                            //{
                            //    recognizedChars.Add(charText); // Оставляем оригинальный символ
                            //}

                        }
                        else
                        {
                            //recognizedChars.Add(charText);
                        }
                    } while (iterator.Next(PageIteratorLevel.Symbol));

                    //if (recognizedChars.Count > 0)
                    //{
                    //    string combinedText = string.Join("", recognizedChars);
                    //    _logger.Log($"Объединенный результат: '{combinedText}'");
                    //    text = combinedText; // Заменяем оригинальный текст
                    //}

                }

                var result = ParseNumber(text);
                //_logger.Log($"Распарсенное число: {result}");
                //_logger.Log($"=== Завершение сессии {sessionId} ===");

                return result;
            }
        }
        catch (Exception ex)
        {
            //_logger.Log($"ОШИБКА в сессии {sessionId}: {ex.Message}");
            //_logger.Log($"Stack trace: {ex.StackTrace}");
            return null;
        }
    }
    private bool IsValidSymbolBounds(Tesseract.Rect bounds, string symbol)
    {
        int width = bounds.X2 - bounds.X1;

        // Настройте эти значения под ваш случай
        const int MAX_SYMBOL_WIDTH = 37;
        const int MIN_SYMBOL_WIDTH = 4;

        bool isValid = width >= MIN_SYMBOL_WIDTH && width <= MAX_SYMBOL_WIDTH;

        if (!isValid)
        {
            _logger.Log($"  Некорректный bounding box: ширина {width}px для символа '{symbol}'");
        }

        return isValid;
    }

    private bool IsInsideOrOverlapping(Tesseract.Rect inner, Tesseract.Rect outer)
    {
        // Проверяем, находится ли inner внутри outer или значительно пересекается с ним
        // Можно настроить пороги в зависимости от ваших потребностей

        // Простая проверка: если большая часть inner находится внутри outer
        double overlapWidth = Math.Max(0, Math.Min(inner.X2, outer.X2) - Math.Max(inner.X1, outer.X1));
        double overlapHeight = Math.Max(0, Math.Min(inner.Y2, outer.Y2) - Math.Max(inner.Y1, outer.Y1));
        double innerArea = (inner.X2 - inner.X1) * (inner.Y2 - inner.Y1);
        double overlapArea = overlapWidth * overlapHeight;

        // Если более 50% площади текущего символа пересекается с предыдущим
        return overlapArea > innerArea * 0.3;
    }

    // Функция для распознавания одиночного символа
    private string RecognizeSingleCharacter(Bitmap symbolImage)
    {
        try
        {
            // Создаем временный экземпляр Tesseract для избежания конфликтов
            using (var tempEngine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
            {
                tempEngine.SetVariable("tessedit_char_whitelist", "0123456789."); // Ограничиваем допустимые символы
                tempEngine.SetVariable("tessedit_pageseg_mode", "10"); // PageSegMode.SingleChar

                using (var pix = ConvertBitmapToPix(symbolImage))
                using (var page = tempEngine.Process(pix))
                {
                    string text = page.GetText().Trim();
                    float confidence = page.GetMeanConfidence();

                    // Фильтруем результат
                    if (!string.IsNullOrEmpty(text) && confidence > 0.7f)
                    {
                        // Берем только первый символ (на случай если распозналось несколько)
                        return text.Length > 0 ? text[0].ToString() : string.Empty;
                    }
                }
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Log($"Ошибка при распознавании одиночного символа: {ex.Message}");
            return string.Empty;
        }
    }


    //public double? RecognizeNumber(Bitmap image)
    //{
    //    if (_disposed) throw new ObjectDisposedException("TextRecognizer");

    //    try
    //    {
    //        using (var pix = ConvertBitmapToPix(PreprocessNumberForTesseract(image)))
    //        using (var page = _engine.Process(pix, PageSegMode.SingleWord))
    //        {
    //            string text = page.GetText().Trim();
    //            Debug.WriteLine($"{page.GetText()}");
    //            //_logger.Log($"Full text: {page.GetText()}");
    //            //_logger.Log($"Trimmed text: {text}");
    //            return ParseNumber(text);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Ошибка распознавания: {ex.Message}");
    //        return null;
    //    }
    //}


    //Распознавание со смещением в случае с 1-2 цифрами
    private double? ParseNumber(string text)
    {
        //_logger.Log($"Функция парсинга: {text}");
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Заменяем похожие на единицы символы
        //char[] chars = inputText.ToCharArray();
        //for (int i = 0; i < chars.Length; i++)
        //{
        //    if (chars[i] == 'l' || chars[i] == 'i' || chars[i] == '|' || chars[i] == 'I')
        //    {
        //        chars[i] = '1';
        //    }
        //}
        //string text = new string(chars); 


        // Извлекаем только цифры
        string digitsOnly = new string(text.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length == 0) return null;

        // Если точка есть в оригинале - используем как есть
        if (text.Contains('.'))
        {
            string cleanNumber = new string(text.Where(c => char.IsDigit(c) || c == '.').ToArray());
            if (double.TryParse(cleanNumber, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return result;
        }

        // Обработка случаев без точки
        double resultNumber;
        if (digitsOnly.Length == 1)
        {
            resultNumber = double.Parse(digitsOnly) / 1.0; // 1 -> 1.00
        }
        else if (digitsOnly.Length == 2)
        {
            resultNumber = double.Parse(digitsOnly[0].ToString()) +
                          double.Parse(digitsOnly[1].ToString()) / 10.0; // 23 -> 2.30
        }
        else
        {
            // Три и более цифр - точка перед последними двумя
            string numberToParse = digitsOnly.Insert(digitsOnly.Length - 2, ".");
            if (double.TryParse(numberToParse, NumberStyles.Any, CultureInfo.InvariantCulture, out resultNumber))
                return resultNumber;
            return null;
        }

        Debug.WriteLine($"Оригинал: {text}");
        Debug.WriteLine($"Сформировано число: '{resultNumber}'");
        //_logger.Log($"Сформировано число: '{resultNumber}'");

        return resultNumber;
    }

    //Распознавание без смещения для случаев с 1-2 цифрами
    //private double? ParseNumber(string text)
    //{
    //    if (string.IsNullOrWhiteSpace(text))
    //        return null;

    //    // Извлекаем только цифры
    //    string digitsOnly = new string(text.Where(char.IsDigit).ToArray());

    //    if (digitsOnly.Length == 0) return null;

    //    // Если точка есть в оригинале - используем как есть
    //    if (text.Contains('.'))
    //    {
    //        string cleanNumber = new string(text.Where(c => char.IsDigit(c) || c == '.').ToArray());
    //        if (double.TryParse(cleanNumber, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
    //            return result;
    //    }

    //    // Автоматически добавляем точку перед последними двумя цифрами
    //    string numberToParse;
    //    if (digitsOnly.Length >= 2)
    //    {
    //        numberToParse = digitsOnly.Insert(digitsOnly.Length - 2, ".");
    //    }
    //    else
    //    {
    //        numberToParse = "0." + digitsOnly.PadLeft(2, '0');
    //    }

    //    Debug.WriteLine($"Оригинал: {text}");
    //    Debug.WriteLine($"Сформировано число: '{numberToParse}'");

    //    if (double.TryParse(numberToParse, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedResult))
    //        return parsedResult;

    //    return null;
    //}

    //Распознавание лобби рейда
    //private string RecognizeSingleNickname(Bitmap image)
    //{
    //    try
    //    {
    //        using (var engine = new TesseractEngine(@"./tessdata", "eng+rus", EngineMode.Default))
    //        {
    //            engine.SetVariable("user_defined_dpi", "300");
    //            engine.SetVariable("tessedit_char_whitelist",
    //                 "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя");
    //            engine.SetVariable("tessedit_pageseg_mode", "7");

    //            using (var pix = ConvertBitmapToPix(image))
    //            using (var page = engine.Process(pix))
    //            {
    //                string text = page.GetText().Trim();
    //                return text;
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Ошибка распознавания одного ника: {ex.Message}");
    //        return string.Empty;
    //    }
    //}

    //Распознавание лобби рейда
    //private void RecognizeColumn(Bitmap image, int x, int y, int width, int height,
    //                         List<string> results)
    //{
    //    try
    //    {
    //        // Обрезаем изображение напрямую из Bitmap
    //        using (var croppedImage = new Bitmap(width, height))
    //        using (var graphics = Graphics.FromImage(croppedImage))
    //        {
    //            graphics.DrawImage(image, new Rectangle(0, 0, width, height),
    //                             new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
    //            OpenBitmap(croppedImage);

    //            // Используем Tesseract напрямую с Bitmap
    //            using (var engine = new TesseractEngine(@"./tessdata", "eng+rus", EngineMode.Default))
    //            {
    //                engine.SetVariable("user_defined_dpi", "300");

    //                engine.SetVariable("tessedit_char_whitelist",
    //                    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя");

    //                engine.SetVariable("tessedit_pageseg_mode", "7");

    //                // Конвертируем Bitmap в Pix (для Tesseract)
    //                using (var pix = ConvertBitmapToPix(croppedImage))
    //                using (var page = engine.Process(pix))
    //                {
    //                    string text = page.GetText();

    //                    var lines = text.Split('\n')
    //                        .Where(line => !string.IsNullOrWhiteSpace(line))
    //                        .Select(line => line.Trim())
    //                        .ToList();

    //                    results.AddRange(lines);
    //                }
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Ошибка распознавания колонки: {ex.Message}");
    //    }
    //}


    //public Bitmap PreprocessNumberForTesseract(Bitmap original)
    //{
    //    using (var src = BitmapConverter.ToMat(original))
    //    using (var mat = new Mat())
    //    using (var hsv = new Mat())
    //    using (var mask = new Mat())
    //    {
    //        // Конвертируем в HSV для лучшей сегментации по цвету
    //        Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV);

    //        // Диапазоны цветов фона в HSV
    //        var lowerGreen1 = new Scalar(35, 40, 35);   // Для светлых зеленых тонов
    //        var upperGreen1 = new Scalar(85, 255, 255);

    //        var lowerGreen2 = new Scalar(100, 40, 25);  // Для темных зеленых тонов  
    //        var upperGreen2 = new Scalar(140, 255, 255);

    //        // Создаем маску для фона
    //        using (var mask1 = new Mat())
    //        using (var mask2 = new Mat())
    //        using (var whiteBackground = new Mat(src.Size(), MatType.CV_8UC3, new Scalar(255, 255, 255)))
    //        {
    //            Cv2.InRange(hsv, lowerGreen1, upperGreen1, mask1);
    //            Cv2.InRange(hsv, lowerGreen2, upperGreen2, mask2);

    //            // Объединяем маски
    //            Cv2.BitwiseOr(mask1, mask2, mask);

    //            // Инвертируем маску - теперь у нас маска для текста
    //            Cv2.BitwiseNot(mask, mask);
    //            Mat inverted = new Mat();
    //            Cv2.BitwiseNot(mask, inverted);
    //            ShowPreviewWindow(inverted, "Preview - Preprocessed Image");

    //            // Копируем только текст на белый фон
    //            src.CopyTo(whiteBackground, mask);

    //            // Конвертируем в grayscale
    //            Cv2.CvtColor(whiteBackground, mat, ColorConversionCodes.BGR2GRAY);
    //        }

            

    //        using (var sharpened = new Mat())
    //        {
    //            var kernel = new float[,] { { -1, -1, -1 }, { -1, 9, -1 }, { -1, -1, -1 } };
    //            using (var kernelMat = new Mat(3, 3, MatType.CV_32F))
    //            {
    //                kernelMat.Set(0, 0, -1f); kernelMat.Set(0, 1, -1f); kernelMat.Set(0, 2, -1f);
    //                kernelMat.Set(1, 0, -1f); kernelMat.Set(1, 1, 9f); kernelMat.Set(1, 2, -1f);
    //                kernelMat.Set(2, 0, -1f); kernelMat.Set(2, 1, -1f); kernelMat.Set(2, 2, -1f);
    //                Cv2.Filter2D(mat, sharpened, MatType.CV_8U, kernelMat);
    //            }
    //            Cv2.BitwiseOr(sharpened, sharpened, mat);
    //        }

    //        // Бинаризация (пороговая обработка)
    //        Cv2.Threshold(mat, mat, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

    //        // Убираем мелкий шум (исправленная морфология)
    //        using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 1)))
    //        {
    //            Cv2.MorphologyEx(mat, mat, MorphTypes.Open, kernel);
    //        }

    //        // Увеличиваем размер
    //        Cv2.Resize(mat, mat, new OpenCvSharp.Size(mat.Width * 2, mat.Height * 2),
    //                  interpolation: InterpolationFlags.Lanczos4);

    //        // Дополнительное улучшение контраста
    //        Cv2.Normalize(mat, mat, 0, 255, NormTypes.MinMax);


    //        //ShowPreviewWindow(mat, "Preview - Preprocessed Image");

    //        return BitmapConverter.ToBitmap(mat);
    //    }
    //}

    private void ShowPreviewWindow(Mat image, string windowName = "Preview")
    {
        // Создаем окно с флагом всегда поверх других
        Cv2.NamedWindow(windowName, WindowFlags.GuiExpanded);

        // Показываем изображение
        Cv2.ImShow(windowName, image);

        // Ждем немного, чтобы окно успело отобразиться
        // (не блокируем основной поток)

        // Или альтернатива - ручное закрытие по клавише
        Cv2.WaitKey(1); // Просто обновляем окно
    }


    public Bitmap PreprocessNumberForTesseract(Bitmap original)
    {
        using (var src = BitmapConverter.ToMat(original))
        using (var mat = new Mat())
        using (var hsv = new Mat())
        using (var mask = new Mat())
        {
            // Конвертируем в HSV для лучшей сегментации по цвету
            Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV);

            // Диапазоны цветов фона в HSV
            var lowerGreen1 = new Scalar(35, 40, 35);   // Для светлых зеленых тонов
            var upperGreen1 = new Scalar(85, 255, 255);

            var lowerGreen2 = new Scalar(100, 40, 25);  // Для темных зеленых тонов  
            var upperGreen2 = new Scalar(140, 255, 255);

            // Создаем маску для фона
            using (var mask1 = new Mat())
            using (var mask2 = new Mat())
            using (var whiteBackground = new Mat(src.Size(), MatType.CV_8UC3, new Scalar(255, 255, 255)))
            {
                Cv2.InRange(hsv, lowerGreen1, upperGreen1, mask1);
                Cv2.InRange(hsv, lowerGreen2, upperGreen2, mask2);

                // Объединяем маски
                Cv2.BitwiseOr(mask1, mask2, mask);

                // Инвертируем маску - теперь у нас маска для текста
                Cv2.BitwiseNot(mask, mask);
                Mat inverted = new Mat();
                Cv2.BitwiseNot(mask, inverted);

                Cv2.Resize(inverted, inverted, new OpenCvSharp.Size(inverted.Width * 2, inverted.Height * 2),
                    interpolation: InterpolationFlags.Cubic);

                //Cv2.Erode(inverted, inverted, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 1)));
                //Cv2.Dilate(inverted, inverted, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 1)));
                //ShowPreviewWindow(inverted, "Hole");

                //OpenBitmap(BitmapConverter.ToBitmap(mat));
                return BitmapConverter.ToBitmap(inverted);
            }
        }
    }


    private Mat InvertMat(Mat input)
    {
        var inverted = new Mat();
        Cv2.BitwiseNot(input, inverted);
        return inverted;
    }

    //public Bitmap PreprocessNumberForTesseract(Bitmap original)
    //{
    //    using (var src = BitmapConverter.ToMat(original))
    //    using (var mat = new Mat())
    //    {
    //        Cv2.CvtColor(src, mat, ColorConversionCodes.BGR2GRAY);

    //        Cv2.Resize(mat, mat, new OpenCvSharp.Size(mat.Width * 2, mat.Height * 2),
    //                  interpolation: InterpolationFlags.Lanczos4);


    //        //OpenBitmap(BitmapConverter.ToBitmap(mat));
    //        return BitmapConverter.ToBitmap(mat);
    //    }
    //}


    //public static Bitmap PreprocessNumberForTesseract(Bitmap original)
    //{
    //    // Конвертируем Bitmap в ImageSharp через MemoryStream
    //    using var memoryStream = new MemoryStream();
    //    original.Save(memoryStream, ImageFormat.Png);
    //    memoryStream.Position = 0;

    //    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(memoryStream);

    //    image.Mutate(x =>
    //    {
    //        x.Grayscale();
    //        x.Resize(image.Width * 2, image.Height * 2, KnownResamplers.Lanczos8);
    //    });

    //    // Конвертируем обратно в Bitmap
    //    using var outputStream = new MemoryStream();
    //    image.SaveAsPng(outputStream);
    //    outputStream.Position = 0;
    //    return new Bitmap(outputStream);
    //}

    //public static Bitmap PreprocessNumberForTesseract(Bitmap original)
    //{
    //    var newSize = new Size(original.Width * 2, original.Height * 2);
    //    var result = new Bitmap(newSize.Width, newSize.Height);

    //    using (var g = Graphics.FromImage(result))
    //    {
    //        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

    //        // Convert to grayscale
    //        using var attributes = new ImageAttributes();
    //        attributes.SetColorMatrix(new ColorMatrix(new float[][] {
    //        new float[] {0.299f, 0.299f, 0.299f, 0, 0},
    //        new float[] {0.587f, 0.587f, 0.587f, 0, 0},
    //        new float[] {0.114f, 0.114f, 0.114f, 0, 0},
    //        new float[] {0, 0, 0, 1, 0},
    //        new float[] {0, 0, 0, 0, 1}
    //    }));

    //        g.DrawImage(original,
    //            new Rectangle(0, 0, newSize.Width, newSize.Height),
    //            0, 0, original.Width, original.Height,
    //            GraphicsUnit.Pixel, attributes);
    //    }

    //    return result;
    //}

    // Распознавание лобби рейда 
    //public Bitmap PreprocessForTesseract(Bitmap original)
    //{
    //    using (var src = BitmapConverter.ToMat(original))
    //    using (var mat = new Mat())
    //    {
    //        // 1. Grayscale
    //        Cv2.CvtColor(src, mat, ColorConversionCodes.BGR2GRAY);

    //        // 8. Увеличение
    //        Cv2.Resize(mat, mat, new OpenCvSharp.Size(mat.Width * 2, mat.Height * 2),
    //                  interpolation: InterpolationFlags.Lanczos4);

    //        // 2. Немного шума убираем
    //        Cv2.GaussianBlur(mat, mat, new OpenCvSharp.Size(1, 1), 0);

    //        // 3. Адаптивная бинаризация
    //        Cv2.AdaptiveThreshold(mat, mat, 255,
    //            AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 21, 10);

    //        //OpenBitmap(BitmapConverter.ToBitmap(mat));
    //        // 4. Инверсия
    //        Cv2.BitwiseNot(mat, mat);

    //        // 5. Заливка фона
    //        var filled = mat.Clone();
    //        Cv2.CopyMakeBorder(mat, filled, 1, 1, 1, 1, BorderTypes.Constant, Scalar.Black);
    //        Cv2.FloodFill(filled, new OpenCvSharp.Point(0, 0), Scalar.White);

    //        var roi = new OpenCvSharp.Rect(1, 1, filled.Width - 2, filled.Height - 2);
    //        var cropped = new Mat(filled, roi);

    //        // 6. Морфологическое закрытие
    //        var kernelClose = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 1));
    //        Cv2.MorphologyEx(cropped, mat, MorphTypes.Close, kernelClose);

    //        // 7. Dilation (немного расширим буквы)
    //        //var kernelDilate = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 1));
    //        //Cv2.Dilate(mat, mat, kernelDilate);


    //        //9.Повышение резкости
    //        var sharpenKernel = new Mat(3, 3, MatType.CV_32F);
    //        float[] kernelData =
    //                            {
    //                            0, -0.5f, 0,
    //                            -0.5f, 3, -0.5f,
    //                            0, -0.5f, 0
    //                            };
    //        sharpenKernel.SetArray(kernelData);
    //        Cv2.Filter2D(mat, mat, MatType.CV_8U, sharpenKernel);

    //        return BitmapConverter.ToBitmap(mat);
    //    }
    //}


    //public Bitmap PreprocessForTesseract(Bitmap original)
    //{
    //    using (var src = BitmapConverter.ToMat(original))
    //    using (var gray = new Mat())
    //    using (var resized = new Mat())
    //    using (var binary = new Mat())
    //    using (var morph = new Mat())
    //    {
    //        // 1. Grayscale
    //        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

    //        // 2. Увеличение (лучше до бинаризации)
    //        Cv2.Resize(gray, resized, new OpenCvSharp.Size(gray.Width * 2, gray.Height * 2),
    //                  interpolation: InterpolationFlags.Lanczos4);

    //        // 3. Адаптивная бинаризация
    //        Cv2.AdaptiveThreshold(resized, binary, 255,
    //            AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 21, 10);

    //        // 4. Морфологическое закрытие (чтобы убрать дырки в буквах)
    //        var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
    //        Cv2.MorphologyEx(binary, morph, MorphTypes.Close, kernel);

    //        // 5. Инвертируем (Tesseract любит чёрный фон / белый текст)
    //        Cv2.BitwiseNot(morph, morph);

    //        // 6. Заливка фона
    //        Cv2.FloodFill(morph, new OpenCvSharp.Point(0, 0), new Scalar(255));

    //        return BitmapConverter.ToBitmap(morph);
    //    }
    //}


    //public Bitmap PreprocessForTesseract(Bitmap original)
    //{
    //    using (var src = BitmapConverter.ToMat(original))
    //    using (var gray = new Mat())
    //    using (var blurred = new Mat())
    //    using (var binary = new Mat())
    //    using (var inverted = new Mat())
    //    using (var floodFilled = new Mat())
    //    using (var morph = new Mat())
    //    using (var resized = new Mat())
    //    using (var sharpened = new Mat())
    //    {
    //        // 1. Grayscale
    //        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

    //        // 2. Легкое размытие для уменьшения шума
    //        Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(1, 1), 0);

    //        // 3. Адаптивная бинаризация с улучшенными параметрами
    //        Cv2.AdaptiveThreshold(blurred, binary, 255,
    //            AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 7, 5);

    //        // 4. Инвертирование
    //        Cv2.BitwiseNot(binary, inverted);

    //        // 5. Заливка фона
    //        Cv2.FloodFill(inverted, new OpenCvSharp.Point(0, 0), new Scalar(255));

    //        OpenBitmap(BitmapConverter.ToBitmap(inverted));

    //        // 6. Морфологическое закрытие ДЛЯ СОЕДИНЕНИЯ РАЗРЫВОВ В БУКВАХ
    //        var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 1));
    //        Cv2.MorphologyEx(inverted, morph, MorphTypes.Close, kernel);

    //        // 7. Увеличение с лучшей интерполяцией
    //        Cv2.Resize(morph, resized, new OpenCvSharp.Size(morph.Width * 2, morph.Height * 2),
    //                  interpolation: InterpolationFlags.Lanczos4);


    //            var sharpenKernel = new Mat(3, 3, MatType.CV_32F);
    //            float[] kernelData = { 0, -0.5f, 0, -0.5f, 3, -0.5f, 0, -0.5f, 0 };
    //            sharpenKernel.SetArray(kernelData);
    //            Cv2.Filter2D(resized, sharpened, MatType.CV_8U, sharpenKernel);

    //            return BitmapConverter.ToBitmap(sharpened);
    //    }
    //}
    //public Bitmap PreprocessForTesseract(Bitmap original)
    //{
    //    using (var src = BitmapConverter.ToMat(original))
    //    using (var gray = new Mat())
    //    using (var binary = new Mat())
    //    using (var inverted = new Mat())
    //    using (var resized = new Mat())
    //    {
    //        // 1. Grayscale
    //        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);


    //        // 2. Адаптивная бинаризация (сохраняет детали!)
    //        Cv2.AdaptiveThreshold(gray, binary, 255,
    //            AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 7, 5);
    //        ////OpenBitmap(BitmapConverter.ToBitmap(binary));

    //        // 3. ИНВЕРТИРОВАТЬ ЦВЕТА
    //        Cv2.BitwiseNot(binary, inverted);

    //        Cv2.FloodFill(inverted, new OpenCvSharp.Point(0, 0), new Scalar(255));

    //        // 4. Увеличение с интерполяцией
    //        Cv2.Resize(inverted, resized, new OpenCvSharp.Size(binary.Width * 2, binary.Height * 2),
    //                   interpolation: InterpolationFlags.Cubic);

    //        return BitmapConverter.ToBitmap(resized);
    //    }
    //}


    //public Bitmap PreprocessForOcr(Bitmap original)
    //{
    //    // 1. Убираем тени / приводим к бинарному виду
    //    var noShadow = RemoveShadow(original, 100);
    //    //var noShadow = AdaptiveThreshold(original);

    //    // 2. Увеличиваем картинку – Tesseract лучше работает на крупных буквах
    //    int newWidth = noShadow.Width * 2;
    //    int newHeight = noShadow.Height * 2;
    //    var resized = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

    //    using (var g = Graphics.FromImage(resized))
    //    {
    //        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    //        g.SmoothingMode = SmoothingMode.None;
    //        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    //        g.DrawImage(noShadow, 0, 0, newWidth, newHeight);
    //    }
    //    noShadow.Dispose();

    //    OpenBitmap(resized);

    //    // 3. Улучшаем яркость/контраст
    //    ApplyContrastBrightness(resized, 1.5f, 0.1f);

    //    OpenBitmap(resized);

    //    // 4. Делаем sharpening (свёртка)
    //    ApplySharpening(resized);

    //    OpenBitmap(resized);

    //    return resized;
    //}


    //public static Bitmap AdaptiveThreshold(Bitmap original, int blockSize = 15, int c = 5)
    //{
    //    // Переводим в серый
    //    Bitmap gray = new Bitmap(original.Width, original.Height, PixelFormat.Format24bppRgb);
    //    using (Graphics g = Graphics.FromImage(gray))
    //    {
    //        var cm = new ColorMatrix(new float[][] {
    //        new float[] {0.299f, 0.299f, 0.299f, 0, 0},
    //        new float[] {0.587f, 0.587f, 0.587f, 0, 0},
    //        new float[] {0.114f, 0.114f, 0.114f, 0, 0},
    //        new float[] {0,      0,      0,      1, 0},
    //        new float[] {0,      0,      0,      0, 1}
    //    });
    //        var ia = new ImageAttributes();
    //        ia.SetColorMatrix(cm);
    //        g.DrawImage(original, new Rectangle(0, 0, gray.Width, gray.Height),
    //            0, 0, original.Width, original.Height, GraphicsUnit.Pixel, ia);
    //    }

    //    // Лочим bitmap для доступа к пикселям
    //    Bitmap result = new Bitmap(gray.Width, gray.Height, PixelFormat.Format1bppIndexed);

    //    BitmapData data = gray.LockBits(new Rectangle(0, 0, gray.Width, gray.Height),
    //        ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

    //    BitmapData resData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height),
    //        ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

    //    int stride = data.Stride;
    //    int resStride = resData.Stride;

    //    unsafe
    //    {
    //        byte* srcPtr = (byte*)data.Scan0;
    //        byte* dstPtr = (byte*)resData.Scan0;

    //        int half = blockSize / 2;

    //        for (int y = 0; y < gray.Height; y++)
    //        {
    //            for (int x = 0; x < gray.Width; x++)
    //            {
    //                // Считаем среднее яркости в окрестности blockSize x blockSize
    //                int sum = 0, count = 0;
    //                for (int dy = -half; dy <= half; dy++)
    //                {
    //                    int yy = y + dy;
    //                    if (yy < 0 || yy >= gray.Height) continue;

    //                    byte* row = srcPtr + yy * stride;
    //                    for (int dx = -half; dx <= half; dx++)
    //                    {
    //                        int xx = x + dx;
    //                        if (xx < 0 || xx >= gray.Width) continue;

    //                        byte val = row[xx * 3]; // gray => все каналы одинаковы
    //                        sum += val;
    //                        count++;
    //                    }
    //                }

    //                int mean = sum / count;
    //                int pixel = srcPtr[y * stride + x * 3];
    //                bool isWhite = pixel > (mean - c);

    //                // Установка пикселя в 1bpp (чёрно-белый)
    //                int byteIndex = x / 8;
    //                int bitIndex = 7 - (x % 8);
    //                if (isWhite)
    //                    dstPtr[y * resStride + byteIndex] |= (byte)(1 << bitIndex);
    //            }
    //        }
    //    }

    //    gray.UnlockBits(data);
    //    result.UnlockBits(resData);

    //    return result;
    //}

    ///// <summary>
    ///// Убираем тени + бинаризация через порог
    ///// </summary>
    //public Bitmap RemoveShadow(Bitmap original, int threshold = 128)
    //{
    //    var gray = new Bitmap(original.Width, original.Height, PixelFormat.Format24bppRgb);

    //    Rectangle rect = new Rectangle(0, 0, gray.Width, gray.Height);
    //    BitmapData data = gray.LockBits(rect, ImageLockMode.WriteOnly, gray.PixelFormat);

    //    int stride = data.Stride;
    //    unsafe
    //    {
    //        byte* ptr = (byte*)data.Scan0;
    //        for (int y = 0; y < original.Height; y++)
    //        {
    //            for (int x = 0; x < original.Width; x++)
    //            {
    //                Color pixel = original.GetPixel(x, y);
    //                int grayVal = (int)(pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11);

    //                byte val = (grayVal > threshold) ? (byte)255 : (byte)0;

    //                int idx = y * stride + x * 3;
    //                ptr[idx] = ptr[idx + 1] = ptr[idx + 2] = val;
    //            }
    //        }
    //    }
    //    gray.UnlockBits(data);

    //    return gray;
    //}

    ///// <summary>
    ///// Контраст + яркость
    ///// </summary>
    //private void ApplyContrastBrightness(Bitmap image, float contrast, float brightness)
    //{
    //    float[][] matrix =
    //    {
    //    new float[] {contrast, 0, 0, 0, 0},
    //    new float[] {0, contrast, 0, 0, 0},
    //    new float[] {0, 0, contrast, 0, 0},
    //    new float[] {0, 0, 0, 1, 0},
    //    new float[] {brightness, brightness, brightness, 0, 1}
    //};

    //    var cm = new ColorMatrix(matrix);
    //    var ia = new ImageAttributes();
    //    ia.SetColorMatrix(cm);

    //    using (var g = Graphics.FromImage(image))
    //    {
    //        g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
    //            0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
    //    }
    //}

    ///// <summary>
    ///// Sharpening через ядро
    ///// </summary>
    //private void ApplySharpening(Bitmap image)
    //{
    //    // Ядро 3x3 для sharpening
    //    int[,] kernel =
    //    {
    //    { -1, -1, -1 },
    //    { -1,  9, -1 },
    //    { -1, -1, -1 }
    //};

    //    int kSize = 3;
    //    int kOffset = kSize / 2;

    //    var temp = (Bitmap)image.Clone();

    //    Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
    //    BitmapData srcData = temp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
    //    BitmapData dstData = image.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

    //    int stride = srcData.Stride;

    //    unsafe
    //    {
    //        byte* src = (byte*)srcData.Scan0;
    //        byte* dst = (byte*)dstData.Scan0;

    //        for (int y = kOffset; y < image.Height - kOffset; y++)
    //        {
    //            for (int x = kOffset; x < image.Width - kOffset; x++)
    //            {
    //                int r = 0, g = 0, b = 0;

    //                for (int ky = -kOffset; ky <= kOffset; ky++)
    //                {
    //                    for (int kx = -kOffset; kx <= kOffset; kx++)
    //                    {
    //                        int px = x + kx;
    //                        int py = y + ky;

    //                        byte* p = src + py * stride + px * 3;
    //                        int kVal = kernel[ky + kOffset, kx + kOffset];

    //                        b += p[0] * kVal;
    //                        g += p[1] * kVal;
    //                        r += p[2] * kVal;
    //                    }
    //                }

    //                byte* d = dst + y * stride + x * 3;
    //                d[0] = (byte)Math.Clamp(b, 0, 255);
    //                d[1] = (byte)Math.Clamp(g, 0, 255);
    //                d[2] = (byte)Math.Clamp(r, 0, 255);
    //            }
    //        }
    //    }

    //    temp.UnlockBits(srcData);
    //    image.UnlockBits(dstData);
    //    temp.Dispose();
    //}




    //public Bitmap PreprocessImage(Bitmap original)
    //{
    //    // Увеличиваем разрешение в 2 раза для лучшего распознавания
    //    int newWidth = original.Width * 2;
    //    int newHeight = original.Height * 2;

    //    var processed = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

    //    using (var graphics = Graphics.FromImage(processed))
    //    {
    //        // Настраиваем высокое качество рендеринга
    //        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
    //        graphics.SmoothingMode = SmoothingMode.HighQuality;
    //        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
    //        graphics.CompositingQuality = CompositingQuality.HighQuality;

    //        // Рисуем исходное изображение с увеличенным размером
    //        graphics.DrawImage(original, 0, 0, newWidth, newHeight);
    //    }

    //    // Применяем дополнительные фильтры улучшения
    //    EnhanceImage(processed);
    //    ConvertToGrayscale(processed);

    //    return processed;
    //}

    //private void ConvertToGrayscale(Bitmap image)
    //{
    //    for (int x = 0; x < image.Width; x++)
    //    {
    //        for (int y = 0; y < image.Height; y++)
    //        {
    //            Color pixel = image.GetPixel(x, y);
    //            int gray = (int)(pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11);
    //            image.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
    //        }
    //    }
    //}

    //private void EnhanceImage(Bitmap image)
    //{
    //    // Повышаем контрастность и яркость
    //    float contrast = 1.5f; // Увеличиваем контрастность
    //    float brightness = 0.1f; // Немного увеличиваем яркость

    //    var attributes = new System.Drawing.Imaging.ImageAttributes();

    //    float[][] colorMatrixElements = {
    //    new float[] {contrast, 0, 0, 0, 0},
    //    new float[] {0, contrast, 0, 0, 0},
    //    new float[] {0, 0, contrast, 0, 0},
    //    new float[] {0, 0, 0, 1, 0},
    //    new float[] {brightness, brightness, brightness, 0, 1}
    //};

    //    var colorMatrix = new System.Drawing.Imaging.ColorMatrix(colorMatrixElements);
    //    attributes.SetColorMatrix(colorMatrix);

    //    using (var graphics = Graphics.FromImage(image))
    //    {
    //        graphics.DrawImage(
    //            image,
    //            new Rectangle(0, 0, image.Width, image.Height),
    //            0, 0, image.Width, image.Height,
    //            GraphicsUnit.Pixel,
    //            attributes
    //        );
    //    }

    //    // Дополнительно: можно добавить резкость или другие фильтры
    //    ApplySharpening(image);
    //}

    //private void ApplySharpening(Bitmap image)
    //{
    //    // Простое повышение резкости
    //    using (var temp = new Bitmap(image))
    //    {
    //        for (int x = 1; x < image.Width - 1; x++)
    //        {
    //            for (int y = 1; y < image.Height - 1; y++)
    //            {
    //                Color current = temp.GetPixel(x, y);
    //                Color left = temp.GetPixel(x - 1, y);
    //                Color right = temp.GetPixel(x + 1, y);
    //                Color top = temp.GetPixel(x, y - 1);
    //                Color bottom = temp.GetPixel(x, y + 1);

    //                // Простой фильтр резкости
    //                int r = Math.Clamp(current.R * 5 - left.R - right.R - top.R - bottom.R, 0, 255);
    //                int g = Math.Clamp(current.G * 5 - left.G - right.G - top.G - bottom.G, 0, 255);
    //                int b = Math.Clamp(current.B * 5 - left.B - right.B - top.B - bottom.B, 0, 255);

    //                image.SetPixel(x, y, Color.FromArgb(r, g, b));
    //            }
    //        }
    //    }
    //}

    public void Dispose()
    {
        if (!_disposed)
        {
            //_logger.Log("Освобождение ресурсов Tesseract...");
            _engine?.Dispose();
            _disposed = true;
        }
    }
}