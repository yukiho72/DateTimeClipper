using Xunit;
using DateTimeClipper.Services;

namespace DateTimeClipper.Tests;

public class TemplateExpanderTests
{
    // 2026-07-06 10:08:45 JST（月曜日）
    private static readonly DateTimeOffset TestDate =
        new(2026, 7, 6, 10, 8, 45, TimeSpan.FromHours(9));

    [Fact]
    public void Expand_日付フォーマットのみ()
        => Assert.Equal("2026/07/06", TemplateExpander.Expand("{yyyy/MM/dd}", TestDate));

    [Fact]
    public void Expand_固定文字列はそのまま()
        => Assert.Equal("本日", TemplateExpander.Expand("本日", TestDate));

    [Fact]
    public void Expand_固定文字列と日時の混在()
        => Assert.Equal("backup_20260706_100845.zip",
            TemplateExpander.Expand("backup_{yyyyMMdd_HHmmss}.zip", TestDate));

    [Fact]
    public void Expand_複数プレースホルダー()
        => Assert.Equal("2026-07-06 10:08",
            TemplateExpander.Expand("{yyyy-MM-dd} {HH:mm}", TestDate));

    [Fact]
    public void Format_ISO8601()
        => Assert.Equal("2026-07-06T10:08:45+09:00", TemplateExpander.Format("ISO8601", TestDate));

    [Fact]
    public void Format_UnixTime()
        => Assert.Equal(TestDate.ToUnixTimeSeconds().ToString(),
            TemplateExpander.Format("UnixTime", TestDate));

    [Fact]
    public void Format_UnixTimeMs()
        => Assert.Equal(TestDate.ToUnixTimeMilliseconds().ToString(),
            TemplateExpander.Format("UnixTimeMs", TestDate));

    [Fact]
    public void Format_Wareki()
        => Assert.Equal("令和8年7月6日", TemplateExpander.Format("Wareki", TestDate));

    [Fact]
    public void Format_WarekiShort()
        => Assert.Equal("R8/7/6", TemplateExpander.Format("WarekiShort", TestDate));

    [Fact]
    public void Expand_和暦キーワードをテンプレート内で使える()
        => Assert.Equal("令和8年7月6日の議事録",
            TemplateExpander.Expand("{Wareki}の議事録", TestDate));

    [Fact]
    public void Format_KindがUtcのDateTimeでも例外にならない()
    {
        var utc = DateTime.SpecifyKind(new DateTime(2026, 7, 6, 1, 8, 45), DateTimeKind.Utc);
        Assert.Equal("20260706", TemplateExpander.Format("yyyyMMdd", utc));
    }

    [Fact]
    public void Format_Wareki_和暦範囲外は西暦フォールバック()
        => Assert.Equal("1800年1月1日",
            TemplateExpander.Format("Wareki", new DateTimeOffset(1800, 1, 1, 0, 0, 0, TimeSpan.FromHours(9))));

    [Fact]
    public void Format_WarekiShort_和暦範囲外は西暦フォールバック()
        => Assert.Equal("1800/1/1",
            TemplateExpander.Format("WarekiShort", new DateTimeOffset(1800, 1, 1, 0, 0, 0, TimeSpan.FromHours(9))));
}
