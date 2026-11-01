# テスト用マスタデータ

`resources/` はテスト専用の CSV です。テストのビルド時に、出力先の `resources/` をこの内容で上書きします（`server.tests.csproj` の `CopyMasterDataFixtures`）。

- 本番のマスタデータ（スプレッドシート）を変えても、テストの前提は変わりません
- テストで新しいマスタデータが必要になったら、ここの CSV を編集します
- 本番のマスタデータで整合性テストを回すときは `-p:UseMasterDataFixtures=false` を付けます

```bash
dotnet test tests/server.tests/server.tests.csproj -p:UseMasterDataFixtures=false \
  --filter "FullyQualifiedName~MasterDataIntegrityTests"
```
