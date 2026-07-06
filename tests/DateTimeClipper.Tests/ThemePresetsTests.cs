using Xunit;
using DateTimeClipper.Models;

namespace DateTimeClipper.Tests;

public class ThemePresetsTests
{
    [Fact]
    public void ダークは不透明()
    {
        var c = new AppConfig();
        ThemePresets.ApplyDark(c);
        Assert.Equal(1.0, c.BackgroundOpacity);
        Assert.Equal("#1E1E1E", c.BackgroundColor);
    }

    [Fact]
    public void 半透明ダークは背景が半透明()
    {
        var c = new AppConfig();
        ThemePresets.ApplyTranslucentDark(c);
        Assert.Equal(0.55, c.BackgroundOpacity);
    }

    [Fact]
    public void 完全透明は背景不透明度ゼロ()
    {
        var c = new AppConfig();
        ThemePresets.ApplyTransparent(c);
        Assert.Equal(0.0, c.BackgroundOpacity);
    }

    [Fact]
    public void プリセットはフォント設定に触らない()
    {
        var c = new AppConfig { FontFamily = "Meiryo", FontSize = 30 };
        ThemePresets.ApplyLight(c);
        Assert.Equal("Meiryo", c.FontFamily);
        Assert.Equal(30, c.FontSize);
    }

    [Fact]
    public void プリセットは文字と時計の不透明度を全開に戻す()
    {
        var c = new AppConfig { TextOpacity = 0.2, ClockOpacity = 0.2 };
        ThemePresets.ApplyDark(c);
        Assert.Equal(1.0, c.TextOpacity);
        Assert.Equal(1.0, c.ClockOpacity);
    }
}
