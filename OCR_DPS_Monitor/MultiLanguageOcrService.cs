using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

public class MultiLanguageOcrService
{
    private readonly List<OcrEngine> _engines = new List<OcrEngine>();
    private readonly Dictionary<string, Language> _availableLanguages = new Dictionary<string, Language>();

    public MultiLanguageOcrService()
    {
        InitializeLanguages();
    }

    private void InitializeLanguages()
    {
        var languages = OcrEngine.AvailableRecognizerLanguages.ToList();

        // Сохраняем все доступные языки
        foreach (var lang in languages)
        {
            _availableLanguages[lang.LanguageTag] = lang;
        }

        // Создаем движки для русского и английского, если доступны
        TryAddEngine("ru"); // Russian
        TryAddEngine("en"); // English

        // Если нет ни русского ни английского, используем системный
        if (_engines.Count == 0)
        {
            var systemEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (systemEngine != null)
            {
                _engines.Add(systemEngine);
                Debug.WriteLine($"Using system language: {systemEngine.RecognizerLanguage?.LanguageTag}");
            }
        }
    }

    private void TryAddEngine(string languageCode)
    {
        var lang = _availableLanguages.Values.FirstOrDefault(l =>
            l.LanguageTag.StartsWith(languageCode, StringComparison.OrdinalIgnoreCase));

        if (lang != null)
        {
            var engine = OcrEngine.TryCreateFromLanguage(lang);
            if (engine != null)
            {
                _engines.Add(engine);
                Debug.WriteLine($"Added OCR engine for: {lang.LanguageTag} - {lang.DisplayName}");
            }
        }
    }

    public async Task<MultiLanguageOcrResult> RecognizeAsync(SoftwareBitmap bitmap)
    {
        var results = new Dictionary<string, string>();
        var tasks = new List<Task>();

        foreach (var engine in _engines)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var result = await engine.RecognizeAsync(bitmap);
                    var languageTag = engine.RecognizerLanguage?.LanguageTag ?? "unknown";
                    results[languageTag] = result.Text.Trim();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error recognizing with {engine.RecognizerLanguage?.LanguageTag}: {ex.Message}");
                }
            }));
        }

        await Task.WhenAll(tasks);

        return new MultiLanguageOcrResult(results);
    }

    public async Task<string> RecognizeCombinedAsync(SoftwareBitmap bitmap)
    {
        var result = await RecognizeAsync(bitmap);
        return result.GetCombinedText();
    }

    // OcrEngine не требует Dispose, поэтому просто очищаем список
    public void Cleanup()
    {
        _engines.Clear();
    }

    public IReadOnlyList<string> AvailableLanguages => _availableLanguages.Keys.ToList();
    public IReadOnlyList<string> ActiveEngines => _engines
        .Select(e => e.RecognizerLanguage?.LanguageTag ?? "unknown")
        .ToList();
}

public class MultiLanguageOcrResult
{
    public Dictionary<string, string> Results { get; }

    public MultiLanguageOcrResult(Dictionary<string, string> results)
    {
        Results = results;
    }

    public string GetCombinedText()
    {
        // Объединяем результаты, убирая дубликаты
        var allText = Results.Values
            .SelectMany(text => text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            .Distinct()
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .ToArray();

        return string.Join(" ", allText);
    }

    public string GetTextForLanguage(string languageCode)
    {
        return Results.TryGetValue(languageCode, out var text) ? text : string.Empty;
    }

    public override string ToString()
    {
        return string.Join("\n\n", Results.Select(kv => $"[{kv.Key}]:\n{kv.Value}"));
    }
}

public class SmartOcrService
{
    private readonly MultiLanguageOcrService _multiLanguageService;
    private readonly OcrResultLogger _logger;

    public SmartOcrService()
    {
        _multiLanguageService = new MultiLanguageOcrService();
        _logger = new OcrResultLogger();
    }

    public async Task<string> RecognizeSmartAsync(SoftwareBitmap bitmap)
    {
        var result = await _multiLanguageService.RecognizeAsync(bitmap);

        // Логируем все результаты
        foreach (var kvp in result.Results)
        {
            await _logger.LogRecognitionResultAsync(kvp.Value, kvp.Key, "multi_language");
        }

        // Выбираем лучший результат или комбинируем
        return CombineResultsSmart(result);
    }

    private string CombineResultsSmart(MultiLanguageOcrResult result)
    {
        if (result.Results.Count == 1)
            return result.Results.Values.First();

        // Если есть результаты на нескольких языках
        var ruText = result.GetTextForLanguage("ru") ?? "";
        var enText = result.GetTextForLanguage("en") ?? "";

        // Простая эвристика: если русский текст содержит кириллицу, предпочитаем его
        if (ContainsCyrillic(ruText) && !string.IsNullOrWhiteSpace(ruText))
        {
            // Но добавляем английские слова, которых нет в русском результате
            var enWords = enText.Split(' ')
                .Where(word => !string.IsNullOrWhiteSpace(word) && IsLatin(word))
                .Except(ruText.Split(' '), StringComparer.OrdinalIgnoreCase);

            return ruText + " " + string.Join(" ", enWords);
        }

        return result.GetCombinedText();
    }

    private bool ContainsCyrillic(string text)
    {
        return text.Any(c => c >= 'А' && c <= 'я');
    }

    private bool IsLatin(string word)
    {
        return word.All(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));
    }

    public void Cleanup()
    {
        _multiLanguageService.Cleanup();
    }
}

public class OcrResultLogger
{
    private readonly string _logFilePath;

    public OcrResultLogger(string fileName = "ocr_results.txt")
    {
        // Папка где запущена программа
        var appFolder = AppDomain.CurrentDomain.BaseDirectory;

        // Создаем подпапку OCR_Results рядом с программой
        var ocrFolder = Path.Combine(appFolder, "OCR_Results");
        Directory.CreateDirectory(ocrFolder);

        _logFilePath = Path.Combine(ocrFolder, fileName);

        Debug.WriteLine($"Log file will be saved to: {_logFilePath}");
    }

    public async Task LogRecognitionResultAsync(string text, string languageTag, string source = "screen")
    {
        try
        {
            string content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                           $"Source: {source}, " +
                           $"Language: {languageTag}\r\n" +
                           $"Text:\r\n{text}\r\n" +
                           new string('-', 60) + "\r\n\r\n";

            await File.AppendAllTextAsync(_logFilePath, content, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error writing to log file: {ex.Message}");
        }
    }

    public async Task LogErrorAsync(string errorMessage, string operation)
    {
        try
        {
            string content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                           $"ERROR in {operation}: {errorMessage}\r\n" +
                           new string('-', 60) + "\r\n\r\n";

            await File.AppendAllTextAsync(_logFilePath, content, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error writing error to log: {ex.Message}");
        }
    }
}