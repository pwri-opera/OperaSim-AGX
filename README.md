# OperaSim-AGX

Simulator on Unity + AGX Dynamics communicating with ROS

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

- Unity：2022.3.16f1
- AGX：2.37.3.0(X64 VS2022)
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

- HierarchyウィンドウからRosConnectorというGameObjectを選択し, Ros Bridge Sercer Urlを入力する

<p align="left">
  <img src="https://user-images.githubusercontent.com/82022162/159667977-d040451e-1cfd-4d5c-bb8e-5d0c6107ba8b.jpg" " width="600px">
</p>

- Playボタンを押してシミュレーションを実行

## ROS通信

[com3_ros][com3_ros page]で定義している独自のメッセージ型を使用している.
メッセージ型の実装ははAssets/RosMessages/msgフォルダ内のソースコードに記載している.

[com3_ros page]: https://github.com/pwri-opera/com3_ros

### 油圧ショベル

ROS >> Unity

|項目|トピック名|メッセージ型|備考|
|-|-|-|-|
|フロント<br>(ブーム, アーム, バケット, 旋回)<br>動作指令|/車体名/front_cmd|com3_ros/msg/JointCmd|以下の順に指令値を格納すること<br>0: bucket_joint<br>1: arm_joint<br>2: boom_joint<br>3: swing_joint|
|左右クローラへの動作指令|/車体名/track_cmd|com3_ros/msg/JointCmd|以下の順に指令値を格納すること<br>0: left_track<br>1: right_track|
|車両中心の<br>並進移動速度/回転速度指令|/車体名/cmd_vel|Geometry_msg/msg/Twist|-|
|非常停止|/車体名/emg_stop_cmd|std_msgs/msg/Bool|true: その場で停止|

Unity >> ROS

|項目|トピック名|メッセージ型|備考|
|-|-|-|-|
|各関節の角度, 角速度|/車体名/joint_state|sensor_msgs/msg/JointState|以下の順で情報が格納される<br>0: bucket_joint<br>1: arm_joint<br>2: boom_joint<br>3: swing_joint<br>4: right_track_joint<br>5: left_track_joint|
|ローカル座標系における車両の中心姿勢|/車体名/odom_pose|nav_msgs/msg/Odometry|-|
|グローバル座標(平面直角座標)系における車両の中心姿勢|/車体名/global_pose|nav_msgs/msg/Odometry|-|
|油圧アクチュエータのメイン圧力|/車体名/main_fluid_pressure|com3_ros/sensor_msgs/FluidPressure|以下の順で情報が格納される<br>0: boom_up_main_pressure<br>1: boom_down_main_pressure<br>2: arm_crowd_main_pressure<br>3: arm_dump_main_pressure<br>4: bucket_crowd_main_pressure<br>5: bucket_dump_main_pressure<br>6: swing_right_main_pressure<br>7: swing_left_main_pressure<br>8: right_track_forward_main_prs<br>9: right_track_backward_main_prs<br>10: "left_track_forward_main_prs<br>11: left_track_backward_main_prs<br>12: attachment_a_main_pressure<br>13: attachment_b_main_pressure<br>14: assist_a_main_pressure<br>15: assist_b_main_pressure|
|油圧アクチュエータのパイロット圧力|/車体名/main_fluid_pressure|com3_ros/sensor_msgs/FluidPressure|以下の順で情報が格納される<br>0: boom_up_pilot_pressure<br>1: boom_down_pilot_pressure<br>2: arm_crowd_pilot_pressure<br>3: arm_dump_pilot_pressure<br>4: bucket_crowd_pilot_pressure<br>5: bucket_dump_pilot_pressure<br>6: swing_right_pilot_pressure<br>7: swing_left_pilot_pressure<br>8: right_track_forward_pilot_prs<br>9: right_track_backward_pilot_prs<br>10: "left_track_forward_pilot_prs<br>11: left_track_backward_pilot_prs<br>12: attachment_a_pilot_pressure<br>13: attachment_b_pilot_pressure<br>14: assist_a_pilot_pressure<br>15: assist_b_pilot_pressure|
|上部構造体の旋回角度|/車体名/upper_body_rot|sensor_msggs/msg/Imu|-|
|バケット内土量|/車体名/soil_volume|std_msg/msg/Float64|-|

### クローラダンプ

ROS >> Unity

|項目|トピック名|メッセージ型|備考|
|-|-|-|-|
|旋回, ダンプの動作指令|/車体名/rot_dump_cmd|com3_ros/msg/JointCmd|以下の順で指令値を格納すること<br>0: rotate_joint<br>1: dump_joint|
|左右クローラへの動作指令|/車体名/track_cmd|com3_ros/msg/JointCmd|以下の順で指令値を格納すること<br>0: right_track<br>1: left_track|
|走行ボリュームの動作指令|/車体名/track_volume_cmd|com3_ros/msg/JointCmd|以下の順で指令値を格納すること<br>0: forward_volume<br>1: turn_volume<br>それぞれのeffortに-1.0~1.0の値を設定する.|
|車両中心の<br>並進移動速度/回転速度指令|/車体名/cmd_vel|Geometry_msg/msg/Twist|-|
|非常停止|/車体名/emg_stop_cmd|std_msgs/msg/Bool|true: その場で停止|

Unity >> ROS

