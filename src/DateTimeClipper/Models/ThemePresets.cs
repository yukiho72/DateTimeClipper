namespace DateTimeClipper.Models;

/// <summary>色・不透明度だけを変えるプリセット。フォント・レイアウト設定には触らない。</summary>
public static class ThemePresets
{
    public static void ApplyDark(AppConfig c)
    {
        c.BackgroundColor = "#1E1E1E";
        c.TextColor = "#E8E8E8";
        c.ClockColor = "#E8E8E8";
        c.BackgroundOpacity = 1.0;
        c.TextOpacity = 1.0;
        c.ClockOpacity = 1.0;
    }

    public static void ApplyLight(AppConfig c)
    {
        c.BackgroundColor = "#FAFAFA";
        c.TextColor = "#222222";
        c.ClockColor = "#222222";
        c.BackgroundOpacity = 1.0;
        c.TextOpacity = 1.0;
        c.ClockOpacity = 1.0;
    }

    public static void ApplyTranslucentDark(AppConfig c)
    {
        c.BackgroundColor = "#141419";
        c.TextColor = "#F0F0F0";
        c.ClockColor = "#F0F0F0";
        c.BackgroundOpacity = 0.55;
        c.TextOpacity = 1.0;
        c.ClockOpacity = 1.0;
    }

    public static void ApplyTransparent(AppConfig c)
    {
        c.BackgroundColor = "#000000";
        c.TextColor = "#FFFFFF";
        c.ClockColor = "#FFFFFF";
        c.BackgroundOpacity = 0.0;
        c.TextOpacity = 1.0;
        c.ClockOpacity = 1.0;
    }
}
