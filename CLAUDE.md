# CLAUDE.md

## プロジェクト概要

ASP.NET Core 10 で構築した REST API サーバー。`Sample` リソースの CRUD を提供する。
詳細な仕様は [SPEC.md](SPEC.md) を参照。

## 技術スタック

- **フレームワーク**: ASP.NET Core 10 / .NET 10
- **ORM**: Entity Framework Core 9（Pomelo 互換のため 9.x）
- **DB（本番）**: MySQL / **DB（ローカル）**: SQLite（`MYSQL_CONNECTION` 未設定時）
- **キャッシュ**: Redis / オンメモリ（`REDIS_CONNECTION` 未設定時）
- **テスト**: NUnit 4 + Shouldly

## アーキテクチャ

```
Controller → ISampleUseCase（DI） → SampleUseCase
                                        ├── ApplicationDbContext（EF Core）
                                        └── IDistributedCache
```

- **Controller**: HTTPリクエスト受け取りとレスポンス返却のみ
- **UseCase**: ビジネスロジック・DB操作・キャッシュ制御
- **DbContext**: 論理削除のグローバルクエリフィルタを適用

## 開発コマンド

```bash
# アプリ起動（MySQL + Redis 込み）
docker compose up app

# テスト実行（Docker）
docker compose run --rm test

# テスト実行（ローカル）
export TEST_MYSQL_CONNECTION="Server=localhost;User=root;Password=root;"
dotnet test ClaudeWebApp.Tests/
```

## ルール

### コミット
- コミットメッセージは**日本語**で書く（英語の技術用語はそのままでOK）
- `git push` は明示的に指示された場合のみ実行する

### 自動生成ファイル
- EF Core マイグレーション等の自動生成ファイルは**絶対に直接編集しない**
- カバレッジ除外など追記が必要な場合は `partial class` で対応する（例: `MigrationsCoverageExclusion.cs`）

### テスト
- テストは実際の MySQL DB に接続して実行する（モックDB不可）
- 各テストは独立した DB（`EnsureCreated` / `EnsureDeleted`）で実行
- キャッシュは `MemoryDistributedCache` を使用（Redis 不要）
