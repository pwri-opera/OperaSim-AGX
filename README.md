# ConstSim
## ソフトウェア要件
- Unity：2020.3.11f1
- AGX：2.29.4.4(x64, VS2015)
- Ubuntu：18.04
- ROS：Melodic
## 制御用ROSメッセージ
- 本シミュレータでは，以下のROSメッセージを受け取ることで建設機械モデルの各軸が制御される

| 対象軸 | ROSメッセージ型 | 指令  | Topic名 |
| ----  |  ---- | ---- | ---- |
|  クローラダンプの履帯速度  |  Twist  | 速度 | /ic120/tracks/cmd_vel |
|  荷台の角度  |  Float64  | 角度 | /ic120/vessel/cmd |
|  アームの角度  |  Float64  | 角度 | /zx120/arm/cmd |
|  ブームの角度  |  Float64  | 角度 | /zx120/boom/cmd |
|  バケットの角度  |  Float64  | 角度 | /zx120/bucket/cmd |
| 車体旋回の角度 | Float64 | 角度 | /zx120/swing/cmd |
| 油圧ショベルの履帯の速度 | Twist | 速度 | /zx120/tracks/cmd_vel |

## 実行方法
### ROS側(初期設定)
- rosbridge_websocketの起動
   - `roslaunch rosbridge_server rosbridge_websocket.launch`
- ショベルの初期姿勢の設定
   - ROSメッセージを介して，Unityモデルの関節角度の入力を行う
   - cmdメッセージを送信し、モデルの初期姿勢を定義する。以下に実行例を示す
      - 以下を実行することで，初期姿勢が定義される  
         - `rostopic pub /zx120/boom/cmd std_msgs/Float64 "data: -0.8"` 
         - `rostopic pub /zx120/bucket/cmd std_msgs/Float64 "data: 1.0"`
         - `rostopic pub /zx120/arm/cmd std_msgs/Float64 "data: 2"`
### Unity側
- Unity Hubより，本プロジェクトファイル(ConstSim)を追加
<p align="left">
  <img src="https://user-images.githubusercontent.com/82022162/159667401-9f5f0393-3e23-4178-98b4-90408fa83305.jpg" " width="600px">
</p>
                                                                                                                                  
- 追加されたプロジェクトのタイトルを押し，Unity Editorで開く
<p align="left">
  <img src="https://user-images.githubusercontent.com/82022162/159667974-c66c51f0-c067-4c7b-a434-2eecf9e2051d.jpg" " width="600px">
</p>             

- 建設機械のモデルが表示されていない場合には，プロジェクトウィンドウからAssets/Scenes/MainScene.unityをダブルクリックしてロードする
<p align="left">
  <img src="https://user-images.githubusercontent.com/82022162/159667970-b24fbc10-f4e8-4acf-8290-d6a541fd2efc.jpg" " width="600px">
</p>     
                                                                                                                                  
- Assets/AGXUnity/Plugins/x86_64のフォルダへAGX Dynamicsのライセンスファイルをコピー&ペースト
<p align="left">
  <img src="https://user-images.githubusercontent.com/82022162/159667980-0426a5a0-f920-4a50-99b3-0b70ddc442c2.jpg" " width="600px">
</p>            

- HierarchyウィンドウからRosConnectorというGameObjectを選択し，Ros Bridge Sercer Urlを入力する
<p align="left">
  <img src="https://user-images.githubusercontent.com/82022162/159667977-d040451e-1cfd-4d5c-bb8e-5d0c6107ba8b.jpg" " width="600px">
</p>
                                                                                                                                  
- Playボタンを押してシミュレーションを実行
### ROS側(制御プログラムの実行)
- Moveitに基づく油圧ショベルを行うためには以下を実行
   - `roslaunch zx120_unity zx120_standby.launch`
   - マーカを任意の位置に移動させ，`Plan&Execute`ボタンを押すと，Unity側のモデルの制御が行われる．
- move_baseに基づくクローラダンプの制御を行うためには，以下を実行
   - `roslaunch ic120_unity ic120_standby.launch`
   - `2D Nav Goal`を押して任意の位置，方向を指定することで，ダンプトラックが目標に向かって制御される．
