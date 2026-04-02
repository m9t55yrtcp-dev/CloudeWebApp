# ClaudeWebApp 仕様書

## 概要

ASP.NET Core 8 で構築した REST API サーバー。`Sample` リソースの CRUD 操作を提供する。

## 技術スタック

| 項目 | 内容 |
| ---- | ---- |
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
| --------- | -- | ---- |
| `Id` | `int` | 主キー（AUTO INCREMENT） |
| `Name` | `string` | 名前（必須、最大100文字） |
| `Description` | `string` | 説明（最大500文字） |
| `CreatedAt` | `DateTime` | 作成日時（UTC、更新不可） |

### `SampleRequest`（リクエスト）

| フィールド | 型 | バリデーション |
| --------- | --- | ------------ |
| `Name` | `string` | Required、MaxLength(100) |
| `Description` | `string` | MaxLength(500) |

### `SampleResponse`（レスポンス）

| フィールド | 型 |
| --------- | -- |
| `Id` | `int` |
| `Name` | `string` |
| `Description` | `string` |
| `CreatedAt` | `DateTime` |

### `PagedResult<T>`（ページネーションラッパー）

| フィールド | 型 | 説明 |
| --------- | -- | ---- |
| `Items` | `IEnumerable<T>` | 取得したアイテム一覧 |
| `TotalCount` | `int` | 全件数 |
| `Page` | `int` | 現在のページ番号 |
| `PageSize` | `int` | 1ページあたりの件数 |
| `TotalPages` | `int` | 総ページ数（算出値） |

## API エンドポイント

ベースパス: `/api/samples`

### GET `/api/samples`

全件取得（ページネーション・ソート・フィルタ対応）。

| クエリパラメータ | 型 | デフォルト | 説明 |
| -------------- | -- | --------- | ---- |
| `page` | `int` | `1` | ページ番号 |
| `pageSize` | `int` | `20` | 1ページあたりの件数 |
| `sortBy` | `string` | なし | `name` または `createdAt` |
| `descending` | `bool` | `false` | 降順にする場合 `true` |
| `nameFilter` | `string` | なし | 名前の部分一致フィルタ |

- レスポンス: `200 OK` + `PagedResult<SampleResponse>`

### GET `/api/samples/{id}`

ID 指定で1件取得。

- レスポンス: `200 OK` + `SampleResponse`
- 存在しない場合: `404 Not Found`

### POST `/api/samples`

新規作成。

- リクエストボディ: `SampleRequest`
- レスポンス: `201 Created` + `SampleResponse`（`Location` ヘッダーあり）
- バリデーションエラー: `400 Bad Request`

### PUT `/api/samples/{id}`

更新（`CreatedAt` は変更不可）。

- リクエストボディ: `SampleRequest`
- レスポンス: `204 No Content`
- 存在しない場合: `404 Not Found`
- バリデーションエラー: `400 Bad Request`

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
