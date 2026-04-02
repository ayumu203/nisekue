# 耐久値初期化調査

以下の主要なプレイヤー装備生成経路をコードで追跡したところ、いずれもマスタの `MaxDurability` をそのまま `PlayerEquipment.Durability` に設定しており、初期値0になるコードパスは確認できませんでした:

- 新規作成時の初期装備 ( `server/endpoints/PlayerEndpoints.cs` で `CreateStarterEquipments`) はマスタの `MaxDurability` を `PlayerEquipment` コンストラクタへ渡す。
- クエスト報酬で装備が付与される `QuestRunService` では `rewardMaster.MaxDurability` を直接指定。
- 宝の地図報酬も `TreasureMapEndpoints` で `equipmentMaster.MaxDurability` を使用して `PlayerEquipmentEntity` を生成。

データベースへの保存/復元 (`DbPlayerEquipmentRepository`) も `Durability` をそのまま永続化しており、ロード側で補正するコードは存在しません。

以上から、現時点では耐久値が0になる問題の再現条件が不明であり、コード上に0をセットする箇所はないと考えられます。
今後、`Durability` が `0` になってしまうデータを確認した場合は、上記経路のどこかで `playerEquipment.ConsumeDurability` や `RepairDurability` などが期待と異なるタイミングで呼ばれていないかを追加調査してください。
