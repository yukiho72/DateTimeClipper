# DateTimeClipper

アナログ/デジタル時計を表示する Windows 常駐アプリ。
時計をクリックすると日付・定型文の一覧が開き、選択した文字列をクリップボードにコピーする。

## 機能

- アナログ / デジタル / 両方（横並び）の表示切替
- デジタルは日付行・時刻行のフォーマットを自由に設定（空欄で行を非表示）
- 時計クリックでコピー一覧をポップアップ表示。項目クリックでコピー
- コピー項目は `{...}` テンプレート1種類（例: `{yyyy/MM/dd}`、`本日`、`backup_{yyyyMMdd}.zip`）
- 独自キーワード: `ISO8601` `UnixTime` `UnixTimeMs` `Wareki` `WarekiShort`
- 設定画面にフォーマット早見表（クリックで編集欄に挿入）
- 枠なしウィンドウをどこを掴んでもドラッグ移動、端のドラッグでサイズ変更
- 文字色・時計色・背景色と、それぞれの不透明度をスライダーでリアルタイム調整
- テーマプリセット4種（ダーク / ライト / 半透明ダーク / 完全透明）
- タスクトレイ常駐（表示/非表示・設定・終了）、多重起動防止
- 設定は `%APPDATA%\DateTimeClipper\config.json` に自動保存

## ビルドと実行

```powershell
dotnet build -c Release
.\src\DateTimeClipper\bin\Release\net8.0-windows\DateTimeClipper.exe
```

## テスト

```powershell
dotnet test
```

## 自動起動したい場合

`Win+R` → `shell:startup` で開くフォルダに exe のショートカットを置く。
