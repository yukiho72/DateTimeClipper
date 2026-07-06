using System.Windows;

namespace DateTimeClipper;

public partial class App : Application
{
    private Mutex? _mutex;
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(initiallyOwned: true, "DateTimeClipper_SingleInstance", out var createdNew);
        if (!createdNew)
        {
            // 既に起動中なら2つ目は黙って終了する
            Shutdown();
            return;
        }

        base.OnStartup(e);
        var main = new MainWindow();
        main.Show();
        SetupTrayIcon(main);
    }

    private void SetupTrayIcon(MainWindow main)
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = CreateClockIcon(),
            Text = "DateTimeClipper",
            Visible = true,
        };
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("表示/非表示(&T)", null, (_, _) => ToggleMain(main));
        menu.Items.Add("設定(&S)", null, (_, _) =>
        {
            main.Show();
            main.OpenSettings();
        });
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("終了(&X)", null, (_, _) =>
        {
            main.AllowClose = true;
            main.Close();
            Shutdown();
        });
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.MouseClick += (_, me) =>
        {
            if (me.Button == System.Windows.Forms.MouseButtons.Left) ToggleMain(main);
        };
    }

    private static void ToggleMain(MainWindow main)
    {
        if (main.IsVisible) main.Hide();
        else main.Show();
    }

    /// <summary>16x16の簡易時計アイコンを実行時生成する（icoファイル不要）。</summary>
    private static System.Drawing.Icon CreateClockIcon()
    {
        var bmp = new System.Drawing.Bitmap(16, 16);
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.FromArgb(30, 30, 40));
            using var pen = new System.Drawing.Pen(System.Drawing.Color.White, 1.5f);
            g.DrawEllipse(pen, 1, 1, 13, 13);
            g.DrawLine(pen, 8, 8, 8, 4);    // 分針（12時方向）
            g.DrawLine(pen, 8, 8, 11, 8);   // 時針（3時方向）
        }
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_trayIcon != null) _trayIcon.Visible = false;
        _trayIcon?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
