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
        var clickThroughItem = new System.Windows.Forms.ToolStripMenuItem("クリックを透過する(&P)")
        {
            CheckOnClick = true,
            Checked = main.Config.ClickThrough,
        };
        clickThroughItem.Click += (_, _) =>
        {
            main.Config.ClickThrough = clickThroughItem.Checked;
        };
        menu.Items.Add(clickThroughItem);
        menu.Opening += (_, _) =>
        {
            clickThroughItem.Checked = main.Config.ClickThrough;
        };
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
            // 時計の色は memo3 のアイコンに合わせた黄色(#FFFF00)
            using var rimPen = new System.Drawing.Pen(System.Drawing.Color.Yellow, 2.5f);
            using var handPen = new System.Drawing.Pen(System.Drawing.Color.Yellow, 1.5f);
            g.DrawEllipse(rimPen, 2, 2, 12, 12);
            g.DrawLine(handPen, 8, 8, 8, 4);    // 分針（12時方向）
            g.DrawLine(handPen, 8, 8, 11, 8);   // 時針（3時方向）
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
