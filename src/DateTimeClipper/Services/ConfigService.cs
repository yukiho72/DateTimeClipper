using System.IO;
using System.Text.Json;
using DateTimeClipper.Models;

namespace DateTimeClipper.Services;

/// <summary>
/// config.json の読み書き。読み込み失敗・不正な内容はデフォルト設定にフォールバックし、
/// 元ファイルを .bak に退避する（ユーザーの手編集内容を消さないため）。
/// </summary>
public class ConfigService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _path;

    public ConfigService(string path) => _path = path;

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DateTimeClipper", "config.json");

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(_path)) return new AppConfig();
            var loaded = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_path), Options);
            if (loaded is null || HasInvalidStrings(loaded))
            {
                BackupBrokenFile();
                return new AppConfig();
            }
            return loaded;
        }
        catch (JsonException)
        {
            BackupBrokenFile();
            return new AppConfig();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new AppConfig();
        }
    }

    /// <summary>手編集などで文字列プロパティが null/空になったファイルは「壊れている」とみなす。</summary>
    private static bool HasInvalidStrings(AppConfig c) =>
        string.IsNullOrEmpty(c.FontFamily) ||
        string.IsNullOrEmpty(c.TextColor) ||
        string.IsNullOrEmpty(c.ClockColor) ||
        string.IsNullOrEmpty(c.BackgroundColor);

    private void BackupBrokenFile()
    {
        try
        {
            File.Copy(_path, _path + ".bak", overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // 退避に失敗しても起動は続行する
        }
    }

    public void Save(AppConfig config)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(config, Options));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // 保存失敗で常駐アプリを落とさない。次回の保存で回復する
        }
    }
}
