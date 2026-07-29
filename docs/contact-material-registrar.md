# 履帯の方向性摩擦は ContactMaterialManager ではなく Registrar が持つ

AGXUnity のマニュアルどおりに `ContactMaterialManager` で方向性摩擦(oriented friction)を設定すると、このプロジェクトでは意図した挙動になりません。履帯の異方性摩擦は `MachineContactMaterialRegistrar` が実行時に機体ごとに設定します。モデルやパラメータを触る前にこの文書を読んでください。

- 実装: [MachineContactMaterialRegistrar.cs](../Assets/Scripts/MachineContactMaterialRegistrar.cs)
- 経緯: issue #10 / #18 / #51

## 標準の方式で困る理由

AGX は接触ごとに、素材ペア `(Material1, Material2)` に一致する ContactMaterial(CM)を1つだけ引き当てます。oriented friction の参照フレーム(どちらが機体の前方か)は、CM が持つ FrictionModel に1つしか結べません。

一方、ic120 も zx120 も、同じ `DumpTruckTrackShapeMat` や `ExcavatorTrackShapeMat` を共有しています。素材が同じなら CM も同じで、参照フレームも1つです。つまり Manager のエントリに機体 A の `body_link` を設定すると、素材を共有する機体 B も A の向きで摩擦が計算されます。B は横向きの低い摩擦係数が進行方向に効いて滑ります。これが #10 の症状です。

## Registrar の方式

機体ごとに素材から分けます。

| タイミング | 処理 | 参照 |
|---|---|---|
| `Awake` | ShapeMaterial と FrictionModel と CM をクローンし、機体配下の Shape と Track の材質をクローンに張り替える | [L56](../Assets/Scripts/MachineContactMaterialRegistrar.cs#L56) |
| `Awake` | インスペクタ参照の欠落を検査して LogError を出す(#67) | [L112](../Assets/Scripts/MachineContactMaterialRegistrar.cs#L112) |
| `Start` | クローン CM を native に登録し、`InitializeOrientedFriction(true, referenceObject, primaryDirection)` を呼ぶ | [L142](../Assets/Scripts/MachineContactMaterialRegistrar.cs#L142) |
| `OnDestroy` | クローンを Manager と native から外して破棄する | [L181](../Assets/Scripts/MachineContactMaterialRegistrar.cs#L181) |

素材ペアが `(クローン素材, Ground/Terrain)` になるので、機体ごとに別の CM が引き当てられ、参照フレームも機体ごとに持てます。

適用されているのは `ic120_pref` と `zx120_pref` と `zx200` の3プレハブです。ブルドーザには付いていません。

## 誰が何を担当するか

| 登録経路 | 素材ペア | oriented | 適用先 |
|---|---|---|---|
| ContactMaterialManager(base CM) | base 素材 × Ground / Terrain | 無効 | Registrar を持たない機体 |
| Registrar(クローン CM) | クローン素材 × Ground / Terrain | 有効(機体ごとの参照フレーム) | ic120 / zx120 / zx200 |

## 初期化順

Unity は全 `Awake` を全 `Start` より先に実行します。Registrar の材質の張り替えは `Awake` なので、`ContactMaterialManager.Initialize()`([L125](../Assets/AGXUnity/AGXUnity/ContactMaterialManager.cs#L125)、`ScriptComponent.Start` 経由)より必ず先です。

Manager と Registrar が登録するのは別の素材ペアなので、どちらが先に登録しても競合しません。「後から登録したほうが勝つ」という優先順位の話にはなりません。

## 踏みやすい落とし穴

**Manager のエントリに `Reference Object` を設定しない**

`m_isOriented: 1` かつ `m_referenceObject` が null のエントリは、`InitializeOrientedFriction` の先頭で警告なく return するため何も起きません([ContactMaterial.cs#L403](../Assets/AGXUnity/AGXUnity/ContactMaterial.cs#L403))。ここに親切心で機体を設定すると、その瞬間に #10 が再発します。履帯の oriented は Registrar の担当です。

**プレハブを編集すると Registrar の参照が壊れる**

AGXUnity はプレハブとの相性が悪く、機体のパラメータ調整のたびにインスペクタ参照が外れることがあります。開始時に LogError を出すようにしてあるので([L112](../Assets/Scripts/MachineContactMaterialRegistrar.cs#L112))、Console を確認してください。自動復元はしません。名前に依存した復元は別の壊れ方を生むためです。

**oriented にしたくないペアは別枠に入れる**

履帯と転輪のように異方性を効かせたくない CM は `baseNonOrientedContactMaterials` に入れます。ここに入れた CM はクローンされますが FrictionModel はそのままで、oriented 化されません(#59)。

**Registrar を持たない機体は base CM にフォールバックする**

フォールバック先の CM は係数 `{x: 1, y: 0.2}` のまま `m_isOriented: 0` なので、ワールド軸基準の異方性摩擦になります。機体の向きには追従しません。平地でも進行方位によって効く係数が変わるため、フォールバックとしては期待できません。ブルドーザが現状これに該当します。

## 新しい機体に適用するとき

1. 機体プレハブのルートに `MachineContactMaterialRegistrar` を付ける
2. `Base Track Shape Material` にその機体の履帯 ShapeMaterial、`Base Friction Model` にそれが参照する FrictionModel を設定する
3. `Base Contact Materials` に履帯 vs Ground / Terrain の CM、`Base Non Oriented Contact Materials` に履帯 vs 転輪の CM を入れる
4. `Track Components` に TrackL / TrackR を設定する。実行時に生成される履帯シューの材質はここから供給される
5. `Reference Object` に車体の `body_link`、`Primary Direction` に機体前方の軸を設定する
6. Manager 側の対応エントリは `m_isOriented: 0` のままにしておく
