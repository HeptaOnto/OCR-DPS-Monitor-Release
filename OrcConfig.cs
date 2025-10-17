public class OcrConfig
{
    public Rectangle Region { get; set; } = new Rectangle(0, 0, 300, 100);

    public static string ConfigPath => "config.json";

    public static OcrConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new OcrConfig();

        string json = File.ReadAllText(ConfigPath);
        return System.Text.Json.JsonSerializer.Deserialize<OcrConfig>(json);
    }

    public void Save()
    {
        string json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(ConfigPath, json);
    }
}