|項目|トピック名|メッセージ型|備考|
|-|-|-|-|
|各関節の角度, 角速度|/車体名/joint_state|sensor_msgs/msg/JointState|以下の順で情報が格納される<br>0: rotate_joint<br>1: dump_joint<br>2: right_track_joint<br>3: left_track_joint|
|ローカル座標系における車両の中心姿勢|/車体名/odom_pose|nav_msgs/msg/Odometry|-|
|グローバル座標(平面直角座標)系における車両の中心姿勢|/車体名/global_pose|nav_msgs/msg/Odometry|-|
|油圧アクチュエータのメイン圧力|/車体名/main_fluid_pressure|com3_ros/sensor_msgs/FluidPressure|-|
|油圧アクチュエータのパイロット圧力|/車体名/main_fluid_pressure|com3_ros/sensor_msgs/FluidPressure|-|
|上部構造体の旋回角度|/車体名/upper_body_rot|sensor_msggs/msg/Imu|-|
|バケット内土量|/車体名/soil_volume|std_msg/msg/Float64|-|

### ブルドーザ

ROS >> Unity

|項目|トピック名|メッセージ型|備考|
|-|-|-|-|
|ブレード<br>(リフト, チルト, アングル)<br>動作指令|/車体名/blade_cmd|com3_ros/msg/JointCmd|配列に以下の順に指令値を格納すること<br>0: lift_joint<br>1: tilt_joint<br>2: angle_joint|
|左右クローラへの動作指令|/車体名/track_cmd|com3_ros/msg/JointCmd|配列に以下の順に指令値を格納すること<br>0: right_track<br>1: left_track|
|車両中心の<br>並進移動速度/回転速度指令|/車体名/cmd_vel|Geometry_msg/msg/Twist|-|
|非常停止|/車体名/emg_stop_cmd|std_msgs/msg/Bool|true: その場で停止|

Unity >> ROS

|項目|トピック名|メッセージ型|備考|
|-|-|-|-|
|各関節の角度, 角速度|/車体名/joint_state|sensor_msgs/msg/JointState|以下の順で情報が格納される<br>0: lift_joint<br>1: tilt_joint<br>2: angle_joint<br>3: right_track_joint<br>4: left_track_joint|
|ローカル座標系における車両の中心姿勢|/車体名/odom_pose|nav_msgs/msg/Odometry|-|
|グローバル座標(平面直角座標)系における車両の中心姿勢|/車体名/global_pose|nav_msgs/msg/Odometry|-|
|油圧アクチュエータのメイン圧力|/車体名/main_fluid_pressure|com3_ros/sensor_msgs/FluidPressure|以下の順で情報が格納される<br>0: lift_up_main_pressure<br>1: lift_down_main_pressure<br>2: tilt_forward_main_pressure<br>3: tilt_back_main_pressure<br>4: angle_right_main_pressure<br>5: angle_left_main_pressure|
|油圧アクチュエータのパイロット圧力|/車体名/main_fluid_pressure|com3_ros/sensor_msgs/FluidPressure|以下の順で情報が格納される<br>0: lift_up_pilot_pressure<br>1: lift_down_pilot_pressure<br>2: tilt_forward_pilot_pressure<br>3: tilt_back_pilot_pressure<br>4: angle_right_pilot_pressure<br>5: angle_left_pilot_pressure|
|上部構造体の旋回角度|/車体名/upper_body_rot|sensor_msggs/msg/Imu|-|

### 備考

- Pub/Sub方式で通信
- 上記表のトピック名にある「車体名」は建設機械モデルの名称で置き換えること
  - より具体的には,各建設機械モデルのルートのゲームオブジェクト名

## オプション

### 指令値の設定

建設機械モデルはROS通信でROSからの指令によって動作する.
指令には次の3種類がある.

Movement Control Type

|項目|説明|
|-|-|
|Actuator Command|建設機械の各関節それぞれに対して直接動作指令を行う|
|Twist Command|建設機械の移動速度,旋回速度を指定する|
|Volume Command|建設機械の移動速度,旋回速度を比率(-1.0~1.0)で指定する|

Actuator Commandには次の3種類のオプションがある.

Control Type

|項目|説明|
|-|-|
|Position|関節の角度の変化量を入力する|
|Speed|関節が移動する角速度を入力する|
|Force|関節が接続しているパーツに対して発生する力を入力する|

建設機械モデルがどのパターンで入力を受け取るか,各モデルのインスペクタで設定できる.

1. HierarchyビューでMACHINESに配置されている建設機械モデルを選択
2. ???Inputという名前のスクリプトがアタッチされていることを確認する.???には建設機械のモデル名(Excavator, Dumptruck, Bulldozer)が入る
3. Movement Control Typeを選択する
4. Control Typeを選択する

なお,建設機械モデルごとに下記の入力パターンに対応している.
対応しているパターンについては,具体的なトピック名も記載した.

|モデル|パターン|対応状況|トピック名|
|-|-|-|-|
|油圧ショベル|Actuator Command|対応|/車体名/front_cmd<br>/車体名/track_cmd|
|油圧ショベル|Twist Command|対応|/車体名/cmd_vel|
|油圧ショベル|Volume Command|未対応|-|
|クローラダンプ|Actuator Command|対応|/車体名/rot_dump_cmd<br>/車体名/track_cmd|
|クローラダンプ|Twist Command|対応|/車体名/cmd_vel|
|クローラダンプ|Volume Command|対応|/車体名/track_volume_cmd|
|ブルドーザ|Actuator Command|対応|/車体名/blade_cmd<br>/車体名/track_cmd|
|ブルドーザ|Twist Command|対応|/車体名/cmd_vel|
|ブルドーザ|Volume Command|未対応|-|
