using System.Windows;
using System.Windows.Media;

namespace DateTimeClipper.Controls;

/// <summary>
/// アナログ時計。ClockBrush（色＋不透明度）1色で外周・目盛・針を描く。
/// 利用側は Time を毎秒更新するだけでよい（AffectsRenderで自動再描画）。
/// </summary>
public class AnalogClock : FrameworkElement
{
    public static readonly DependencyProperty TimeProperty =
        DependencyProperty.Register(nameof(Time), typeof(DateTime), typeof(AnalogClock),
            new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ClockBrushProperty =
        DependencyProperty.Register(nameof(ClockBrush), typeof(Brush), typeof(AnalogClock),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowSecondsProperty =
        DependencyProperty.Register(nameof(ShowSeconds), typeof(bool), typeof(AnalogClock),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public DateTime Time
    {
        get => (DateTime)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public Brush ClockBrush
    {
        get => (Brush)GetValue(ClockBrushProperty);
        set => SetValue(ClockBrushProperty, value);
    }

    public bool ShowSeconds
    {
        get => (bool)GetValue(ShowSecondsProperty);
        set => SetValue(ShowSecondsProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double size = Math.Min(ActualWidth, ActualHeight);
        if (size < 10) return;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        double r = size / 2 - 2;
        // 線の太さはサイズ比例（直径100pxのとき外周2px）
        double scale = size / 100.0;

        var rimPen = new Pen(ClockBrush, 2 * scale);
        dc.DrawEllipse(null, rimPen, center, r, r);

        var tickPen = new Pen(ClockBrush, 1.5 * scale);
        for (int i = 0; i < 12; i++)
        {
            double angle = i * Math.PI / 6;
            // 3,6,9,12時は長め
            double inner = (i % 3 == 0) ? r * 0.82 : r * 0.90;
            dc.DrawLine(tickPen, PointOnDial(center, inner, angle), PointOnDial(center, r * 0.96, angle));
        }

        var t = Time;
        double hourAngle = (t.Hour % 12 + t.Minute / 60.0) * Math.PI / 6;
        double minuteAngle = (t.Minute + t.Second / 60.0) * Math.PI / 30;

        dc.DrawLine(RoundPen(3.5 * scale), center, PointOnDial(center, r * 0.50, hourAngle));
        dc.DrawLine(RoundPen(2.5 * scale), center, PointOnDial(center, r * 0.74, minuteAngle));

        if (ShowSeconds)
        {
            double secondAngle = t.Second * Math.PI / 30;
            dc.DrawLine(RoundPen(1.0 * scale), center, PointOnDial(center, r * 0.86, secondAngle));
        }

        dc.DrawEllipse(ClockBrush, null, center, 2.5 * scale, 2.5 * scale);
    }

    private Pen RoundPen(double thickness) => new(ClockBrush, thickness)
    {
        StartLineCap = PenLineCap.Round,
        EndLineCap = PenLineCap.Round,
    };

    /// <summary>12時方向を0として時計回りの角度（ラジアン）から文字盤上の座標を得る。</summary>
    private static Point PointOnDial(Point center, double radius, double angle)
        => new(center.X + radius * Math.Sin(angle), center.Y - radius * Math.Cos(angle));
}
