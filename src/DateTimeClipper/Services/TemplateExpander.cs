using System.Globalization;
using System.Text.RegularExpressions;

namespace DateTimeClipper.Services;

/// <summary>
/// コピー項目テンプレートの展開。{...} 内を日時フォーマットとして展開し、
/// それ以外の文字はそのまま残す。{} を含まない文字列は固定文字列として扱われる。
/// </summary>
public static partial class TemplateExpander
{
    private static readonly CultureInfo JapaneseCI = new("ja-JP");
    private static readonly Calendar JapaneseCal = new JapaneseCalendar();

    public static string Expand(string template, DateTimeOffset dto)
        => PlaceholderRegex().Replace(template, m => Format(m.Groups[1].Value, dto));

    public static string Expand(string template, DateTime dt)
        => Expand(template, ToOffset(dt));

    /// <summary>単一フォーマットの展開。独自キーワードと .NET 標準書式の両方を受け付ける。</summary>
    public static string Format(string format, DateTimeOffset dto)
    {
        return format switch
        {
            "ISO8601" => dto.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            "UnixTime" => dto.ToUnixTimeSeconds().ToString(),
            "UnixTimeMs" => dto.ToUnixTimeMilliseconds().ToString(),
            "Wareki" => FormatWareki(dto.DateTime),
            "WarekiShort" => FormatWarekiShort(dto.DateTime),
            _ => dto.DateTime.ToString(format, CultureInfo.CurrentCulture)
        };
    }

    public static string Format(string format, DateTime dt)
        => Format(format, ToOffset(dt));

    /// <summary>Kind=Utc の DateTime はオフセット0として扱い、それ以外はローカルタイムゾーンのオフセットを付与する。</summary>
    private static DateTimeOffset ToOffset(DateTime dt)
        => dt.Kind == DateTimeKind.Utc
            ? new DateTimeOffset(dt)
            : new DateTimeOffset(dt, TimeZoneInfo.Local.GetUtcOffset(dt));

    private static string FormatWareki(DateTime dt)
    {
        try
        {
            var culture = (CultureInfo)JapaneseCI.Clone();
            culture.DateTimeFormat.Calendar = JapaneseCal;
            return dt.ToString("gy年M月d日", culture);
        }
        catch (ArgumentOutOfRangeException)
        {
            // JapaneseCalendar の範囲外（1868-09-08 より前）は西暦にフォールバック
            return $"{dt.Year}年{dt.Month}月{dt.Day}日";
        }
    }

    private static readonly Dictionary<int, char> EraShortNames = new()
    {
        { 1, 'M' }, { 2, 'T' }, { 3, 'S' }, { 4, 'H' }, { 5, 'R' },
    };

    private static string FormatWarekiShort(DateTime dt)
    {
        try
        {
            int era = JapaneseCal.GetEra(dt);
            int year = JapaneseCal.GetYear(dt);
            var eraChar = EraShortNames.TryGetValue(era, out var c) ? c : '?';
            return $"{eraChar}{year}/{dt.Month}/{dt.Day}";
        }
        catch (ArgumentOutOfRangeException)
        {
            // JapaneseCalendar の範囲外（1868-09-08 より前）は西暦にフォールバック
            return $"{dt.Year}/{dt.Month}/{dt.Day}";
        }
    }

    [GeneratedRegex(@"\{([^}]+)\}")]
    private static partial Regex PlaceholderRegex();
}
