using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DateTimeClipper.Models;
using DateTimeClipper.Services;

namespace DateTimeClipper;

public partial class MainWindow : Window
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExTopmost = 0x00000008;
    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;

    private readonly AppConfig _config;
    private readonly ConfigService _configService;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _saveTimer;
    private SettingsWindow? _settingsWindow;
    private CopyPopup? _popup;
    private bool _sourceInitialized;

    /// <summary>トレイの「終了」からのみ true にする。false の間は Close が非表示になる。</summary>
    internal bool AllowClose { get; set; }

    internal AppConfig Config => _config;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            _sourceInitialized = true;
            ApplyClickThrough();
        };
        _configService = new ConfigService(ConfigService.DefaultPath);
        _config = _configService.Load();
        Left = _config.WindowLeft;
        Top = _config.WindowTop;
        Width = _config.WindowWidth;
        Height = _config.WindowHeight;

        // 保存はデバウンス(スライダードラッグ中の連続書き込みを避ける)。見た目の反映は即時
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            _configService.Save(_config);
        };
        _config.PropertyChanged += (_, _) => OnConfigChanged();
        _config.CopyItems.CollectionChanged += (_, _) => ScheduleSave();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) =>
        {
            UpdateClock();
            // WPFのTopmostは他アプリの最前面ウィンドウ・解像度変更・セッション復帰等で
            // OS側の最前面フラグを失うことがあるため、毎秒監視して失われていたら復帰する
            EnsureTopmost();
        };
        _clockTimer.Start();

        // 画面構成変更・ロック解除/セッション切替の直後は最前面が外れやすいので即復帰
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;

        // リサイズのたびにサイズを保存する。これをしないとトレイの「終了」以外
        // (PC再起動・シャットダウン等)でサイズが保存されず、次回起動時に前回サイズへ戻る
        SizeChanged += (_, _) => PersistWindowSize();

        ApplyConfig();
        UpdateClock();
    }

    /// <summary>現在のウィンドウサイズを設定へ反映する（保存はデバウンス）。</summary>
    private void PersistWindowSize()
    {
        if (double.IsNaN(Width) || double.IsNaN(Height)) return;
        _config.WindowWidth = Width;
        _config.WindowHeight = Height;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(EnsureTopmost);

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e) =>
        Dispatcher.BeginInvoke(EnsureTopmost);

    /// <summary>設定が最前面ONで、かつOSの最前面フラグが失われているときだけ再アサートすべき。</summary>
    public static bool ShouldReassertTopmost(bool configTopmost, long exStyle) =>
        configTopmost && (exStyle & WsExTopmost) == 0;

    /// <summary>最前面が失われていたら、フォーカスを奪わずに最前面へ復帰させる。</summary>
    private void EnsureTopmost()
    {
        if (!_sourceInitialized || !_config.Topmost) return;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        var exStyle = GetWindowLongPtr(hwnd, GwlExstyle).ToInt64();
        if (!ShouldReassertTopmost(_config.Topmost, exStyle)) return;

        SetWindowPos(hwnd, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    private void OnConfigChanged()
    {
        ApplyConfig();
        UpdateClock();
        ScheduleSave();
    }

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    /// <summary>AppConfig の内容を画面に反映する（毎秒の時刻更新以外のすべて）。</summary>
    private void ApplyConfig()
    {
        Topmost = _config.Topmost;
        ApplyClickThrough();

        var bg = ColorUtil.ParseOrDefault(_config.BackgroundColor, Colors.Black);
        // 完全に透明(アルファ0)のピクセルはOSレベルでクリック透過になり
        // ドラッグ移動できなくなるため、不透明度は最低1%を確保する
        RootBorder.Background = new SolidColorBrush(bg)
        {
            Opacity = Math.Max(_config.BackgroundOpacity, 0.01),
        };

        var textBrush = new SolidColorBrush(ColorUtil.ParseOrDefault(_config.TextColor, Colors.White))
        {
            Opacity = _config.TextOpacity,
        };
        DateText.Foreground = textBrush;
        TimeText.Foreground = textBrush;
        DateText.FontFamily = new FontFamily(_config.FontFamily);
        TimeText.FontFamily = new FontFamily(_config.FontFamily);
        DateText.FontSize = _config.FontSize;
        TimeText.FontSize = _config.FontSize;
        RootBorder.ContextMenu.FontSize = _config.FontSize;

        Analog.ClockBrush = new SolidColorBrush(ColorUtil.ParseOrDefault(_config.ClockColor, Colors.White))
        {
            Opacity = _config.ClockOpacity,
        };
        Analog.ShowSeconds = _config.ShowSecondsHand;

        bool showAnalog = _config.DisplayMode != ClockDisplayMode.Digital;
        bool showDigital = _config.DisplayMode != ClockDisplayMode.Analog;
        Analog.Visibility = showAnalog ? Visibility.Visible : Visibility.Collapsed;
        DigitalPanel.Visibility = showDigital ? Visibility.Visible : Visibility.Collapsed;
        AnalogColumn.Width = showAnalog ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        DigitalColumn.Width = showDigital ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        DateText.Visibility = string.IsNullOrWhiteSpace(_config.DateFormat)
            ? Visibility.Collapsed : Visibility.Visible;
        TimeText.Visibility = string.IsNullOrWhiteSpace(_config.TimeFormat)
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyClickThrough()
    {
        if (!_sourceInitialized) return;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        var style = GetWindowLongPtr(hwnd, GwlExstyle);
        var nextStyle = _config.ClickThrough
            ? style.ToInt64() | WsExTransparent
            : style.ToInt64() & ~WsExTransparent;
        if (nextStyle == style.ToInt64()) return;

        SetWindowLongPtr(hwnd, GwlExstyle, new IntPtr(nextStyle));
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        Analog.Time = now;
        DateText.Text = SafeFormat(_config.DateFormat, now);
        TimeText.Text = SafeFormat(_config.TimeFormat, now);
    }

    private static string SafeFormat(string format, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(format)) return "";
        try
        {
            return TemplateExpander.Format(format, now);
        }
        catch (FormatException)
        {
            return "(書式エラー)";
        }
    }

    // ---- クリック／ドラッグ判定 ----

    private void OnBorderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed || IsOnInteractiveElement(e.OriginalSource))
            return;

        // DragMove はボタンが離されるまでブロックする。移動していなければクリックとみなす
        var beforeLeft = Left;
        var beforeTop = Top;
        DragMove();
        bool moved = Math.Abs(Left - beforeLeft) > 3 || Math.Abs(Top - beforeTop) > 3;
        if (moved)
        {
            _config.WindowLeft = Left;
            _config.WindowTop = Top;
        }
        else
        {
            ShowCopyPopup();
        }
    }

    private static bool IsOnInteractiveElement(object source)
    {
        var d = source as DependencyObject;
        while (d != null)
        {
            if (d is Button or ScrollBar) return true;
            d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
        }
        return false;
    }

    private void ShowCopyPopup()
    {
        if (_popup is { IsLoaded: true })
        {
            _popup.Close();
            return;
        }
        _popup = new CopyPopup(_config) { Owner = this };
        _popup.Show();
        // SizeToContent のため実サイズは Show 後に確定する。
        // 主モニタ固定ではなく時計のあるモニタの作業領域を使う（マルチモニタ対応）
        var (left, top) = PlacePopup(Left, Top, Width,
            _popup.ActualWidth, _popup.ActualHeight, CurrentMonitorWorkArea());
        _popup.Left = left;
        _popup.Top = top;
        _popup.Activate();
    }

    /// <summary>時計が今表示されているモニタの作業領域をWPF(DIP)座標で返す。取得不能時は主モニタ。</summary>
    private Rect CurrentMonitorWorkArea()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = PresentationSource.FromVisual(this);
        if (hwnd == IntPtr.Zero || source?.CompositionTarget is null)
            return SystemParameters.WorkArea;

        var wa = System.Windows.Forms.Screen.FromHandle(hwnd).WorkingArea; // 物理px
        var toDip = source.CompositionTarget.TransformFromDevice;          // 物理px→DIP(スケールのみ)
        var topLeft = toDip.Transform(new Point(wa.Left, wa.Top));
        var bottomRight = toDip.Transform(new Point(wa.Right, wa.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    /// <summary>
    /// コピー一覧ポップアップの表示位置を決める。既定は時計の右横、右にはみ出すなら左横。
    /// 縦は時計の上端に合わせ、下（や上）にはみ出す場合は作業領域内へ詰める。
    /// </summary>
    public static (double Left, double Top) PlacePopup(
        double clockLeft, double clockTop, double clockWidth,
        double popupWidth, double popupHeight, Rect workArea)
    {
        const double gap = 8;

        double left = clockLeft + clockWidth + gap;
        if (left + popupWidth > workArea.Right)
            left = clockLeft - popupWidth - gap; // 右に入らなければ左横へ
        left = Math.Clamp(left, workArea.Left,
            Math.Max(workArea.Left, workArea.Right - popupWidth));

        double top = clockTop;
        if (top + popupHeight > workArea.Bottom)
            top = workArea.Bottom - popupHeight; // 下にはみ出すなら上へ詰める
        top = Math.Max(top, workArea.Top);

        return (left, top);
    }

    // ---- メニュー・ボタン ----

    internal void OpenSettings()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(_config) { Owner = this };
        // 本体の右横に表示。時計のあるモニタの右端からはみ出す場合は左横に出す
        var wa = CurrentMonitorWorkArea();
        _settingsWindow.Left = Left + Width + 8;
        _settingsWindow.Top = Top;
        if (_settingsWindow.Left + _settingsWindow.Width > wa.Right)
        {
            _settingsWindow.Left = Left - _settingsWindow.Width - 8;
        }
        _settingsWindow.Show();
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e) => OpenSettings();

    private void OnHideClick(object sender, RoutedEventArgs e) => Hide();

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        AllowClose = true;
        Close();
        Application.Current.Shutdown();
    }

    private void OnMouseEnterWindow(object sender, MouseEventArgs e) => AnimateButtons(1);

    private void OnMouseLeaveWindow(object sender, MouseEventArgs e) => AnimateButtons(0);

    private void AnimateButtons(double to) =>
        HoverButtons.BeginAnimation(OpacityProperty,
            new DoubleAnimation(to, TimeSpan.FromMilliseconds(150)));

    // ---- 常駐動作 ----

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            // Alt+F4 等では終了せず、トレイ常駐のまま隠れる
            e.Cancel = true;
            Hide();
            return;
        }
        _saveTimer.Stop();
        _clockTimer.Stop();
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        _config.WindowLeft = Left;
        _config.WindowTop = Top;
        _config.WindowWidth = Width;
        _config.WindowHeight = Height;
        _configService.Save(_config);
        base.OnClosing(e);
    }

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) =>
        IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : new IntPtr(GetWindowLong32(hWnd, nIndex));

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) =>
        IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
}
