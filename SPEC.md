# ClaudeWebApp 仕様書

## 概要

ASP.NET Core 10 で構築した REST API サーバー。`Sample` リソースの CRUD 操作を提供する。

## 技術スタック

| 項目 | 内容 |
| ---- | ---- |
| フレームワーク | ASP.NET Core 10 |
| ORM | Entity Framework Core 9（Pomelo 互換のため 9.x） |
| DB（本番） | MySQL（Pomelo.EntityFrameworkCore.MySql 9.0.0） |
| DB（ローカル開発） | SQLite（`MYSQL_CONNECTION` 未設定時） |
| キャッシュ | Redis（`REDIS_CONNECTION` 未設定時はオンメモリ） |
| API ドキュメント | Swagger UI（`/swagger`） |
| テストフレームワーク | NUnit 4 |
| テスト用キャッシュ | MemoryDistributedCache（Redis不要） |

## アーキテクチャ

```
Controller
    └── ISampleUseCase（DI）
            └── SampleUseCase
                    ├── ApplicationDbContext（EF Core）
                    │           └── MySQL / SQLite
                    └── IDistributedCache
                                └── Redis / MemoryDistributedCache
```

- **Controller** — HTTPリクエストの受け取りとレスポンス返却のみ担当
- **UseCase** — ビジネスロジック、DB操作、キャッシュ制御を担当
- **DbContext** — EF Core によるデータアクセス層。論理削除のグローバルクエリフィルタを適用

## データモデル

### `BaseEntity`（基底クラス）

全エンティティが継承する。

| フィールド | 型 | 説明 |
| --------- | -- | ---- |
| `CreatedAt` | `DateTime` | 作成日時（JST）、更新不可 |
| `UpdatedAt` | `DateTime` | 最終更新日時（JST）、作成時は `CreatedAt` と同値 |
| `DeletedAt` | `DateTime?` | 論理削除日時（JST）。`null` = 未削除 |

### `Sample`（エンティティ）

`BaseEntity` を継承。

| フィールド | 型 | 説明 |
| --------- | -- | ---- |
| `Id` | `int` | 主キー（AUTO INCREMENT） |
| `Name` | `string` | 名前（必須、最大100文字） |
| `Description` | `string` | 説明（最大500文字） |

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
| `UpdatedAt` | `DateTime` |
| `DeletedAt` | `DateTime?` |

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

Swagger UI: `http://localhost:8080/swagger`

### GET `/api/samples`

全件取得（ページネーション・ソート・フィルタ対応）。論理削除済みレコードは除外。

| クエリパラメータ | 型 | デフォルト | 説明 |
| -------------- | -- | --------- | ---- |
| `page` | `int` | `1` | ページ番号 |
| `pageSize` | `int` | `20` | 1ページあたりの件数 |
| `sortBy` | `string` | なし | `name` または `createdAt` |
| `descending` | `bool` | `false` | 降順にする場合 `true` |
| `nameFilter` | `string` | なし | 名前の部分一致フィルタ |

- レスポンス: `200 OK` + `PagedResult<SampleResponse>`

### GET `/api/samples/{id}`

ID 指定で1件取得。論理削除済みの場合は `404`。

- レスポンス: `200 OK` + `SampleResponse`
- 存在しない / 削除済みの場合: `404 Not Found`

### POST `/api/samples`

新規作成。`CreatedAt` / `UpdatedAt` はサーバー側でセット。

- リクエストボディ: `SampleRequest`
- レスポンス: `201 Created` + `SampleResponse`（`Location` ヘッダーあり）
- バリデーションエラー: `400 Bad Request`

### PUT `/api/samples/{id}`

更新。`CreatedAt` は不変、`UpdatedAt` をサーバー側で更新。

- リクエストボディ: `SampleRequest`
- レスポンス: `204 No Content`
- 存在しない場合: `404 Not Found`
- バリデーションエラー: `400 Bad Request`

### DELETE `/api/samples/{id}`

論理削除。`DeletedAt` に日時をセットし、物理削除は行わない。

- レスポンス: `204 No Content`
- 存在しない場合: `404 Not Found`

## キャッシュ戦略

| 対象 | キャッシュキー | TTL | 無効化タイミング |
| ---- | ------------ | --- | -------------- |
| 個別取得 | `sample:{id}` | 5分 | 更新・削除時に削除 |
| 一覧取得 | `samples:all:{version}:{params}` | 1分 | 作成・更新・削除時にバージョンをインクリメント |

バージョンキー（`samples:version`）をインクリメントすることで、古い一覧キャッシュを自然に無効化する。

## Docker 構成

```bash
docker compose up app      # アプリ + MySQL + Redis を起動
docker compose run --rm test  # テストを実行
```

### サービス一覧

| サービス | イメージ | ポート | 説明 |
| ------- | ------- | ----- | ---- |
| `mysql` | mysql:8.0 | 3306 | アプリ用 DB |
| `redis` | redis:7-alpine | 6379 | キャッシュ |
| `app` | （ビルド） | 8080 | API サーバー |
| `test` | （ビルド） | - | テスト実行用 |

### 環境変数

| 変数名 | サービス | 説明 |
| ------ | ------- | ---- |
| `MYSQL_CONNECTION` | app | MySQL 接続文字列（未設定時は SQLite） |
| `REDIS_CONNECTION` | app | Redis 接続文字列（未設定時はオンメモリキャッシュ） |
| `TZ` | app | タイムゾーン（`Asia/Tokyo`） |
| `TEST_MYSQL_CONNECTION` | test | テスト用 MySQL 接続文字列 |

## テスト

テストプロジェクト: `ClaudeWebApp.Tests`

各テストケースは独立した MySQL DB（`EnsureCreated` / `EnsureDeleted`）と `MemoryDistributedCache` で実行されるため、Redis は不要。

```bash
# ローカル実行
export TEST_MYSQL_CONNECTION="Server=localhost;User=root;Password=root;"
dotnet test ClaudeWebApp.Tests/

# Docker で実行
docker compose run --rm test
```
