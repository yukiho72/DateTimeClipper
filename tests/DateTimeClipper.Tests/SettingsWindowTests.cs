using DateTimeClipper.Models;
using Xunit;

namespace DateTimeClipper.Tests;

public class SettingsWindowTests
{
    /// <summary>WPFのWindow生成はSTAスレッドを要するため、専用スレッドで実行して例外を回収する。</summary>
    private static Exception? RunOnStaThread(Action action)
    {
        Exception? captured = null;
        var t = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { captured = ex; }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        return captured;
    }

    [Fact]
    public void 設定画面はコンストラクタで例外を投げずに生成できる()
    {
        // 回帰: SizeSlider(Minimum=8) の初期補正で InitializeComponent 中に
        // OnSizeChanged が発火し、_config/_loading 未初期化により NRE で落ちていた
        var ex = RunOnStaThread(() =>
        {
            var window = new SettingsWindow(new AppConfig());
            window.Close();
        });
        Assert.Null(ex);
    }
}
