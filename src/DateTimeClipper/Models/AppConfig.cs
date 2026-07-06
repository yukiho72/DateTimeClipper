using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DateTimeClipper.Models;

[JsonConverter(typeof(JsonStringEnumConverter<ClockDisplayMode>))]
public enum ClockDisplayMode
{
    Both,     // アナログ＋デジタル（横並び）
    Analog,   // アナログのみ
    Digital,  // デジタルのみ
}

/// <summary>アプリの全設定。デフォルト値は「半透明ダーク」プリセット相当。</summary>
public class AppConfig : INotifyPropertyChanged
{
    private ClockDisplayMode _displayMode = ClockDisplayMode.Both;
    private string _dateFormat = "yyyy/MM/dd (ddd)";
    private string _timeFormat = "HH:mm:ss";
    private bool _showSecondsHand = true;
    private bool _topmost = true;
    private string _fontFamily = "Yu Gothic UI";
    private double _fontSize = 14;
    private string _textColor = "#F0F0F0";
    private double _textOpacity = 1.0;
    private string _clockColor = "#F0F0F0";
    private double _clockOpacity = 1.0;
    private string _backgroundColor = "#141419";
    private double _backgroundOpacity = 0.55;
    private double _windowLeft = 100;
    private double _windowTop = 100;
    private double _windowWidth = 300;
    private double _windowHeight = 130;

    public ClockDisplayMode DisplayMode { get => _displayMode; set => Set(ref _displayMode, value); }
    /// <summary>デジタル1行目（日付行）。空なら行を非表示。</summary>
    public string DateFormat { get => _dateFormat; set => Set(ref _dateFormat, value); }
    /// <summary>デジタル2行目（時刻行）。空なら行を非表示。</summary>
    public string TimeFormat { get => _timeFormat; set => Set(ref _timeFormat, value); }
    public bool ShowSecondsHand { get => _showSecondsHand; set => Set(ref _showSecondsHand, value); }
    public bool Topmost { get => _topmost; set => Set(ref _topmost, value); }
    public string FontFamily { get => _fontFamily; set => Set(ref _fontFamily, value); }
    public double FontSize { get => _fontSize; set => Set(ref _fontSize, value); }
    public string TextColor { get => _textColor; set => Set(ref _textColor, value); }
    public double TextOpacity { get => _textOpacity; set => Set(ref _textOpacity, value); }
    public string ClockColor { get => _clockColor; set => Set(ref _clockColor, value); }
    public double ClockOpacity { get => _clockOpacity; set => Set(ref _clockOpacity, value); }
    public string BackgroundColor { get => _backgroundColor; set => Set(ref _backgroundColor, value); }
    public double BackgroundOpacity { get => _backgroundOpacity; set => Set(ref _backgroundOpacity, value); }
    public double WindowLeft { get => _windowLeft; set => Set(ref _windowLeft, value); }
    public double WindowTop { get => _windowTop; set => Set(ref _windowTop, value); }
    public double WindowWidth { get => _windowWidth; set => Set(ref _windowWidth, value); }
    public double WindowHeight { get => _windowHeight; set => Set(ref _windowHeight, value); }

    /// <summary>コピー項目（template 文字列）。UIバインド用に ObservableCollection。</summary>
    public ObservableCollection<string> CopyItems { get; set; } = new()
    {
        "{yyyy/MM/dd}",
        "{yyyyMMdd}",
        "{yyyy/MM/dd HH:mm:ss}",
        "{Wareki}",
        "backup_{yyyyMMdd_HHmmss}.zip",
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
