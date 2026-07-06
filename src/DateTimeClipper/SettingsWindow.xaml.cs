using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DateTimeClipper.Models;
using DateTimeClipper.Services;

namespace DateTimeClipper;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;
    private readonly bool _loading;

    // WPFのバインディング（DisplayMemberBinding）はリフレクションで解決するため public にする
    public sealed record FormatRow(string Format, string Example);

    /// <summary>早見表に載せるフォーマット。上段=よく使う複合書式、下段=単体要素と独自キーワード。</summary>
    private static readonly string[] FormatCatalog =
    {
        "yyyy/MM/dd", "yyyy-MM-dd", "yyyyMMdd", "yyyy/MM/dd (ddd)",
        "HH:mm:ss", "HH:mm", "HHmmss", "yyyyMMdd_HHmmss",
        "yyyy", "yy", "MM", "dd", "ddd", "dddd", "HH", "mm", "ss",
        "ISO8601", "UnixTime", "UnixTimeMs", "Wareki", "WarekiShort",
    };

    public SettingsWindow(AppConfig config)
    {
        InitializeComponent();
        _config = config;
        _loading = true;

        // 時計タブ
        var modeButton = config.DisplayMode switch
        {
            ClockDisplayMode.Analog => ModeAnalog,
            ClockDisplayMode.Digital => ModeDigital,
            _ => ModeBoth,
        };
        modeButton.IsChecked = true;
        DateFormatBox.Text = config.DateFormat;
        TimeFormatBox.Text = config.TimeFormat;
        SecondsCheck.IsChecked = config.ShowSecondsHand;
        TopmostCheck.IsChecked = config.Topmost;

        // 外観タブ
        FontBox.ItemsSource = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(s => s).ToList();
        FontBox.SelectedItem = config.FontFamily;
        SizeSlider.Value = config.FontSize;
        TextColorBox.Text = config.TextColor;
        ClockColorBox.Text = config.ClockColor;
        BgColorBox.Text = config.BackgroundColor;
        TextOpacitySlider.Value = config.TextOpacity;
        ClockOpacitySlider.Value = config.ClockOpacity;
        BgOpacitySlider.Value = config.BackgroundOpacity;

        // コピー項目タブ
        ItemsListBox.ItemsSource = config.CopyItems;
        var now = DateTime.Now;
        FormatList.ItemsSource = FormatCatalog
            .Select(f => new FormatRow(f, SafeFormat(f, now)))
            .ToList();

        _loading = false;
        RefreshDerivedLabels();
    }

    private static string SafeFormat(string format, DateTime now)
    {
        try
        {
            return TemplateExpander.Format(format, now);
        }
        catch (FormatException)
        {
            return "(書式エラー)";
        }
    }

    private static string SafeExpand(string template, DateTime now)
    {
        try
        {
            return TemplateExpander.Expand(template, now);
        }
        catch (FormatException)
        {
            return "(書式エラー)";
        }
    }

    /// <summary>スライダー値ラベルや各プレビューなど、設定値から導出される表示をまとめて更新する。</summary>
    private void RefreshDerivedLabels()
    {
        SizeLabel.Text = ((int)SizeSlider.Value).ToString();
        TextOpacityLabel.Text = TextOpacitySlider.Value.ToString("0.00");
        ClockOpacityLabel.Text = ClockOpacitySlider.Value.ToString("0.00");
        BgOpacityLabel.Text = BgOpacitySlider.Value.ToString("0.00");
        TextColorPreview.Background = new SolidColorBrush(ColorUtil.ParseOrDefault(TextColorBox.Text, Colors.Transparent));
        ClockColorPreview.Background = new SolidColorBrush(ColorUtil.ParseOrDefault(ClockColorBox.Text, Colors.Transparent));
        BgColorPreview.Background = new SolidColorBrush(ColorUtil.ParseOrDefault(BgColorBox.Text, Colors.Transparent));
        var now = DateTime.Now;
        DigitalPreview.Text = "プレビュー: "
            + (string.IsNullOrWhiteSpace(DateFormatBox.Text) ? "" : SafeFormat(DateFormatBox.Text, now) + "  ")
            + (string.IsNullOrWhiteSpace(TimeFormatBox.Text) ? "" : SafeFormat(TimeFormatBox.Text, now));
        PreviewText.Text = ItemsListBox.SelectedItem is string t
            ? "プレビュー: " + SafeExpand(t, now)
            : "";
    }

    // ---- 時計タブ ----

    private void OnModeChecked(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _config.DisplayMode =
            ModeAnalog.IsChecked == true ? ClockDisplayMode.Analog :
            ModeDigital.IsChecked == true ? ClockDisplayMode.Digital :
            ClockDisplayMode.Both;
    }

    private void OnDateFormatChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        _config.DateFormat = DateFormatBox.Text;
        RefreshDerivedLabels();
    }

    private void OnTimeFormatChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        _config.TimeFormat = TimeFormatBox.Text;
        RefreshDerivedLabels();
    }

    private void OnSecondsChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _config.ShowSecondsHand = SecondsCheck.IsChecked == true;
    }

    private void OnTopmostChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _config.Topmost = TopmostCheck.IsChecked == true;
    }

    // ---- 外観タブ ----

    private void ApplyPreset(Action<AppConfig> apply)
    {
        apply(_config);
        // プリセットが変えた値をUIに反映（イベントループは _config 側が同値ならSetで抑止される）
        TextColorBox.Text = _config.TextColor;
        ClockColorBox.Text = _config.ClockColor;
        BgColorBox.Text = _config.BackgroundColor;
        TextOpacitySlider.Value = _config.TextOpacity;
        ClockOpacitySlider.Value = _config.ClockOpacity;
        BgOpacitySlider.Value = _config.BackgroundOpacity;
        RefreshDerivedLabels();
    }

    private void OnPresetDark(object sender, RoutedEventArgs e) => ApplyPreset(ThemePresets.ApplyDark);
    private void OnPresetLight(object sender, RoutedEventArgs e) => ApplyPreset(ThemePresets.ApplyLight);
    private void OnPresetTranslucent(object sender, RoutedEventArgs e) => ApplyPreset(ThemePresets.ApplyTranslucentDark);
    private void OnPresetTransparent(object sender, RoutedEventArgs e) => ApplyPreset(ThemePresets.ApplyTransparent);

    private void OnFontChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || FontBox.SelectedItem is not string font) return;
        _config.FontFamily = font;
    }

    private void OnSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;
        _config.FontSize = SizeSlider.Value;
        RefreshDerivedLabels();
    }

    private void OnColorChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        // 不正な色文字列は反映しない（入力途中の "#F0" などで画面が壊れないように）
        if (IsValidColor(TextColorBox.Text)) _config.TextColor = TextColorBox.Text;
        if (IsValidColor(ClockColorBox.Text)) _config.ClockColor = ClockColorBox.Text;
        if (IsValidColor(BgColorBox.Text)) _config.BackgroundColor = BgColorBox.Text;
        RefreshDerivedLabels();
    }

    private static bool IsValidColor(string hex)
    {
        try
        {
            ColorConverter.ConvertFromString(hex);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or NullReferenceException)
        {
            return false;
        }
    }

    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;
        _config.TextOpacity = TextOpacitySlider.Value;
        _config.ClockOpacity = ClockOpacitySlider.Value;
        _config.BackgroundOpacity = BgOpacitySlider.Value;
        RefreshDerivedLabels();
    }

    // ---- コピー項目タブ ----

    private void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ItemsListBox.SelectedItem is string item)
        {
            EditBox.Text = item;
        }
        RefreshDerivedLabels();
    }

    private void OnEditChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        int i = ItemsListBox.SelectedIndex;
        if (i < 0 || _config.CopyItems[i] == EditBox.Text) return;
        _config.CopyItems[i] = EditBox.Text;
        // ObservableCollectionの置換で選択が外れるため復元する
        ItemsListBox.SelectedIndex = i;
        RefreshDerivedLabels();
    }

    private void OnAddItem(object sender, RoutedEventArgs e)
    {
        _config.CopyItems.Add("{yyyy/MM/dd}");
        ItemsListBox.SelectedIndex = _config.CopyItems.Count - 1;
        EditBox.Focus();
        EditBox.SelectAll();
    }

    private void OnDeleteItem(object sender, RoutedEventArgs e)
    {
        int i = ItemsListBox.SelectedIndex;
        if (i < 0) return;
        _config.CopyItems.RemoveAt(i);
        ItemsListBox.SelectedIndex = Math.Min(i, _config.CopyItems.Count - 1);
    }

    private void OnMoveUp(object sender, RoutedEventArgs e) => MoveItem(-1);

    private void OnMoveDown(object sender, RoutedEventArgs e) => MoveItem(+1);

    private void MoveItem(int delta)
    {
        int i = ItemsListBox.SelectedIndex;
        int j = i + delta;
        if (i < 0 || j < 0 || j >= _config.CopyItems.Count) return;
        _config.CopyItems.Move(i, j);
        ItemsListBox.SelectedIndex = j;
    }

    private void OnFormatListClick(object sender, MouseButtonEventArgs e)
    {
        if (FormatList.SelectedItem is not FormatRow row) return;
        if (ItemsListBox.SelectedIndex < 0) return;
        var insert = "{" + row.Format + "}";
        int caret = EditBox.CaretIndex;
        EditBox.Text = EditBox.Text.Insert(caret, insert);
        EditBox.CaretIndex = caret + insert.Length;
        EditBox.Focus();
    }
}
