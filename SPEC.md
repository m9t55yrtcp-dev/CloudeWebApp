# ClaudeWebApp 仕様書

## 概要

ASP.NET Core 8 で構築した REST API サーバー。`Sample` リソースの CRUD 操作を提供する。

## 技術スタック

| 項目 | 内容 |
|------|------|
| フレームワーク | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 |
| DB（本番） | SQLite |
| DB（テスト） | MySQL（Pomelo.EntityFrameworkCore.MySql） |
| テストフレームワーク | NUnit 3 |

## アーキテクチャ

```
Controller
    └── ISampleUseCase（DI）
            └── SampleUseCase
                    └── ApplicationDbContext（EF Core）
                                └── MySQL / SQLite
```

- **Controller** — HTTPリクエストの受け取りとレスポンス返却のみ担当
- **UseCase** — ビジネスロジックと DB 操作を担当
- **DbContext** — EF Core によるデータアクセス層

## データモデル

### `Sample`（エンティティ）

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `Id` | `int` | 主キー（AUTO INCREMENT） |
| `Name` | `string` | 名前（必須） |
| `Description` | `string` | 説明（必須） |
| `CreatedAt` | `DateTime` | 作成日時（UTC） |

### `SampleDto`（入出力）

`Sample` と同じフィールド構成。作成時は `Id` / `CreatedAt` をリクエストボディに含めても無視される。

## API エンドポイント

ベースパス: `/api/samples`

### GET `/api/samples`

全件取得。

- レスポンス: `200 OK` + `SampleDto[]`

### GET `/api/samples/{id}`

ID 指定で1件取得。

- レスポンス: `200 OK` + `SampleDto`
- 存在しない場合: `404 Not Found`

### POST `/api/samples`

新規作成。

- リクエストボディ: `{ "name": "...", "description": "..." }`
- レスポンス: `201 Created` + `SampleDto`（`Location` ヘッダーあり）

### PUT `/api/samples/{id}`

更新。

- リクエストボディ: `{ "name": "...", "description": "..." }`
- レスポンス: `204 No Content`
- 存在しない場合: `404 Not Found`

### DELETE `/api/samples/{id}`

削除。

- レスポンス: `204 No Content`
- 存在しない場合: `404 Not Found`

## テスト

テストプロジェクト: `ClaudeWebApp.Tests`

接続先 MySQL は環境変数 `TEST_MYSQL_CONNECTION` で指定する。未設定時は `Server=localhost;User=root;Password=root;` をデフォルトとして使用。

```bash
export TEST_MYSQL_CONNECTION="Server=localhost;User=myuser;Password=mypass;"
dotnet test ClaudeWebApp.Tests/
```

各テストケースは `EnsureCreated` / `EnsureDeleted` により独立した DB で実行される。

## 既知の問題・改善候補

| 優先度 | 内容 |
|--------|------|
| 高 | `UpdateSampleAsync` が更新時に `CreatedAt` を上書きしている（作成日時は不変であるべき） |
| 高 | バリデーションが未実装（`Name` / `Description` の必須チェック、文字数制限など） |
| 中 | エラーハンドリングが不十分（`UpdateSampleAsync` の `catch` が全例外を握りつぶしている） |
| 中 | `SampleDto` をリクエスト用・レスポンス用に分離すべき（現状は1クラスを兼用） |
| 低 | ページネーション未対応（全件取得のみ） |
| 低 | `GET /api/samples` のソート・フィルタ未対応 |
