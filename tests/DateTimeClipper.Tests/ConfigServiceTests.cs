using Xunit;
using DateTimeClipper.Models;
using DateTimeClipper.Services;

namespace DateTimeClipper.Tests;

public class ConfigServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "DateTimeClipperTests_" + Guid.NewGuid());
    private readonly string _path;

    public ConfigServiceTests()
    {
        Directory.CreateDirectory(_dir);
        _path = Path.Combine(_dir, "config.json");
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void ファイルがなければデフォルト設定を返す()
    {
        var config = new ConfigService(_path).Load();
        Assert.Equal(ClockDisplayMode.Both, config.DisplayMode);
        Assert.Equal(5, config.CopyItems.Count);
    }

    [Fact]
    public void 保存して読み込むと同じ値になる()
    {
        var service = new ConfigService(_path);
        var config = new AppConfig
        {
            DisplayMode = ClockDisplayMode.Analog,
            ClickThrough = true,
            FontSize = 22,
            ClockOpacity = 0.4,
        };
        config.CopyItems.Add("{HH:mm}");
        service.Save(config);

        var loaded = service.Load();
        Assert.Equal(ClockDisplayMode.Analog, loaded.DisplayMode);
        Assert.True(loaded.ClickThrough);
        Assert.Equal(22, loaded.FontSize);
        Assert.Equal(0.4, loaded.ClockOpacity);
        Assert.Contains("{HH:mm}", loaded.CopyItems);
    }

    [Fact]
    public void 壊れたJSONはデフォルトに戻しbakに退避する()
    {
        File.WriteAllText(_path, "{ これはJSONではない");
        var config = new ConfigService(_path).Load();
        Assert.Equal(ClockDisplayMode.Both, config.DisplayMode);
        Assert.True(File.Exists(_path + ".bak"));
        Assert.Equal("{ これはJSONではない", File.ReadAllText(_path + ".bak"));
    }

    [Fact]
    public void 文字列プロパティが空のファイルは壊れているとみなす()
    {
        File.WriteAllText(_path, """{"fontFamily": ""}""");
        var config = new ConfigService(_path).Load();
        Assert.Equal("Yu Gothic UI", config.FontFamily);
        Assert.True(File.Exists(_path + ".bak"));
    }

    [Fact]
    public void ディレクトリ部分のないパスでもSaveは例外を投げない()
    {
        const string bareName = "config.json";
        try
        {
            // 例外にならないことだけが検証対象
            new ConfigService(bareName).Save(new AppConfig());
        }
        finally
        {
            // カレントディレクトリに書けてしまった場合の後始末
            if (File.Exists(bareName)) File.Delete(bareName);
        }
    }
}
