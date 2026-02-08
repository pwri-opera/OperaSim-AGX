# OperaSim-AGX

Simulator on Unity + AGX Dynamics communicating with ROS

## 詳細マニュアル
[OperaSim-AGXマニュアル](https://operasim-agx-doc.readthedocs.io/ja/latest/)

## 概要

- 油圧ショベル, クローラダンプ, ブルドーザと土砂の挙動を再現したシミュレータである
- [Unity][Unity page]を使用する. 物理エンジンは[AGX Dynamics][AGX Dynamics page]を使用する
  - 本シミュレータを実行する場合, Unity, AGX Dynamicsライセンスが必要である
- ROSメッセージを使って本シミュレータの建設機械を制御可能である
- 本シミュレータから建設機械の情報(関節の角度など)を含んだROSメッセージを受信可能である
- ROSメッセージの送受信に[ROS-TCP-Connector][ROS-TCP-Connector page]を使用する

[Unity page]: https://unity.com/
[AGX Dynamics page]: https://www.vmc-motion.com/%E5%95%86%E5%93%81%E3%83%BB%E3%82%B5%E3%83%BC%E3%83%93%E3%82%B9/agx-dynamics/

## ソフトウェア要件

- Unity：2022.3.62f1
- AGX：2.38.0.1(X64 VS2022)
  - AGX (Core)
  - Particles
  - Granular
  - Terrain
  - Tracks

### Unityで使用するパッケージ

初回プロジェクト読み込み時に自動的に追加される.  
(もしも追加されなかった場合は手動で追加すること)

- [AGXUnity][AGXUnity page]: 5.0.1
- [ROS-TCP-Connector][ROS-TCP-Connector page]: 0.7.0
- [URDF-Importer][URDF-Importer page]: 0.5.2
- [UnitySensors, UnitySensorsROS][UnitySensors page]: 開発版

[AGXUnity page]: https://github.com/Algoryx/AGXUnity
[ROS-TCP-Connector page]: https://github.com/Unity-Technologies/ROS-TCP-Connector
[URDF-Importer page]: https://github.com/Unity-Technologies/URDF-Importer
[UnitySensors page]: https://github.com/Field-Robotics-Japan/UnitySensors

## 実行方法

- Unity Hubより, 本プロジェクトファイル(OperaSim-AGX)を追加

<p align="left">
  <img src="https://user-images.githubusercontent.com/82022162/159667401-9f5f0393-3e23-4178-98b4-90408fa83305.jpg" " width="600px">
</p>

- 追加されたプロジェクトのタイトルを押し, Unity Editorで開く

<p align="left">
  <img src="https://user-images.githubusercontent.com/82022162/159667974-c66c51f0-c067-4c7b-a434-2eecf9e2051d.jpg" " width="600px">
</p>

- 建設機械のモデルが表示されていない場合には, プロジェクトウィンドウからAssets/Scenes/MainScene.unityをダブルクリックしてロードする

<p align="left">
  <img src="https://user-images.githubusercontent.com/82022162/159667970-b24fbc10-f4e8-4acf-8290-d6a541fd2efc.jpg" " width="600px">
</p>

- Assets/AGXUnity/Plugins/x86_64のフォルダへAGX Dynamicsのライセンスファイルをコピー&ペースト

<p align="left">
  <img src="https://user-images.githubusercontent.com/82022162/159667980-0426a5a0-f920-4a50-99b3-0b70ddc442c2.jpg" " width="600px">
</p>

- RoboticsタブからROS Settingを開き、ROS TCP Endpointで接続するPCのIPアドレスおよびポート番号を入力する

- Playボタンを押してシミュレーションを実行
