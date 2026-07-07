using System.Windows;
using DateTimeClipper;
using Xunit;

namespace DateTimeClipper.Tests;

public class MainWindowTests
{
    private const int WsExTopmost = 0x00000008;
    private const int WsExTransparent = 0x00000020;

    // 作業領域: 1920x1080 のうち下40pxがタスクバー → 下端は1040
    private static readonly Rect WorkArea = new(0, 0, 1920, 1040);

    [Fact]
    public void ポップアップは通常は時計の右横に出る()
    {
        var (left, top) = MainWindow.PlacePopup(
            clockLeft: 100, clockTop: 100, clockWidth: 300,
            popupWidth: 200, popupHeight: 300, workArea: WorkArea);
        Assert.Equal(408, left); // 100 + 300 + 8
        Assert.Equal(100, top);
    }

    [Fact]
    public void 右にはみ出す場合は時計の左横に出る()
    {
        var (left, _) = MainWindow.PlacePopup(
            clockLeft: 1700, clockTop: 100, clockWidth: 200,
            popupWidth: 300, popupHeight: 300, workArea: WorkArea);
        Assert.Equal(1392, left); // 1700 - 300 - 8
    }

    [Fact]
    public void 下にはみ出す場合は上に詰めて作業領域内に収める()
    {
        var (_, top) = MainWindow.PlacePopup(
            clockLeft: 100, clockTop: 900, clockWidth: 300,
            popupWidth: 200, popupHeight: 300, workArea: WorkArea);
        Assert.Equal(740, top); // 1040 - 300 なので下端がちょうど作業領域下端
    }

    [Fact]
    public void 右下の角でも縦横ともに作業領域内に収まる()
    {
        var (left, top) = MainWindow.PlacePopup(
            clockLeft: 1700, clockTop: 900, clockWidth: 200,
            popupWidth: 300, popupHeight: 300, workArea: WorkArea);
        Assert.Equal(1392, left);
        Assert.Equal(740, top);
        Assert.True(left + 300 <= WorkArea.Right);
        Assert.True(top + 300 <= WorkArea.Bottom);
    }

    [Fact]
    public void 最前面OFFなら再アサートしない()
    {
        Assert.False(MainWindow.ShouldReassertTopmost(configTopmost: false, exStyle: 0));
        Assert.False(MainWindow.ShouldReassertTopmost(configTopmost: false, exStyle: WsExTopmost));
    }

    [Fact]
    public void 最前面ONでOSのTOPMOSTフラグが健在なら再アサートしない()
    {
        Assert.False(MainWindow.ShouldReassertTopmost(configTopmost: true, exStyle: WsExTopmost));
        // 他の拡張スタイルが立っていてもTOPMOSTがあれば復帰不要
        Assert.False(MainWindow.ShouldReassertTopmost(configTopmost: true, exStyle: WsExTopmost | WsExTransparent));
    }

    [Fact]
    public void 最前面ONでOSのTOPMOSTフラグが失われていたら再アサートする()
    {
        Assert.True(MainWindow.ShouldReassertTopmost(configTopmost: true, exStyle: 0));
        Assert.True(MainWindow.ShouldReassertTopmost(configTopmost: true, exStyle: WsExTransparent));
    }
}
