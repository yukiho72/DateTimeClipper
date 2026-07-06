using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DateTimeClipper.Models;
using DateTimeClipper.Services;

namespace DateTimeClipper;

public partial class CopyPopup : Window
{
    private readonly DispatcherTimer _closeTimer;
    private bool _copying;

    public CopyPopup(AppConfig config)
    {
        InitializeComponent();
        var now = DateTime.Now;
        ItemList.ItemsSource = config.CopyItems
            .Select(t => TemplateExpander.Expand(t, now))
            .ToList();

        // 外観はメインウィンドウの設定に追従（背景は視認性のため最低30%）
        var bg = ColorUtil.ParseOrDefault(config.BackgroundColor, Colors.Black);
        RootBorder.Background = new SolidColorBrush(bg)
        {
            Opacity = Math.Max(config.BackgroundOpacity, 0.3),
        };
        var fg = new SolidColorBrush(ColorUtil.ParseOrDefault(config.TextColor, Colors.White))
        {
            Opacity = Math.Max(config.TextOpacity, 0.3),
        };
        ItemList.Foreground = fg;
        FontFamily = new FontFamily(config.FontFamily);
        FontSize = config.FontSize;

        _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
        _closeTimer.Tick += (_, _) => { _closeTimer.Stop(); Close(); };
    }

    private void OnItemClick(object sender, RoutedEventArgs e)
    {
        if (_copying) return;
        var text = (string)((Button)sender).Tag;
        if (!TrySetClipboard(text))
        {
            StatusText.Text = "コピーに失敗しました";
        }
        _copying = true;
        StatusOverlay.Visibility = Visibility.Visible;
        _closeTimer.Start();
    }

    /// <summary>他プロセスがクリップボードをロックしていると失敗するため少しリトライする。</summary>
    private static bool TrySetClipboard(string text)
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                Clipboard.SetDataObject(text, copy: true);
                return true;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                Thread.Sleep(50);
            }
        }
        return false;
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        // コピー直後の自動クローズ待ち中はタイマーに任せる
        if (!_copying) Close();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }
}
