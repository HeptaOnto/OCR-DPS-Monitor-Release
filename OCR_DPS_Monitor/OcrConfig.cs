public class OcrConfig
{
    public Rectangle Region { get; set; } = new Rectangle(0, 0, 0, 0);
    public Rectangle DpsRegion { get; set; } = new Rectangle(0, 0, 0, 0); 

    public static string ConfigPath => Path.Combine("config", "config.json");

    public static OcrConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new OcrConfig();

        try
        {
            string json = File.ReadAllText(ConfigPath);
            var config = System.Text.Json.JsonSerializer.Deserialize<OcrConfig>(json);

            // Защита от null значений после десериализации
            config ??= new OcrConfig();
            config.Region = config.Region != Rectangle.Empty ? config.Region : new Rectangle(0, 0, 0, 0);
            config.DpsRegion = config.DpsRegion != Rectangle.Empty ? config.DpsRegion : new Rectangle(0, 0, 0, 0);

            return config;
        }
        catch (Exception ex)
        {
            // В случае ошибки чтения/парсинга возвращаем конфиг по умолчанию
            MessageBox.Show($"Error loading config: {ex.Message}\nUsing default configuration.");
            return new OcrConfig();
        }
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigPath);

            // Создаем директорию, если она не существует
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
