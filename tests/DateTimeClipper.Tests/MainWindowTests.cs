using DateTimeClipper;
using Xunit;

namespace DateTimeClipper.Tests;

public class MainWindowTests
{
    private const int WsExTopmost = 0x00000008;
    private const int WsExTransparent = 0x00000020;

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
